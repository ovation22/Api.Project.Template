using Api.Project.Template.Core.Interfaces.Logging;
using Api.Project.Template.Infrastructure.Logging;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;

namespace Api.Project.Template.Tests.Benchmark;

[ShortRunJob]
[MemoryDiagnoser]
public class LoggingBenchmarks
{
    [Params(LogLevel.Information, LogLevel.Warning)]
    public LogLevel BenchmarkLogLevel { get; set; }

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
    public void Logger_With_If()
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("test {0}", BenchmarkLogLevel);
        }
    }

    [Benchmark]
    public void Logger_Without_If()
    {
        _logger.LogInformation("test {0}", BenchmarkLogLevel);
    }

    [Benchmark]
    public void LoggerAdapter_With_Args()
    {
        _loggerAdapter.LogInformation("test {0}", BenchmarkLogLevel);
    }

    [Benchmark]
    public void LoggerAdapter_With_StringInterpolation()
    {
        _loggerAdapter.LogInformation("test {0}", BenchmarkLogLevel);
    }
}
