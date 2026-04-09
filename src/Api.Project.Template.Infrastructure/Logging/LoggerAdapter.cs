using System;
using Api.Project.Template.Application.Abstractions.Logging;
using Microsoft.Extensions.Logging;

namespace Api.Project.Template.Infrastructure.Logging;

public class LoggerAdapter<T> : ILoggerAdapter<T>
{
    private readonly ILogger<T> _logger;

    public LoggerAdapter(ILogger<T> logger)
    {
        _logger = logger;
    }

    public void LogDebug(string message)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(message);
        }
    }

    public void LogDebug<T0>(string message, T0 arg0)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(message, arg0);
        }
    }

    public void LogDebug<T0, T1>(string message, T0 arg0, T1 arg1)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(message, arg0, arg1);
        }
    }

    public void LogDebug<T0, T1, T2>(string message, T0 arg0, T1 arg1, T2 arg2)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(message, arg0, arg1, arg2);
        }
    }

    public void LogInformation(string message)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(message);
        }
    }

    public void LogInformation<T0>(string message, T0 arg0)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(message, arg0);
        }
    }

    public void LogInformation<T0, T1>(string message, T0 arg0, T1 arg1)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(message, arg0, arg1);
        }
    }

    public void LogInformation<T0, T1, T2>(string message, T0 arg0, T1 arg1, T2 arg2)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(message, arg0, arg1, arg2);
        }
    }

    public void LogInformation<T0, T1, T2, T3>(string message, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(message, arg0, arg1, arg2, arg3);
        }
    }

    public void LogInformation<T0, T1, T2, T3, T4>(string message, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(message, arg0, arg1, arg2, arg3, arg4);
        }
    }

    public void LogInformation<T0, T1, T2, T3, T4, T5>(string message, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(message, arg0, arg1, arg2, arg3, arg4, arg5);
        }
    }

    public void LogWarning(string message)
    {
        if (_logger.IsEnabled(LogLevel.Warning))
        {
            _logger.LogWarning(message);
        }
    }

    public void LogWarning<T0>(string message, T0 arg0)
    {
        if (_logger.IsEnabled(LogLevel.Warning))
        {
            _logger.LogWarning(message, arg0);
        }
    }

    public void LogWarning<T0, T1>(string message, T0 arg0, T1 arg1)
    {
        if (_logger.IsEnabled(LogLevel.Warning))
        {
            _logger.LogWarning(message, arg0, arg1);
        }
    }

    public void LogWarning<T0, T1, T2>(string message, T0 arg0, T1 arg1, T2 arg2)
    {
        if (_logger.IsEnabled(LogLevel.Warning))
        {
            _logger.LogWarning(message, arg0, arg1, arg2);
        }
    }

    public void LogWarning(Exception ex, string message)
    {
        if (_logger.IsEnabled(LogLevel.Warning))
        {
            _logger.LogWarning(ex, message);
        }
    }

    public void LogWarning<T0>(Exception ex, string message, T0 arg0)
    {
        if (_logger.IsEnabled(LogLevel.Warning))
        {
            _logger.LogWarning(ex, message, arg0);
        }
    }

    public void LogWarning<T0, T1>(Exception ex, string message, T0 arg0, T1 arg1)
    {
        if (_logger.IsEnabled(LogLevel.Warning))
        {
            _logger.LogWarning(ex, message, arg0, arg1);
        }
    }

    public void LogWarning<T0, T1, T2>(Exception ex, string message, T0 arg0, T1 arg1, T2 arg2)
    {
        if (_logger.IsEnabled(LogLevel.Warning))
        {
            _logger.LogWarning(ex, message, arg0, arg1, arg2);
        }
    }

    public void LogError(Exception ex, string message)
    {
        if (_logger.IsEnabled(LogLevel.Error))
        {
            _logger.LogError(ex, message);
        }
    }

    public void LogError<T0>(Exception ex, string message, T0 arg0)
    {
        if (_logger.IsEnabled(LogLevel.Error))
        {
            _logger.LogError(ex, message, arg0);
        }
    }

    public void LogError<T0, T1>(Exception ex, string message, T0 arg0, T1 arg1)
    {
        if (_logger.IsEnabled(LogLevel.Error))
        {
            _logger.LogError(ex, message, arg0, arg1);
        }
    }

    public void LogError<T0, T1, T2>(Exception ex, string message, T0 arg0, T1 arg1, T2 arg2)
    {
        if (_logger.IsEnabled(LogLevel.Error))
        {
            _logger.LogError(ex, message, arg0, arg1, arg2);
        }
    }
}
