using Api.Project.Template.Application.Common.Pagination;
using Ardalis.Result;
using MediatR;

namespace Api.Project.Template.Application.Features.Weather.Queries;

public record GetWeatherForecastsQuery(PaginationRequest PaginationRequest) : IRequest<Result<PagedList<GetWeatherForecastsResponse>>>;
