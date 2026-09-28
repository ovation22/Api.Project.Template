using System.Diagnostics;

namespace Api.Project.Template.Infrastructure.Messaging;

/// <summary>
/// Resolves the correlation ID stamped on published messages, so consumer logs can be tied back
/// to the request that published them.
/// </summary>
internal static class CorrelationId
{
    /// <summary>
    /// The current request's correlation ID (the <c>client.correlation_id</c> tag or baggage set by the
    /// correlation-ID enricher), else the current trace ID, else a new GUID.
    /// </summary>
    public static string Current()
    {
        var activity = Activity.Current;

        return activity?.Tags.FirstOrDefault(t => t.Key == "client.correlation_id").Value
               ?? activity?.Baggage.FirstOrDefault(kv => kv.Key == "client.correlation_id").Value
               ?? activity?.TraceId.ToString()
               ?? Guid.NewGuid().ToString();
    }
}
