using System.Net;
using Api.Project.Template.Api;
using Xunit;

namespace Api.Project.Template.Tests.Integration.Api;

public class WeatherForecastTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public WeatherForecastTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ItShouldReturnOk()
    {
        // Arrange
        var httpClient = _factory.CreateClient();

        // Act
        var response = await httpClient.GetAsync("WeatherForecast");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
