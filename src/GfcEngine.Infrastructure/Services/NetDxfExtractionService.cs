using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using netDxf;
using netDxf.Entities;
using NetTopologySuite.Geometries;
using GfcEngine.Application.Common.Interfaces;
using GfcEngine.Domain.Entities;
using GfcEngine.Infrastructure.Persistence;

namespace GfcEngine.Infrastructure.Services;

public class NetDxfExtractionService : IDxfExtractionService
{
    private readonly ApplicationDbContext _context;
    private readonly ICrsTransformer _transformer;
    private readonly ILogger<NetDxfExtractionService> _logger;
    private static readonly GeometryFactory _geomFactory = new(new PrecisionModel(), 32644);

    public NetDxfExtractionService(
        ApplicationDbContext context,
        ICrsTransformer transformer,
        ILogger<NetDxfExtractionService> logger)
    {
        _context = context;
        _transformer = transformer;
        _logger = logger;
    }

    public async Task<DxfExtractionResult> ExtractAsync(string filePath, int sourceEpsg, Guid workspaceId, CancellationToken cancellationToken = default)
    {
        var result = new DxfExtractionResult();

        if (!File.Exists(filePath))
        {
            result.Success = false;
            result.ErrorCode = "FILE_NOT_FOUND";
            result.ErrorMessage = $"Drawing file could not be found at {filePath}.";
            return result;
        }

        DxfDocument dxf;
        try
        {
            dxf = DxfDocument.Load(filePath);
            if (dxf == null)
            {
                result.Success = false;
                result.ErrorCode = "INVALID_DXF";
                result.ErrorMessage = "The DXF document failed to load or is corrupt.";
                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading DXF file {FilePath}", filePath);
            result.Success = false;
            result.ErrorCode = "UNREADABLE_FILE";
            result.ErrorMessage = $"Could not parse DXF file: {ex.Message}";
            return result;
        }

        // Fetch layer mappings for workspace
        var mappings = await _context.CadLayerMappings
            .Where(m => m.WorkspaceId == workspaceId)
            .ToDictionaryAsync(m => m.CadLayer, StringComparer.OrdinalIgnoreCase, cancellationToken);

        // Fetch workspace configuration for boundary / warning checks
        var workspace = await _context.Workspaces.FirstOrDefaultAsync(w => w.Id == workspaceId, cancellationToken);
        var farThreshold = workspace?.FarFromExistingDataWarningM ?? 5000.0;

        var extractedList = new List<ExtractedEntityDto>();
        var unmappedSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Extract TEXT annotations to match nearby features
        var textLabels = new List<(string text, Coordinate coord)>();
        foreach (var text in dxf.Entities.Texts)
        {
            textLabels.Add((text.Value, new Coordinate(text.Position.X, text.Position.Y)));
        }
        foreach (var mtext in dxf.Entities.MTexts)
        {
            textLabels.Add((mtext.PlainText(), new Coordinate(mtext.Position.X, mtext.Position.Y)));
        }

        // Process Lines
        foreach (var line in dxf.Entities.Lines)
        {
            ProcessEntity(line.Layer.Name, () =>
            {
                var p1 = new Coordinate(line.StartPoint.X, line.StartPoint.Y);
                var p2 = new Coordinate(line.EndPoint.X, line.EndPoint.Y);
                var geom = _geomFactory.CreateLineString(new[] { p1, p2 });
                double? z = (line.StartPoint.Z != 0 || line.EndPoint.Z != 0) ? (line.StartPoint.Z + line.EndPoint.Z) / 2.0 : null;
                return (geom, "line", z);
            });
        }

        // Process Polylines2D (handles both LWPOLYLINE and 2D POLYLINE)
        foreach (var pl in dxf.Entities.Polylines2D)
        {
            ProcessEntity(pl.Layer.Name, () =>
            {
                var coords = pl.Vertexes.Select(v => new Coordinate(v.Position.X, v.Position.Y)).ToList();
                if (coords.Count < 2) return (null, "", null);

                if (pl.IsClosed)
                {
                    if (coords.First().Distance(coords.Last()) > 0.001)
                    {
                        coords.Add(new Coordinate(coords.First().X, coords.First().Y));
                    }
                    var ring = _geomFactory.CreateLinearRing(coords.ToArray());
                    var poly = _geomFactory.CreatePolygon(ring);
                    return (poly, "polygon", (double?)null);
                }
                else
                {
                    var line = _geomFactory.CreateLineString(coords.ToArray());
                    return (line, "line", (double?)null);
                }
            });
        }

        // Process Polylines3D (Crucial for 3D utilities: depth stored as negative Z)
        foreach (var pl in dxf.Entities.Polylines3D)
        {
            ProcessEntity(pl.Layer.Name, () =>
            {
                var coords = pl.Vertexes.Select(v => new Coordinate(v.X, v.Y)).ToList();
                if (coords.Count < 2) return (null, "", null);

                double avgZ = pl.Vertexes.Average(v => v.Z);
                var line = _geomFactory.CreateLineString(coords.ToArray());
                return (line, "line", (double?)avgZ);
            });
        }

        // Process Points
        foreach (var pt in dxf.Entities.Points)
        {
            ProcessEntity(pt.Layer.Name, () =>
            {
                var coord = new Coordinate(pt.Position.X, pt.Position.Y);
                var point = _geomFactory.CreatePoint(coord);
                double? z = pt.Position.Z != 0 ? pt.Position.Z : null;
                return (point, "point", z);
            });
        }

        // Process Circles
        foreach (var circle in dxf.Entities.Circles)
        {
            ProcessEntity(circle.Layer.Name, () =>
            {
                var center = new Coordinate(circle.Center.X, circle.Center.Y);
                var point = _geomFactory.CreatePoint(center);
                return (point, "point", (double?)null);
            });
        }

        void ProcessEntity(string layerName, Func<(Geometry? geom, string geomType, double? z)> factory)
        {
            if (string.Equals(layerName, "TEXT", StringComparison.OrdinalIgnoreCase))
            {
                return; // Labels handled separately
            }

            if (!mappings.TryGetValue(layerName, out var mapping))
            {
                unmappedSet.Add(layerName);
                return;
            }

            var (geom, geomType, z) = factory();
            if (geom == null || geom.IsEmpty) return;

            // Compute depth from Z (Z is negative in CAD: Z = -1.5 -> depth = 1.5m)
            double? depthM = null;
            if (z.HasValue)
            {
                depthM = z.Value < 0 ? Math.Abs(z.Value) : z.Value;
            }

            // Text attribute parsing near geometry (DN300, DEPTH 1.5m, etc.)
            int? diameterMm = null;
            var centroid = geom.Centroid.Coordinate;
            foreach (var (text, tCoord) in textLabels)
            {
                if (tCoord.Distance(centroid) <= 150)
                {
                    var dnMatch = Regex.Match(text, @"DN\s*(\d+)", RegexOptions.IgnoreCase);
                    if (dnMatch.Success && int.TryParse(dnMatch.Groups[1].Value, out var dn))
                    {
                        diameterMm = dn;
                    }

                    var depthMatch = Regex.Match(text, @"DEPTH\s*([\d\.]+)\s*m", RegexOptions.IgnoreCase);
                    if (depthMatch.Success && double.TryParse(depthMatch.Groups[1].Value, out var parsedDepth))
                    {
                        depthM ??= parsedDepth;
                    }
                }
            }

            // Material parsing from mapping or category
            string? material = null;
            if (mapping.SubType.Contains("Water", StringComparison.OrdinalIgnoreCase)) material = "DI";
            else if (mapping.SubType.Contains("Sewer", StringComparison.OrdinalIgnoreCase)) material = "RCC";

            // Coordinate transformation to EPSG:4326 for display
            var displayGeom = _transformer.Transform(geom, sourceEpsg, 4326);

            var entityDto = new ExtractedEntityDto
            {
                LayerName = layerName,
                Category = mapping.Category,
                SubType = mapping.SubType,
                GeometryType = string.IsNullOrEmpty(mapping.GeometryType) ? geomType : mapping.GeometryType,
                NativeGeometry = geom,
                DisplayGeometry = displayGeom,
                Z = z,
                DepthM = depthM,
                DiameterMm = diameterMm,
                Material = material
            };

            extractedList.Add(entityDto);
        }

        result.UnmappedLayers = unmappedSet.ToList();

        // VALIDATION CHECK 1: If no mapped entities found and unmapped layers exist
        if (extractedList.Count == 0 && unmappedSet.Count > 0)
        {
            result.Success = false;
            result.ErrorCode = "UNMAPPED_LAYER";
            result.ErrorMessage = $"Drawing rejected: contains unmapped layer(s): {string.Join(", ", unmappedSet)}. Please update the layer mapping table first.";
            return result;
        }

        if (extractedList.Count == 0)
        {
            result.Success = false;
            result.ErrorCode = "NO_FEATURES";
            result.ErrorMessage = "No recognizable features found in the drawing.";
            return result;
        }

        // Envelope computation in native coordinates
        var nativeEnv = new Envelope();
        foreach (var entity in extractedList)
        {
            nativeEnv.ExpandToInclude(entity.NativeGeometry.EnvelopeInternal);
        }
        result.NativeExtent = nativeEnv;

        // VALIDATION CHECK 2: Local grid coordinates near (0,0)
        double centerDistance = Math.Sqrt(Math.Pow(nativeEnv.Centre.X, 2) + Math.Pow(nativeEnv.Centre.Y, 2));
        if (centerDistance < 10000.0) // UTM coords are hundreds of thousands of meters
        {
            result.Success = false;
            result.ErrorCode = "INVALID_COORDINATES_LOCAL_GRID";
            result.ErrorMessage = "Drawing coordinates appear to be on a local grid near (0,0) without real-world georeferencing. Projected UTM coordinates required.";
            return result;
        }

        // VALIDATION CHECK 3: Check distance to existing workspace data (or configured center)
        var existingFeatures = await _context.ExtractedFeatures
            .Where(f => f.WorkspaceId == workspaceId && f.IsActive)
            .Select(f => f.NativeGeometry)
            .Take(50)
            .ToListAsync(cancellationToken);

        if (existingFeatures.Count > 0)
        {
            var existingEnv = new Envelope();
            foreach (var ef in existingFeatures)
            {
                existingEnv.ExpandToInclude(ef.EnvelopeInternal);
            }

            var distanceToExisting = nativeEnv.Centre.Distance(existingEnv.Centre);
            if (distanceToExisting > farThreshold)
            {
                result.Success = false;
                result.ErrorCode = "FAR_FROM_PROJECT_AREA";
                result.ErrorMessage = $"Drawing coordinates are {distanceToExisting:F0}m away from the existing workspace project area (exceeding the warning threshold of {farThreshold:F0}m).";
                return result;
            }
        }

        var displayEnv = new Envelope();
        foreach (var entity in extractedList)
        {
            displayEnv.ExpandToInclude(entity.DisplayGeometry.EnvelopeInternal);
        }
        result.DisplayExtent = displayEnv;

        result.Success = true;
        result.Entities = extractedList;
        return result;
    }
}
