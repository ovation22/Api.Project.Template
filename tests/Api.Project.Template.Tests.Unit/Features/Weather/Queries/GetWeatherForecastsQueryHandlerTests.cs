using Api.Project.Template.Application.Common.Pagination;
using Api.Project.Template.Application.Features.Weather.Queries;
using Api.Project.Template.Application.Features.Weather.Queries.Handlers;
using Api.Project.Template.Application.Features.Weather.Services;
using FluentAssertions;
using Moq;

namespace Api.Project.Template.Tests.Unit.Features.Weather.Queries;

public class GetWeatherForecastsQueryHandlerTests
{
    private readonly Mock<IWeatherForecastService> _serviceMock;
    private readonly GetWeatherForecastsQueryHandler _handler;

    public GetWeatherForecastsQueryHandlerTests()
    {
        _serviceMock = new Mock<IWeatherForecastService>();
        _handler = new GetWeatherForecastsQueryHandler(_serviceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenServiceReturnsForecasts_ReturnsMappedResponses()
    {
        // Arrange
        var request = new PaginationRequest { Page = 1, Size = 10 };
        var responses = new List<GetWeatherForecastsResponse>
        {
            new(new DateOnly(2025, 6, 1), 20, 68, "Warm"),
            new(new DateOnly(2025, 6, 2), -5, 23, "Freezing")
        };
        var paged = new PagedList<GetWeatherForecastsResponse>(responses, responses.Count, 1, 10);

        _serviceMock
            .Setup(s => s.GetForecastsAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);

        // Act
        var result = await _handler.Handle(new GetWeatherForecastsQuery(request), CancellationToken.None);

        // Assert
        result.Data.Should().HaveCount(2);

        var data = result.Data.ToList();
        data[0].Date.Should().Be(new DateOnly(2025, 6, 1));
        data[0].TemperatureC.Should().Be(20);
        data[0].TemperatureF.Should().Be(68);
        data[0].Summary.Should().Be("Warm");

        data[1].Date.Should().Be(new DateOnly(2025, 6, 2));
        data[1].TemperatureC.Should().Be(-5);
        data[1].Summary.Should().Be("Freezing");
    }

    [Fact]
    public async Task Handle_WhenServiceReturnsNoForecasts_ReturnsEmptyCollection()
    {
        // Arrange
        var request = new PaginationRequest();
        var empty = new PagedList<GetWeatherForecastsResponse>([], 0, 1, 10);

        _serviceMock
            .Setup(s => s.GetForecastsAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(empty);

        // Act
        var result = await _handler.Handle(new GetWeatherForecastsQuery(request), CancellationToken.None);

        // Assert
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenCalled_InvokesServiceExactlyOnce()
    {
        // Arrange
        var request = new PaginationRequest();
        var empty = new PagedList<GetWeatherForecastsResponse>([], 0, 1, 10);

        _serviceMock
            .Setup(s => s.GetForecastsAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(empty);

        // Act
        await _handler.Handle(new GetWeatherForecastsQuery(request), CancellationToken.None);

        // Assert
        _serviceMock.Verify(s => s.GetForecastsAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }
}
