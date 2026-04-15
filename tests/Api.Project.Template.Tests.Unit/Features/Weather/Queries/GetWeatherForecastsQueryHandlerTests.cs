using Api.Project.Template.Application.Abstractions;
using Api.Project.Template.Application.Common.Pagination;
using Api.Project.Template.Application.Common.Specifications;
using Api.Project.Template.Application.Features.Weather.Queries;
using Api.Project.Template.Application.Features.Weather.Queries.Handlers;
using Api.Project.Template.Domain.Entities;
using Ardalis.Result;
using FluentAssertions;
using MediatR;
using Moq;

namespace Api.Project.Template.Tests.Unit.Features.Weather.Queries;

public class GetWeatherForecastsQueryHandlerTests
{
    private readonly Mock<IRepository> _repositoryMock;
    private readonly Mock<IPublisher> _publisherMock;
    private readonly GetWeatherForecastsQueryHandler _handler;

    public GetWeatherForecastsQueryHandlerTests()
    {
        _repositoryMock = new Mock<IRepository>();
        _publisherMock = new Mock<IPublisher>();
        _handler = new GetWeatherForecastsQueryHandler(_repositoryMock.Object, _publisherMock.Object);
    }

    [Fact]
    public async Task Handle_WhenRepositoryReturnsForecasts_ReturnsMappedResponses()
    {
        // Arrange
        var request = new PaginationRequest { Page = 1, Size = 10 };
        var responses = new List<GetWeatherForecastsResponse>
        {
            new(new DateOnly(2025, 6, 1), 20, 68, "Warm"),
            new(new DateOnly(2025, 6, 2), -5, 23, "Freezing")
        };
        var paged = new PagedList<GetWeatherForecastsResponse>(responses, responses.Count, 1, 10);

        _repositoryMock
            .Setup(r => r.ListAsync(
                It.IsAny<PaginatedSpecification<WeatherForecast, GetWeatherForecastsResponse>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);

        // Act
        var result = await _handler.Handle(new GetWeatherForecastsQuery(request), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Data.Should().HaveCount(2);

        var data = result.Value.Data.ToList();
        data[0].Date.Should().Be(new DateOnly(2025, 6, 1));
        data[0].TemperatureC.Should().Be(20);
        data[0].TemperatureF.Should().Be(68);
        data[0].Summary.Should().Be("Warm");

        data[1].Date.Should().Be(new DateOnly(2025, 6, 2));
        data[1].TemperatureC.Should().Be(-5);
        data[1].Summary.Should().Be("Freezing");
    }

    [Fact]
    public async Task Handle_WhenRepositoryReturnsNoForecasts_ReturnsEmptyCollection()
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
        var result = await _handler.Handle(new GetWeatherForecastsQuery(request), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenCalled_InvokesRepositoryExactlyOnce()
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
        await _handler.Handle(new GetWeatherForecastsQuery(request), CancellationToken.None);

        // Assert
        _repositoryMock.Verify(r => r.ListAsync(
            It.IsAny<PaginatedSpecification<WeatherForecast, GetWeatherForecastsResponse>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
