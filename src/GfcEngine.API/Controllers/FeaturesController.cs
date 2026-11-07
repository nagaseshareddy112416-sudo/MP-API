using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using GfcEngine.Application.Common.Interfaces;
using GfcEngine.Infrastructure.Persistence;

namespace GfcEngine.API.Controllers;

[ApiController]
[Route("api/workspaces/{workspaceId}/features")]
[Authorize]
public class FeaturesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ICrsTransformer _transformer;

    public FeaturesController(ApplicationDbContext context, ICrsTransformer transformer)
    {
        _context = context;
        _transformer = transformer;
    }

    [HttpGet]
    public async Task<IActionResult> GetFeatures(
        Guid workspaceId,
        [FromQuery] string? bbox,
        [FromQuery] string? category,
        [FromQuery] bool? isAsBuilt,
        [FromQuery] int? zoom)
    {
        var query = _context.ExtractedFeatures
            .Where(f => f.WorkspaceId == workspaceId && f.IsActive);

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(f => f.Category == category);
        }

        if (isAsBuilt.HasValue)
        {
            query = query.Where(f => f.IsAsBuilt == isAsBuilt.Value);
        }

        // Bounding box filter (in display WGS84 coords: minLon, minLat, maxLon, maxLat)
        if (!string.IsNullOrEmpty(bbox))
        {
            var parts = bbox.Split(',').Select(double.Parse).ToArray();
            if (parts.Length == 4)
            {
                var env = new Envelope(parts[0], parts[2], parts[1], parts[3]);
                var bboxGeom = new GeometryFactory(new PrecisionModel(), 4326).ToGeometry(env);
                query = query.Where(f => f.DisplayGeometry.Intersects(bboxGeom));
            }
        }

        var features = await query
            .Include(f => f.Drawing)
            .Take(1000)
            .ToListAsync();

        var featureCollection = new FeatureCollection();
        foreach (var f in features)
        {
            var attr = new AttributesTable
            {
                { "id", f.Id },
                { "drawingId", f.DrawingId },
                { "projectName", f.Drawing?.ProjectName ?? "" },
                { "layer", f.CadLayer },
                { "category", f.Category },
                { "subType", f.SubType },
                { "geometryType", f.GeometryType },
                { "depth", f.DepthM },
                { "diameter", f.DiameterMm },
                { "material", f.Material },
                { "chainage", f.Chainage },
                { "revision", f.Drawing?.CurrentRevision ?? "" },
                { "isAsBuilt", f.IsAsBuilt },
                { "stageName", f.Drawing?.StageName ?? "" },
                { "constructionStatus", f.ConstructionStatus.ToString() }
            };

            featureCollection.Add(new Feature(f.DisplayGeometry, attr));
        }

        var writer = new GeoJsonWriter();
        var json = writer.Write(featureCollection);

        return Content(json, "application/json");
    }

    [HttpGet("export-geojson")]
    public async Task<IActionResult> ExportGeoJson(Guid workspaceId, [FromQuery] string? category)
    {
        var query = _context.ExtractedFeatures
            .Where(f => f.WorkspaceId == workspaceId && f.IsActive);

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(f => f.Category == category);
        }

        var features = await query.Include(f => f.Drawing).ToListAsync();
        var featureCollection = new FeatureCollection();

        foreach (var f in features)
        {
            var attr = new AttributesTable
            {
                { "id", f.Id },
                { "projectName", f.Drawing?.ProjectName ?? "" },
                { "layer", f.CadLayer },
                { "category", f.Category },
                { "subType", f.SubType },
                { "geometryType", f.GeometryType },
                { "depth", f.DepthM },
                { "diameter", f.DiameterMm },
                { "material", f.Material },
                { "chainage", f.Chainage }
            };

            featureCollection.Add(new Feature(f.DisplayGeometry, attr));
        }

        var writer = new GeoJsonWriter();
        var json = writer.Write(featureCollection);
        var bytes = Encoding.UTF8.GetBytes(json);

        return File(bytes, "application/geo+json", $"workspace_{workspaceId}_features.geojson");
    }
}

