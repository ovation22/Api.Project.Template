using Api.Project.Template.Application.Common.Pagination;
using Api.Project.Template.Application.Features.Weather.Queries;
using Api.Project.Template.Domain.Entities;

namespace Api.Project.Template.Application.Features.Weather.Services;

public interface IWeatherForecastService
{
    Task<PagedList<GetWeatherForecastsResponse>> GetForecastsAsync(PaginationRequest request, CancellationToken cancellationToken);
}
