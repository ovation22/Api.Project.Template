using System;
using System.Collections.Generic;
using Api.Project.Template.Core.Interfaces.Logging;
using Api.Project.Template.Core.Interfaces.Services;
using Api.Project.Template.Core.Models.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Project.Template.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private readonly ILoggerAdapter<WeatherForecastController> _logger;
    private readonly IWeatherForecastService _service;

    public WeatherForecastController(IWeatherForecastService service, ILoggerAdapter<WeatherForecastController> logger)
    {
        _logger = logger;
        _service = service;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesDefaultResponseType]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<IEnumerable<WeatherForecast>> Get()
    {
        try
        {
            var result = _service.GetWeatherForecast();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }

        return Problem("Unable to return forecast", statusCode: StatusCodes.Status400BadRequest);
    }
}
