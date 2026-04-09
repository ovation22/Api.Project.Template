using Api.Project.Template.Application.Abstractions.Logging;
using Api.Project.Template.Application.Messaging.Abstractions;
using Api.Project.Template.Infrastructure.Messaging.Abstractions;
using Microsoft.Extensions.Hosting;

namespace Api.Project.Template.Infrastructure.Messaging;

/// <summary>
/// Generic background service that hosts an <see cref="IMessageConsumer"/>.
/// Registered automatically by <see cref="MessageBusExtensions.AddMessageConsumer{TMessage,TProcessor}"/>.
/// </summary>
public class MessageConsumerWorker<TMessage, TProcessor>(
    IMessageConsumer consumer,
    ILoggerAdapter<MessageConsumerWorker<TMessage, TProcessor>> logger)
    : BackgroundService
    where TProcessor : IMessageProcessor<TMessage>
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "MessageConsumerWorker<{MessageType}, {ProcessorType}> starting.",
            typeof(TMessage).Name, typeof(TProcessor).Name);

        try
        {
            await consumer.StartAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to start consumer for {MessageType}. Host will stop.",
                typeof(TMessage).Name);
            throw;
        }

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // expected on host shutdown
        }
        finally
        {
            logger.LogInformation(
                "MessageConsumerWorker<{MessageType}, {ProcessorType}> stopping.",
                typeof(TMessage).Name, typeof(TProcessor).Name);

            try
            {
                await consumer.StopAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error stopping consumer for {MessageType}.", typeof(TMessage).Name);
            }

            try
            {
                consumer.Dispose();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Exception disposing consumer for {MessageType}.", typeof(TMessage).Name);
            }

            logger.LogInformation(
                "MessageConsumerWorker<{MessageType}, {ProcessorType}> stopped.",
                typeof(TMessage).Name, typeof(TProcessor).Name);
        }
    }
}
