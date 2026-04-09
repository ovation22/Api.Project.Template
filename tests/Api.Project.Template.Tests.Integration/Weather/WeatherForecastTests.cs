using System.Net;
using System.Net.Http.Json;
using Api.Project.Template.Application.Features.Weather.Queries;
using Api.Project.Template.Infrastructure.Data;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Project.Template.Tests.Integration.Weather;

/// <summary>
/// Minimal shape matching the JSON returned by PagedList&lt;T&gt;.
/// </summary>
/// <typeparam name="T">Item type.</typeparam>
file record PagedResponse<T>(int Total, int PageNumber, IEnumerable<T> Data);

public class WeatherForecastTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public WeatherForecastTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();

        // Ensure schema and seed data are applied once per factory lifetime.
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApiProjectTemplateContext>();
        db.Database.EnsureCreated();
    }

    [Fact]
    [Trait("Category", "Baseline")]
    public async Task Get_ReturnsOkStatusCode()
    {
        // Arrange

        // Act
        var response = await _client.GetAsync("/weatherforecast", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Category", "Baseline")]
    public async Task Get_ReturnsAllSeededForecasts()
    {
        // Arrange

        // Act
        // Request all 50 seeded records in a single page.
        var result = await _client.GetFromJsonAsync<PagedResponse<GetWeatherForecastsResponse>>(
            "/weatherforecast?size=50",
            TestContext.Current.CancellationToken);

        // Assert
        // 10 summaries × 5 entries each = 50 seeded records.
        result!.Total.Should().Be(50);
        result.Data.Should().HaveCount(50);
    }

    [Fact]
    [Trait("Category", "Baseline")]
    public async Task Get_ReturnsForecastsWithCorrectShape()
    {
        // Arrange

        // Act
        var result = await _client.GetFromJsonAsync<PagedResponse<GetWeatherForecastsResponse>>(
            "/weatherforecast?size=50",
            TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.Data.Should().AllSatisfy(f =>
        {
            f.Date.Should().NotBe(default);
            f.Summary.Should().NotBeNullOrWhiteSpace();
            f.TemperatureC.Should().BeInRange(-20, 45);
            f.TemperatureF.Should().Be(32 + (int)(f.TemperatureC / 0.5556));
        });
    }

    [Fact]
    [Trait("Category", "Pagination")]
    public async Task Get_Paginated_ReturnsCorrectPageSize()
    {
        // Arrange

        // Act
        var result = await _client.GetFromJsonAsync<PagedResponse<GetWeatherForecastsResponse>>(
            "/weatherforecast?page=1&size=10",
            TestContext.Current.CancellationToken);

        // Assert
        result!.Total.Should().Be(50);
        result.Data.Should().HaveCount(10);
    }

    [Fact]
    [Trait("Category", "Filters")]
    public async Task Get_FilterByDate_GreaterThan_ReturnsOnlyMatchingForecasts()
    {
        // Arrange
        // All seeded dates are in Jan 2025, so filtering after 2024-01-01 returns all 50.

        // Act
        var result = await _client.GetFromJsonAsync<PagedResponse<GetWeatherForecastsResponse>>(
            "/weatherforecast?size=50&Filters[Date].Operator=Gt&Filters[Date].Value=2024-01-01",
            TestContext.Current.CancellationToken);

        // Assert
        result!.Total.Should().Be(50);
        result.Data.Should().AllSatisfy(f => f.Date.Should().BeOnOrAfter(new DateOnly(2024, 1, 2)));
    }

    [Fact]
    [Trait("Category", "Filters")]
    public async Task Get_FilterByDate_GreaterThan_FiltersOutEarlierRecords()
    {
        // Arrange
        // Only date 2025-01-05 is after 2025-01-04 → 10 records (one per summary).

        // Act
        var result = await _client.GetFromJsonAsync<PagedResponse<GetWeatherForecastsResponse>>(
            "/weatherforecast?size=50&Filters[Date].Operator=Gt&Filters[Date].Value=2025-01-04",
            TestContext.Current.CancellationToken);

        // Assert
        result!.Total.Should().Be(10);
        result.Data.Should().AllSatisfy(f => f.Date.Should().BeOnOrAfter(new DateOnly(2025, 1, 5)));
    }

    [Fact]
    [Trait("Category", "Filters")]
    public async Task Get_FilterByTemperatureC_GreaterThan_ReturnsOnlyMatchingForecasts()
    {
        // Arrange

        // Act
        var result = await _client.GetFromJsonAsync<PagedResponse<GetWeatherForecastsResponse>>(
            "/weatherforecast?size=50&Filters[TemperatureC].Operator=Gt&Filters[TemperatureC].Value=20",
            TestContext.Current.CancellationToken);

        // Assert
        result!.Total.Should().BeGreaterThan(0);
        result.Data.Should().AllSatisfy(f => f.TemperatureC.Should().BeGreaterThan(20));
    }

    [Fact]
    [Trait("Category", "Filters")]
    public async Task Get_FilterByTemperatureC_LessThan_ReturnsOnlyMatchingForecasts()
    {
        // Arrange
        // Only Freezing records have TemperatureC < 0 (range -20 to -1).

        // Act
        var result = await _client.GetFromJsonAsync<PagedResponse<GetWeatherForecastsResponse>>(
            "/weatherforecast?size=50&Filters[TemperatureC].Operator=Lt&Filters[TemperatureC].Value=0",
            TestContext.Current.CancellationToken);

        // Assert
        result!.Total.Should().BeGreaterThan(0);
        result.Data.Should().AllSatisfy(f => f.TemperatureC.Should().BeLessThan(0));
    }

    [Fact]
    [Trait("Category", "Filters")]
    public async Task Get_FilterBySummary_Contains_ReturnsOnlyMatchingForecasts()
    {
        // Arrange
        // Summaries containing "ing": Freezing, Bracing, Sweltering, Scorching → 4 × 5 = 20 records.

        // Act
        var result = await _client.GetFromJsonAsync<PagedResponse<GetWeatherForecastsResponse>>(
            "/weatherforecast?size=50&Filters[Summary].Operator=Contains&Filters[Summary].Value=ing",
            TestContext.Current.CancellationToken);

        // Assert
        result!.Total.Should().Be(20);
        result.Data.Should().AllSatisfy(f =>
            f.Summary.Should().ContainEquivalentOf("ing"));
    }

    [Fact]
    [Trait("Category", "Filters")]
    public async Task Get_FilterBySummary_Equals_ReturnsOnlyMatchingForecasts()
    {
        // Arrange
        // Exact match "Warm" → 5 records.

        // Act
        var result = await _client.GetFromJsonAsync<PagedResponse<GetWeatherForecastsResponse>>(
            "/weatherforecast?size=50&Filters[Summary].Operator=Eq&Filters[Summary].Value=Warm",
            TestContext.Current.CancellationToken);

        // Assert
        result!.Total.Should().Be(5);
        result.Data.Should().AllSatisfy(f => f.Summary.Should().Be("Warm"));
    }

    [Fact]
    [Trait("Category", "Filters")]
    public async Task Get_FilterAndTemperatureCAndSummary_ReturnsOnlyRecordsSatisfyingBothConditions()
    {
        // Arrange
        // TemperatureC > 20 AND Summary contains "arm" (matches "Warm").
        // Warm range is 20-24 inclusive via random; TemperatureC > 20 narrows to 21-24.

        // Act
        var result = await _client.GetFromJsonAsync<PagedResponse<GetWeatherForecastsResponse>>(
            "/weatherforecast?size=50&Operator=And" +
            "&Filters[TemperatureC].Operator=Gt&Filters[TemperatureC].Value=20" +
            "&Filters[Summary].Operator=Contains&Filters[Summary].Value=arm",
            TestContext.Current.CancellationToken);

        // Assert
        result!.Total.Should().BeGreaterThanOrEqualTo(0);
        result.Data.Should().AllSatisfy(f =>
        {
            f.TemperatureC.Should().BeGreaterThan(20);
            f.Summary.Should().ContainEquivalentOf("arm");
        });
    }

    [Fact]
    [Trait("Category", "Filters")]
    public async Task Get_FilterOrTemperatureCOrSummary_ReturnsRecordsSatisfyingEitherCondition()
    {
        // Arrange
        // TemperatureC < 0 OR Summary contains "zing" (matches "Freezing").
        // All Freezing records have TemperatureC < 0, so both conditions overlap.

        // Act
        var result = await _client.GetFromJsonAsync<PagedResponse<GetWeatherForecastsResponse>>(
            "/weatherforecast?size=50&Operator=Or" +
            "&Filters[TemperatureC].Operator=Lt&Filters[TemperatureC].Value=0" +
            "&Filters[Summary].Operator=Contains&Filters[Summary].Value=zing",
            TestContext.Current.CancellationToken);

        // Assert
        result!.Total.Should().BeGreaterThan(0);
        result.Data.Should().AllSatisfy(f =>
            (f.TemperatureC < 0 || f.Summary.Contains("zing", StringComparison.OrdinalIgnoreCase))
                .Should().BeTrue());
    }
}
