using Api.Project.Template.Application.Abstractions.Logging;
using Api.Project.Template.Application.Messaging;
using Api.Project.Template.Application.Messaging.Abstractions;

namespace Api.Project.Template.Worker;

public class WeatherRequestProcessor(ILoggerAdapter<WeatherRequestProcessor> logger)
    : IMessageProcessor<WeatherRequested>
{
    public Task<MessageProcessingResult> ProcessAsync(WeatherRequested message, MessageContext context)
    {
        logger.LogInformation(
            "Processing WeatherRequested: Page={Page}, Size={Size}, RequestedAt={RequestedAt} (MessageId={MessageId})",
            message.Page, message.Size, message.RequestedAt, context.MessageId);

        return Task.FromResult(MessageProcessingResult.Succeeded());
    }
}
