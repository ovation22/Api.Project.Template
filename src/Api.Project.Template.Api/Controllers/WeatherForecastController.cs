using Api.Project.Template.Api.Conventions;
using Api.Project.Template.Application.Common.Pagination;
using Api.Project.Template.Application.Features.Weather.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Project.Template.Api.Controllers;

[ApiController]
[Route("[controller]")]
[ApiConventionType(typeof(ApiConventions))]
public class WeatherForecastController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Returns a paginated and optionally filtered list of weather forecasts.
    /// </summary>
    /// <param name="request">Pagination, sorting and filter parameters (from query string).</param>
    /// <param name="cancellationToken"></param>
    /// <returns>200 with <see cref="PagedList{GetWeatherForecastsResponse}"/>; 400 on failure.</returns>
    /// <remarks>
    /// Example URLs (copy the full URL to test):
    /// <code>
    /// /WeatherForecast?page=1&amp;size=10&amp;Filters[Date].Operator=Gt&amp;Filters[Date].Value=2024-01-01
    /// /WeatherForecast?page=1&amp;size=10&amp;Filters[TemperatureC].Operator=Gt&amp;Filters[TemperatureC].Value=20
    /// /WeatherForecast?page=1&amp;size=10&amp;Filters[Summary].Operator=Contains&amp;Filters[Summary].Value=Sunny
    /// /WeatherForecast?page=1&amp;size=10&amp;Operator=And&amp;Filters[TemperatureC].Operator=Gt&amp;Filters[TemperatureC].Value=20&amp;Filters[Summary].Operator=Contains&amp;Filters[Summary].Value=Sunny
    /// /WeatherForecast?page=1&amp;size=10&amp;Operator=Or&amp;Filters[TemperatureC].Operator=Lt&amp;Filters[TemperatureC].Value=0&amp;Filters[Summary].Operator=Contains&amp;Filters[Summary].Value=Snow
    /// </code>
    /// 
    /// In Scalar, add each filter as a separate query parameter row:
    /// <list>
    ///   <listheader><term>Parameter name</term><description>Value</description></listheader>
    ///   <item><term>Operator</term><description>And</description></item>
    ///   <item><term>Filters[TemperatureC].Operator</term><description>Gt</description></item>
    ///   <item><term>Filters[TemperatureC].Value</term><description>20</description></item>
    ///   <item><term>Filters[Summary].Operator</term><description>Contains</description></item>
    ///   <item><term>Filters[Summary].Value</term><description>Warm</description></item>
    /// </list>
    /// 
    /// Available FilterOperators: Eq, NotEq, Gt, Gte, Lt, Lte, Contains, StartsWith, EndsWith, Between
    /// </remarks>
    /// <response code="200">Returns the paged weather forecasts result.</response>
    /// <response code="400">Unable to return weather forecasts.</response>
    [HttpGet(Name = "GetWeatherForecast")]
    [ProducesDefaultResponseType]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedList<GetWeatherForecastsResponse>>> Filter([FromQuery] PaginationRequest request, CancellationToken cancellationToken)
    {
        return await sender.Send(new GetWeatherForecastsQuery(request), cancellationToken);
    }

    /// <summary>
    /// Gets a single weather forecast by ID.
    /// </summary>
    /// <param name="id">The ID of the weather forecast to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 with the weather forecast; 404 if not found.</returns>
    /// <response code="200">Returns the weather forecast.</response>
    /// <response code="404">Weather forecast not found.</response>
    /// <response code="501">Not implemented.</response>
    [HttpGet("{id}", Name = "GetWeatherForecastById")]
    [ProducesDefaultResponseType]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return StatusCode(StatusCodes.Status501NotImplemented);
    }

    /// <summary>
    /// Creates a new weather forecast.
    /// </summary>
    /// <param name="request">The weather forecast to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>201 with the created forecast; 400 on validation failure.</returns>
    /// <response code="201">Weather forecast created successfully.</response>
    /// <response code="400">Invalid request data.</response>
    /// <response code="501">Not implemented.</response>
    [HttpPost(Name = "CreateWeatherForecast")]
    [ProducesDefaultResponseType]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public async Task<IActionResult> Create([FromBody] object request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return StatusCode(StatusCodes.Status501NotImplemented);
    }

    /// <summary>
    /// Updates an existing weather forecast.
    /// </summary>
    /// <param name="id">The ID of the weather forecast to update.</param>
    /// <param name="request">The updated weather forecast data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 on success; 404 if not found; 400 on validation failure.</returns>
    /// <response code="200">Weather forecast updated successfully.</response>
    /// <response code="400">Invalid request data.</response>
    /// <response code="404">Weather forecast not found.</response>
    /// <response code="501">Not implemented.</response>
    [HttpPut("{id}", Name = "UpdateWeatherForecast")]
    [ProducesDefaultResponseType]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public async Task<IActionResult> Update(int id, [FromBody] object request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return StatusCode(StatusCodes.Status501NotImplemented);
    }

    /// <summary>
    /// Deletes a weather forecast.
    /// </summary>
    /// <param name="id">The ID of the weather forecast to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>204 on success; 404 if not found.</returns>
    /// <response code="204">Weather forecast deleted successfully.</response>
    /// <response code="404">Weather forecast not found.</response>
    /// <response code="501">Not implemented.</response>
    [HttpDelete("{id}", Name = "DeleteWeatherForecast")]
    [ProducesDefaultResponseType]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return StatusCode(StatusCodes.Status501NotImplemented);
    }
}
