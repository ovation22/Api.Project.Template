using Api.Project.Template.Application.Abstractions.Logging;
using Api.Project.Template.Infrastructure.Messaging;
using Api.Project.Template.Infrastructure.Messaging.Abstractions;

namespace Api.Project.Template.Worker;

public sealed class Worker(
    IMessageConsumer consumer,
    ILoggerAdapter<MessageConsumerWorker<WeatherRequested, WeatherRequestProcessor>> logger)
    : MessageConsumerWorker<WeatherRequested, WeatherRequestProcessor>(consumer, logger);
