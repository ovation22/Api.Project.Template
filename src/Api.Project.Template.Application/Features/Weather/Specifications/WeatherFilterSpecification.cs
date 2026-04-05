using Api.Project.Template.Application.Common.Pagination;
using Api.Project.Template.Application.Common.Specifications;
using Api.Project.Template.Application.Features.Weather.Queries;
using Api.Project.Template.Domain.Entities;
using Ardalis.Specification;

namespace Api.Project.Template.Application.Features.Weather.Specifications;

public sealed class WeatherForecastFilterSpecification : FilterSpecification<WeatherForecast, GetWeatherForecastsResponse>
{
    private static readonly Dictionary<string, string> Mappings = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Summary", "Summary.Name" }
    };

    public WeatherForecastFilterSpecification(PaginationRequest request)
        : base(request, Mappings, defaultSortBy: "Date", defaultSortDirection: SortDirection.Asc)
    {
        Query.Include(x => x.Summary);

        Query.Select(x => new GetWeatherForecastsResponse(
            x.Date,
            x.TemperatureC,
            x.TemperatureF,
            x.Summary.Name
        ));
    }
}
