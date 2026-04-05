using Api.Project.Template.Application.Abstractions;
using Api.Project.Template.Application.Common.Pagination;
using Api.Project.Template.Application.Common.Specifications;
using Api.Project.Template.Application.Features.Weather.Queries;
using Api.Project.Template.Application.Features.Weather.Services;
using Api.Project.Template.Domain.Entities;
using FluentAssertions;
using Moq;

namespace Api.Project.Template.Tests.Unit.Features.Weather.Services;

public class WeatherForecastServiceTests
{
    private readonly Mock<IRepository> _repositoryMock;
    private readonly WeatherForecastService _service;

    public WeatherForecastServiceTests()
    {
        _repositoryMock = new Mock<IRepository>();
        _service = new WeatherForecastService(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetForecastsAsync_WhenCalled_DelegatesToRepository()
    {
        // Arrange
        var request = new PaginationRequest();
        var empty = new PagedList<GetWeatherForecastsResponse>([], 0, 1, 10);

        _repositoryMock
            .Setup(r => r.ListAsync(
                It.IsAny<PaginatedSpecification<WeatherForecast, GetWeatherForecastsResponse>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(empty);

        // Act
        await _service.GetForecastsAsync(request, CancellationToken.None);

        // Assert
        _repositoryMock.Verify(
            r => r.ListAsync(
                It.IsAny<PaginatedSpecification<WeatherForecast, GetWeatherForecastsResponse>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetForecastsAsync_WhenRepositoryReturnsForecasts_ReturnsForecasts()
    {
        // Arrange
        var request = new PaginationRequest();
        var responses = new List<GetWeatherForecastsResponse>
        {
            new(new DateOnly(2025, 6, 1), 20, 68, "Warm"),
            new(new DateOnly(2025, 6, 2), 10, 50, "Cool")
        };
        var paged = new PagedList<GetWeatherForecastsResponse>(responses, responses.Count, 1, 10);

        _repositoryMock
            .Setup(r => r.ListAsync(
                It.IsAny<PaginatedSpecification<WeatherForecast, GetWeatherForecastsResponse>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);

        // Act
        var result = await _service.GetForecastsAsync(request, CancellationToken.None);

        // Assert
        result.Data.Should().BeEquivalentTo(responses);
    }

    [Fact]
    public async Task GetForecastsAsync_WhenRepositoryReturnsEmpty_ReturnsEmptyCollection()
    {
        // Arrange
        var request = new PaginationRequest();
        var empty = new PagedList<GetWeatherForecastsResponse>([], 0, 1, 10);

        _repositoryMock
            .Setup(r => r.ListAsync(
                It.IsAny<PaginatedSpecification<WeatherForecast, GetWeatherForecastsResponse>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(empty);

        // Act
        var result = await _service.GetForecastsAsync(request, CancellationToken.None);

        // Assert
        result.Data.Should().BeEmpty();
    }
}
