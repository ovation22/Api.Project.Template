using System.Diagnostics;
using Amazon.SQS.Model;
using Api.Project.Template.Application.Messaging;
using Api.Project.Template.Infrastructure.Messaging;
using Api.Project.Template.Infrastructure.Messaging.Sqs;
using static Api.Project.Template.Tests.Integration.Messaging.TestSupport;

namespace Api.Project.Template.Tests.Integration.Messaging.Sqs;

/// <summary>
/// Publishes with the real SqsMessagePublisher and consumes with the real SqsBrokerAdapter.
/// </summary>
[Trait("Category", "Sqs")]
[Collection(LocalStackCollection.Name)]
public sealed class SqsMessagePublisherTests(LocalStackFixture fixture)
{
    [Fact]
    public async Task PublishToQueue_DeliversPayloadAndCorrelationId()
    {
        // Arrange
        var queue = UniqueName("publish");
        var received = new TaskCompletionSource<(TestMessage Message, MessageContext Context)>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var consumer = await StartConsumerAsync(queue, received);
        await using var publisher = new SqsMessagePublisher(fixture.CreateAppConfiguration(), Logger<SqsMessagePublisher>());
        using var activity = new Activity("publish").Start();

        // Act
        await publisher.PublishAsync(new TestMessage(7), new MessagePublishOptions { Destination = queue },
            TestContext.Current.CancellationToken);
        var (message, context) = await received.Task.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(7, message.Id);
        Assert.Equal(activity.TraceId.ToString(), context.CorrelationId);
    }

    [Fact]
    public async Task PublishToSnsTopic_ConsumerUnwrapsTheEnvelope()
    {
        // Arrange
        // Topic → queue subscription without raw delivery: the queue receives SNS envelopes.
        var queue = UniqueName("fanout");
        var received = new TaskCompletionSource<(TestMessage Message, MessageContext Context)>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var consumer = await StartConsumerAsync(queue, received);
        var topicArn = await SubscribeQueueToNewTopicAsync(queue);
        await using var publisher = new SqsMessagePublisher(fixture.CreateAppConfiguration(), Logger<SqsMessagePublisher>());
        using var activity = new Activity("publish").Start();

        // Act
        await publisher.PublishAsync(new TestMessage(9), new MessagePublishOptions { Destination = topicArn },
            TestContext.Current.CancellationToken);
        var (message, context) = await received.Task.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);

        // Assert
        // Before unwrapping, the envelope deserialized into a TestMessage with Id 0.
        Assert.Equal(9, message.Id);
        Assert.Equal(activity.TraceId.ToString(), context.CorrelationId);
    }

    private async Task<SqsBrokerAdapter> StartConsumerAsync(
        string queue, TaskCompletionSource<(TestMessage, MessageContext)> received)
    {
        var adapter = new SqsBrokerAdapter(Logger<SqsBrokerAdapter>());
        await adapter.ConnectAsync(new MessageBrokerConfig
        {
            ConnectionString = fixture.ServiceUrl,
            Queue = queue,
            ProviderSpecific =
            {
                ["Region"] = LocalStackFixture.Region,
                ["AccessKey"] = LocalStackFixture.AccessKey,
                ["SecretKey"] = LocalStackFixture.SecretKey
            }
        }, TestContext.Current.CancellationToken);

        await adapter.SubscribeAsync<TestMessage>((message, context) =>
        {
            received.TrySetResult((message, context));
            return Task.FromResult(MessageProcessingResult.Succeeded());
        }, TestContext.Current.CancellationToken);

        return adapter;
    }

    private async Task<string> SubscribeQueueToNewTopicAsync(string queue)
    {
        using var sqs = fixture.CreateClient();
        using var sns = fixture.CreateSnsClient();
        var ct = TestContext.Current.CancellationToken;

        var queueUrl = (await sqs.GetQueueUrlAsync(queue, ct)).QueueUrl;
        var queueArn = (await sqs.GetQueueAttributesAsync(new GetQueueAttributesRequest
        {
            QueueUrl = queueUrl,
            AttributeNames = ["QueueArn"]
        }, ct)).Attributes["QueueArn"];

        var topicArn = (await sns.CreateTopicAsync($"{queue}-topic", ct)).TopicArn;
        await sns.SubscribeAsync(topicArn, "sqs", queueArn, ct);
        return topicArn;
    }
}
