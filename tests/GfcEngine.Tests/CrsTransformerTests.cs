using FluentAssertions;
using NetTopologySuite.Geometries;
using GfcEngine.Infrastructure.Services;
using Xunit;

namespace GfcEngine.Tests;

public class CrsTransformerTests
{
    private readonly ProjNetCrsTransformer _transformer = new();

    [Fact]
    public void TransformPoint_FromUtm44N_ToWgs84_ProducesAccurateCoordinates()
    {
        // Arrange
        // UTM Zone 44N origin test point near Amaravati: 448350, 1825744
        double utmX = 448350;
        double utmY = 1825744;

        // Act
        var result = _transformer.Transform(utmX, utmY, 32644, 4326);

        // Assert - Expected around lon 80.516, lat 16.513
        result.X.Should().BeApproximately(80.516, 0.01);
        result.Y.Should().BeApproximately(16.513, 0.01);
    }

    [Fact]
    public void TransformGeometry_LineString_PreservesVertices()
    {
        // Arrange
        var factory = new GeometryFactory();
        var line = factory.CreateLineString(new[]
        {
            new Coordinate(448350, 1825744),
            new Coordinate(449350, 1825744)
        });

        // Act
        var transformed = _transformer.Transform(line, 32644, 4326);

        // Assert
        transformed.Should().NotBeNull();
        transformed.Coordinates.Length.Should().Be(2);
        transformed.Coordinates[0].X.Should().BeApproximately(80.516, 0.01);
    }
}

