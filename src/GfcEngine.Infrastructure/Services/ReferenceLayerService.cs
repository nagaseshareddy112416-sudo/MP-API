using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NetTopologySuite.IO.Esri;
using GfcEngine.Application.Common.Interfaces;

namespace GfcEngine.Infrastructure.Services;

public class ReferenceLayerService : IReferenceLayerService
{
    private readonly ICrsTransformer _transformer;
    private static readonly GeometryFactory _geomFactory = new(new PrecisionModel(), 4326);

    public ReferenceLayerService(ICrsTransformer transformer)
    {
        _transformer = transformer;
    }

    public async Task<string> ParseToGeoJsonAsync(Stream fileStream, string fileName, int sourceEpsg, CancellationToken cancellationToken = default)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();

        if (ext == ".geojson" || ext == ".json")
        {
            using var reader = new StreamReader(fileStream, Encoding.UTF8);
            return await reader.ReadToEndAsync();
        }
        else if (ext == ".zip")
        {
            // Unpack shapefile from zip in a temporary folder
            var tempDir = Path.Combine(Path.GetTempPath(), $"shp_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);
            try
            {
                using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read);
                archive.ExtractToDirectory(tempDir);

                var shpPath = Directory.GetFiles(tempDir, "*.shp", SearchOption.AllDirectories).FirstOrDefault();
                if (shpPath == null)
                {
                    throw new FileNotFoundException("No .shp file found inside the uploaded zip archive.");
                }

                var featureCollection = new FeatureCollection();
                foreach (var record in Shapefile.ReadAllFeatures(shpPath))
                {
                    var geom = record.Geometry;
                    if (geom != null)
                    {
                        var displayGeom = _transformer.Transform(geom, sourceEpsg, 4326);
                        var attr = new AttributesTable();
                        foreach (var key in record.Attributes.GetNames())
                        {
                            attr.Add(key, record.Attributes[key]);
                        }
                        featureCollection.Add(new Feature(displayGeom, attr));
                    }
                }

                var writer = new GeoJsonWriter();
                return writer.Write(featureCollection);
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
        else if (ext == ".kml")
        {
            // Parse KML coordinates
            using var reader = new StreamReader(fileStream, Encoding.UTF8);
            var xmlContent = await reader.ReadToEndAsync();
            var doc = XDocument.Parse(xmlContent);
            XNamespace ns = "http://www.opengis.net/kml/2.2";

            var featureCollection = new FeatureCollection();

            var placemarks = doc.Descendants(ns + "Placemark");
            foreach (var pm in placemarks)
            {
                var name = pm.Element(ns + "name")?.Value ?? "Feature";
                var coordsElem = pm.Descendants(ns + "coordinates").FirstOrDefault();
                if (coordsElem == null) continue;

                var rawCoords = coordsElem.Value.Trim().Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                var coordsList = new List<Coordinate>();

                foreach (var cStr in rawCoords)
                {
                    var parts = cStr.Split(',');
                    if (parts.Length >= 2 && double.TryParse(parts[0], out var lon) && double.TryParse(parts[1], out var lat))
                    {
                        coordsList.Add(new Coordinate(lon, lat));
                    }
                }

                if (coordsList.Count == 0) continue;

                Geometry geom;
                if (coordsList.Count == 1)
                {
                    geom = _geomFactory.CreatePoint(coordsList[0]);
                }
                else if (coordsList.Count > 3 && coordsList.First().Equals2D(coordsList.Last()))
                {
                    geom = _geomFactory.CreatePolygon(coordsList.ToArray());
                }
                else
                {
                    geom = _geomFactory.CreateLineString(coordsList.ToArray());
                }

                var attr = new AttributesTable { { "name", name } };
                featureCollection.Add(new Feature(geom, attr));
            }

            var writer = new GeoJsonWriter();
            return writer.Write(featureCollection);
        }

        throw new NotSupportedException($"Unsupported reference layer format '{ext}'. Allowed: .geojson, .zip (Shapefile), .kml");
    }
}
