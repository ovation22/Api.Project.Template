using Api.Project.Template.Application.Common.Pagination;
using MediatR;

namespace Api.Project.Template.Application.Features.Weather.Queries;

public record GetWeatherForecastsQuery(PaginationRequest PaginationRequest) : IRequest<PagedList<GetWeatherForecastsResponse>>;
