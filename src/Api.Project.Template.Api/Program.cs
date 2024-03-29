﻿using Api.Project.Template.Api.Config;
using Api.Project.Template.Core.Interfaces.Data;
using Api.Project.Template.Core.Interfaces.Logging;
using Api.Project.Template.Core.Interfaces.Services;
using Api.Project.Template.Core.Services;
using Api.Project.Template.Infrastructure.Data;
using Api.Project.Template.Infrastructure.Logging;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Api.Project.Template.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseSerilog((ctx, lc) =>
            lc.ReadFrom.Configuration(ctx.Configuration));

        builder.Services.AddHttpContextAccessor();

        builder.Services.AddHealthCheckConfig(builder.Configuration);

        builder.Services.AddControllers();
        builder.Services.AddProblemDetails();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddRouting(x => x.LowercaseUrls = true);

        builder.Services.AddScoped(typeof(IRepository), typeof(ContextRepository));
        builder.Services.AddDbContextPool<Context>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("Database")!));
        builder.Services.AddApplicationInsightsTelemetry(opt => opt.EnableActiveTelemetryConfigurationSetup = true);
        builder.Services.ConfigureTelemetryModule<DependencyTrackingTelemetryModule>((module, _) =>
        {
            module.EnableSqlCommandTextInstrumentation = true;
        });

        builder.Services.AddScoped<IWeatherForecastService, WeatherForecastService>();
        builder.Services.AddSingleton(typeof(ILoggerAdapter<>), typeof(LoggerAdapter<>));

        var app = builder.Build();

        app.UseSerilogRequestLogging();

        app.UseHealthCheckConfig();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseExceptionHandler();

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
