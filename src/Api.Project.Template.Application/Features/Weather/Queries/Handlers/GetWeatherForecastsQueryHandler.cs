using Api.Project.Template.Application.Abstractions;
using Api.Project.Template.Application.Common.Pagination;
using Api.Project.Template.Application.Features.Weather.Events;
using Api.Project.Template.Application.Features.Weather.Specifications;
using Ardalis.Result;
using MediatR;

namespace Api.Project.Template.Application.Features.Weather.Queries.Handlers;

public class GetWeatherForecastsQueryHandler(IRepository repository, IPublisher publisher)
    : IRequestHandler<GetWeatherForecastsQuery, Result<PagedList<GetWeatherForecastsResponse>>>
{
    public async Task<Result<PagedList<GetWeatherForecastsResponse>>> Handle(
        GetWeatherForecastsQuery request,
        CancellationToken cancellationToken)
    {
        var spec = new WeatherForecastFilterSpecification(request.PaginationRequest);
        var forecasts = await repository.ListAsync(spec, cancellationToken);

        // Contrived example to demonstrate MediatR's publish/subscribe capabilities.
        // In a real application, you might want to publish an event when a new weather forecast is created or updated,
        // rather than when forecasts are requested.
        await publisher.Publish(
            new WeatherForecastRequestedEvent(
                request.PaginationRequest.Page,
                request.PaginationRequest.Size,
                DateTimeOffset.UtcNow),
            cancellationToken);

        return Result.Success(forecasts);
    }
}
