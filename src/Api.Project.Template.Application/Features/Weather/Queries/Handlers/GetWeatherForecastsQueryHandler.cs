using Api.Project.Template.Application.Abstractions;
using Api.Project.Template.Application.Common.Pagination;
using Api.Project.Template.Application.Features.Weather.Specifications;
using MediatR;

namespace Api.Project.Template.Application.Features.Weather.Queries.Handlers;

public class GetWeatherForecastsQueryHandler(IRepository repository)
    : IRequestHandler<GetWeatherForecastsQuery, PagedList<GetWeatherForecastsResponse>>
{
    public async Task<PagedList<GetWeatherForecastsResponse>> Handle(
        GetWeatherForecastsQuery request,
        CancellationToken cancellationToken)
    {
        var spec = new WeatherForecastFilterSpecification(request.PaginationRequest);
        return await repository.ListAsync(spec, cancellationToken);
    }
}
