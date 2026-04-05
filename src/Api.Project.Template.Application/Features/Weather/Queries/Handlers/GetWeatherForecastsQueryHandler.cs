using Api.Project.Template.Application.Common.Pagination;
using Api.Project.Template.Application.Features.Weather.Services;
using MediatR;

namespace Api.Project.Template.Application.Features.Weather.Queries.Handlers;

public class GetWeatherForecastsQueryHandler : IRequestHandler<GetWeatherForecastsQuery, PagedList<GetWeatherForecastsResponse>>
{
    private readonly IWeatherForecastService _weatherForecastService;

    public GetWeatherForecastsQueryHandler(IWeatherForecastService weatherForecastService)
    {
        _weatherForecastService = weatherForecastService;
    }

    public async Task<PagedList<GetWeatherForecastsResponse>> Handle(
        GetWeatherForecastsQuery request,
        CancellationToken cancellationToken)
    {
        return await _weatherForecastService.GetForecastsAsync(request.PaginationRequest, cancellationToken);
    }
}
