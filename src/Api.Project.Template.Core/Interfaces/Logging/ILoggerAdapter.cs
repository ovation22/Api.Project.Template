using System;

namespace Api.Project.Template.Core.Interfaces.Logging
{
    public interface ILoggerAdapter<T>
    {
        void LogInformation(string message);
        void LogInformation<T0>(string message, T0 arg0);
        void LogInformation<T0, T1>(string message, T0 arg0, T1 arg1);
        void LogInformation<T0, T1, T2>(string message, T0 arg0, T1 arg1, T2 arg2);

        void LogWarning(string message);
        void LogWarning<T0>(string message, T0 arg0);
        void LogWarning<T0, T1>(string message, T0 arg0, T1 arg1);
        void LogWarning<T0, T1, T2>(string message, T0 arg0, T1 arg1, T2 arg2);

        void LogWarning(Exception ex, string message);
        void LogWarning<T0>(Exception ex, string message, T0 arg0);
        void LogWarning<T0, T1>(Exception ex, string message, T0 arg0, T1 arg1);
        void LogWarning<T0, T1, T2>(Exception ex, string message, T0 arg0, T1 arg1, T2 arg2);

        void LogError(Exception ex, string message);
        void LogError<T0>(Exception ex, string message, T0 arg0);
        void LogError<T0, T1>(Exception ex, string message, T0 arg0, T1 arg1);
        void LogError<T0, T1, T2>(Exception ex, string message, T0 arg0, T1 arg1, T2 arg2);
    }
}
