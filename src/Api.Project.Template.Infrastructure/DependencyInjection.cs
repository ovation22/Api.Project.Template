using Api.Project.Template.Application.Abstractions.Logging;
using Api.Project.Template.Application.Abstractions;
using Api.Project.Template.Infrastructure.Data.Repositories;
using Api.Project.Template.Infrastructure.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Project.Template.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IRepository, ContextRepository>();

        services.AddSingleton(typeof(ILoggerAdapter<>), typeof(LoggerAdapter<>));

        return services;
    }
}
