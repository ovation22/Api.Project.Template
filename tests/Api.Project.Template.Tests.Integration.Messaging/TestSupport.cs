using System.Collections.Concurrent;
using Api.Project.Template.Application.Messaging;
using Api.Project.Template.Infrastructure.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Api.Project.Template.Tests.Integration.Messaging;

public record TestMessage(int Id);

/// <summary>
/// A message processor stand-in that records every delivery and returns a scripted result per message Id.
/// </summary>
public sealed class RecordingHandler
{
    public const int TransientFailureId = 1;
    public const int PermanentFailureId = 2;
    public const int SuccessId = 3;

    private readonly ConcurrentQueue<(int Id, int DeliveryCount)> _deliveries = new();

    public IReadOnlyList<int> DeliveryCountsFor(int id)
        => _deliveries.Where(d => d.Id == id).Select(d => d.DeliveryCount).ToList();

    public Task<MessageProcessingResult> HandleAsync(TestMessage message, MessageContext context)
    {
        _deliveries.Enqueue((message.Id, context.DeliveryCount));

        return Task.FromResult(message.Id switch
        {
            TransientFailureId => MessageProcessingResult.Failed("transient", requeue: true),
            PermanentFailureId => MessageProcessingResult.Failed("permanent", requeue: false),
            _ => MessageProcessingResult.Succeeded()
        });
    }
}

/// <summary>
/// A handler that signals when it starts, then either runs for a fixed time (ignoring cancellation,
/// like a processor finishing its work) or waits until the consumer's token is cancelled.
/// </summary>
public sealed class SlowHandler(TimeSpan? duration = null)
{
    private int _calls;
    private int _completed;

    public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public int Calls => Volatile.Read(ref _calls);
    public int Completed => Volatile.Read(ref _completed);

    public async Task<MessageProcessingResult> HandleAsync(TestMessage message, MessageContext context)
    {
        Interlocked.Increment(ref _calls);
        Started.TrySetResult();

        if (duration is { } d)
            await Task.Delay(d);
        else
            await Task.Delay(Timeout.Infinite, context.CancellationToken);

        Interlocked.Increment(ref _completed);
        return MessageProcessingResult.Succeeded();
    }
}

public static class TestSupport
{
    public const string MalformedPayload = "this is not json";

    public static string Payload(int id) => $"{{\"id\":{id}}}";

    public static string UniqueName(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..(prefix.Length + 13)];

    public static LoggerAdapter<T> Logger<T>() => new(NullLogger<T>.Instance);

    /// <summary>
    /// Builds and starts a container, retrying with a fresh container if startup fails.
    /// Docker Desktop / Rancher Desktop on Windows occasionally return a malformed response over the
    /// named pipe ("Invalid chunk header"), which fails an otherwise healthy start.
    /// </summary>
    public static async Task<TContainer> StartContainerAsync<TContainer>(Func<TContainer> build, int attempts = 5)
        where TContainer : DotNet.Testcontainers.Containers.IContainer
    {
        for (var attempt = 1; ; attempt++)
        {
            var container = build();
            try
            {
                await container.StartAsync();
                return container;
            }
            catch (Exception) when (attempt < attempts)
            {
                await container.DisposeAsync();
                await Task.Delay(TimeSpan.FromSeconds(attempt));
            }
        }
    }

    /// <summary>
    /// Polls <paramref name="probe"/> until <paramref name="isDone"/> holds or the timeout elapses,
    /// then returns the last observed value (the caller asserts on it).
    /// </summary>
    public static async Task<T> EventuallyAsync<T>(Func<Task<T>> probe, Func<T, bool> isDone, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(30));
        var value = await probe();

        while (!isDone(value) && DateTime.UtcNow < deadline)
        {
            await Task.Delay(200, TestContext.Current.CancellationToken);
            value = await probe();
        }

        return value;
    }
}
