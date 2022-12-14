using Api.Project.Template.Core.Interfaces.Logging;
using Api.Project.Template.Infrastructure.Logging;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;

namespace Api.Project.Template.Tests.Benchmark;

[MemoryDiagnoser]
public class LoggingBenchmarks
{
    private readonly ILogger<LoggingBenchmarks> _logger;
    private readonly ILoggerAdapter<LoggingBenchmarks> _loggerAdapter;

    public LoggingBenchmarks()
    {
        ILoggerFactory? loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(LogLevel.Warning);
        });

        _logger = new Logger<LoggingBenchmarks>(loggerFactory);
        _loggerAdapter = new LoggerAdapter<LoggingBenchmarks>(_logger);
    }

    [Benchmark]
    public void Logger_Without_If()
    {
        _logger.LogInformation("test {0}", 42);
    }

    [Benchmark]
    public void Logger_With_If()
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("test {0}", 42);
        }
    }

    [Benchmark]
    public void LoggerAdapter()
    {
        _loggerAdapter.LogInformation("test {0}", 42);
    }
}
