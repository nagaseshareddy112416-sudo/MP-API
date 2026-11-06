using NetTopologySuite.Geometries;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using GfcEngine.Application.Common.Interfaces;

namespace GfcEngine.Infrastructure.Services;

public class ProjNetCrsTransformer : ICrsTransformer
{
    private readonly CoordinateTransformationFactory _ctFactory = new();
    private readonly Dictionary<(int from, int to), ICoordinateTransformation> _transforms = new();
    private readonly object _lock = new();

    public Coordinate Transform(double x, double y, int fromEpsg, int toEpsg)
    {
        if (fromEpsg == toEpsg)
        {
            return new Coordinate(x, y);
        }

        var transform = GetOrCreateTransform(fromEpsg, toEpsg);
        var result = transform.MathTransform.Transform(new double[] { x, y });
        return new Coordinate(result[0], result[1]);
    }

    public Geometry Transform(Geometry geometry, int fromEpsg, int toEpsg)
    {
        if (fromEpsg == toEpsg || geometry == null)
        {
            return geometry!;
        }

        var geomCopy = (Geometry)geometry.Copy();
        var transform = GetOrCreateTransform(fromEpsg, toEpsg);

        geomCopy.Apply((ICoordinateSequenceFilter)new CoordinateTransformFilter(transform));
        geomCopy.SRID = toEpsg;
        return geomCopy;
    }

    private ICoordinateTransformation GetOrCreateTransform(int fromEpsg, int toEpsg)
    {
        lock (_lock)
        {
            if (_transforms.TryGetValue((fromEpsg, toEpsg), out var transform))
            {
                return transform;
            }

            var fromCs = GetCoordinateSystem(fromEpsg);
            var toCs = GetCoordinateSystem(toEpsg);

            transform = _ctFactory.CreateFromCoordinateSystems(fromCs, toCs);
            _transforms[(fromEpsg, toEpsg)] = transform;
            return transform;
        }
    }

    private static CoordinateSystem GetCoordinateSystem(int epsg)
    {
        if (epsg == 4326)
        {
            return GeographicCoordinateSystem.WGS84;
        }
        else if (epsg == 32644)
        {
            // WGS 84 / UTM zone 44N (Central Meridian: 81 deg E, Lat of Origin: 0, False Easting: 500000, False Northing: 0)
            return ProjectedCoordinateSystem.WGS84_UTM(44, true);
        }
        else if (epsg >= 32601 && epsg <= 32660)
        {
            int zone = epsg - 32600;
            return ProjectedCoordinateSystem.WGS84_UTM(zone, true);
        }
        else if (epsg >= 32701 && epsg <= 32760)
        {
            int zone = epsg - 32700;
            return ProjectedCoordinateSystem.WGS84_UTM(zone, false);
        }

        // Fallback for generic projected or geographic
        throw new NotSupportedException($"EPSG:{epsg} is not currently supported for transformation.");
    }

    private class CoordinateTransformFilter : ICoordinateSequenceFilter
    {
        private readonly ICoordinateTransformation _transformation;

        public CoordinateTransformFilter(ICoordinateTransformation transformation)
        {
            _transformation = transformation;
        }

        public bool Done => false;
        public bool GeometryChanged => true;

        public void Filter(CoordinateSequence seq, int i)
        {
            var x = seq.GetX(i);
            var y = seq.GetY(i);
            var res = _transformation.MathTransform.Transform(new double[] { x, y });
            seq.SetX(i, res[0]);
            seq.SetY(i, res[1]);
        }
    }
}
