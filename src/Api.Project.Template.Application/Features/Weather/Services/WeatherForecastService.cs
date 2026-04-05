using Api.Project.Template.Application.Abstractions;
using Api.Project.Template.Application.Common.Pagination;
using Api.Project.Template.Application.Features.Weather.Queries;
using Api.Project.Template.Application.Features.Weather.Specifications;
using Api.Project.Template.Domain.Entities;
using MediatR;

namespace Api.Project.Template.Application.Features.Weather.Services;

public class WeatherForecastService(IRepository repository) : IWeatherForecastService
{
    public async Task<PagedList<GetWeatherForecastsResponse>> GetForecastsAsync(PaginationRequest request, CancellationToken cancellationToken)
    {
        var spec = new WeatherForecastFilterSpecification(request);

        return await repository.ListAsync(spec, cancellationToken);
    }
}
