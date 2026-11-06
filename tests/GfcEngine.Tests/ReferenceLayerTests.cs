using System.Text;
using FluentAssertions;
using GfcEngine.Infrastructure.Services;
using Xunit;

namespace GfcEngine.Tests;

public class ReferenceLayerTests
{
    private readonly ProjNetCrsTransformer _transformer = new();

    [Fact]
    public async Task ParseGeoJson_ReturnsUnchangedGeoJsonString()
    {
        // Arrange
        var service = new ReferenceLayerService(_transformer);
        var json = "{\"type\":\"FeatureCollection\",\"features\":[{\"type\":\"Feature\",\"geometry\":{\"type\":\"Point\",\"coordinates\":[80.516,16.513]},\"properties\":{\"name\":\"Master Plan Center\"}}]}";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        // Act
        var result = await service.ParseToGeoJsonAsync(stream, "masterplan.geojson", 32644);

        // Assert
        result.Should().Be(json);
    }

    [Fact]
    public async Task ParseKml_ExtractsCoordinatesAndGeneratesGeoJson()
    {
        // Arrange
        var service = new ReferenceLayerService(_transformer);
        var kml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<kml xmlns=""http://www.opengis.net/kml/2.2"">
  <Document>
    <Placemark>
      <name>Corridor Boundary</name>
      <LineString>
        <coordinates>
          80.516,16.513,0 80.520,16.515,0 80.525,16.518,0
        </coordinates>
      </LineString>
    </Placemark>
  </Document>
</kml>";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kml));

        // Act
        var result = await service.ParseToGeoJsonAsync(stream, "boundary.kml", 32644);

        // Assert
        result.Should().Contain("LineString");
        result.Should().Contain("80.516");
        result.Should().Contain("Corridor Boundary");
    }
}
