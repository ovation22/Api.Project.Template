using Api.Project.Template.Application.Features.Weather.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Project.Template.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddScoped<IWeatherForecastService, WeatherForecastService>();

        return services;
    }
}
