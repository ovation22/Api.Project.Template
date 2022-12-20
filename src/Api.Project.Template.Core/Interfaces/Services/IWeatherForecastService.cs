using System.Collections.Generic;
using Api.Project.Template.Core.Models.DTO;

namespace Api.Project.Template.Core.Interfaces.Services;

public interface IWeatherForecastService
{
    IEnumerable<WeatherForecast> GetWeatherForecast();
}