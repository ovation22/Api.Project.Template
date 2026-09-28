using Amazon.SQS.Model;
using Api.Project.Template.Infrastructure.Messaging;
using Api.Project.Template.Infrastructure.Messaging.Sqs;
using static Api.Project.Template.Tests.Integration.Messaging.TestSupport;

namespace Api.Project.Template.Tests.Integration.Messaging.Sqs;

[Trait("Category", "Sqs")]
public sealed class SqsBrokerAdapterTests(LocalStackFixture fixture) : IClassFixture<LocalStackFixture>
{
    private const int MaxRetries = 3;

    [Fact]
    public async Task TransientFailure_IsRetriedUntilMaxRetriesThenDeadLettered()
    {
        // Arrange
        var queue = UniqueName("retry");
        var handler = new RecordingHandler();
        await using var adapter = await StartAdapterAsync(queue, handler);

        // Act
        await SendAsync(queue, Payload(RecordingHandler.TransientFailureId));
        var reasons = await ReceiveDeadLetterReasonsAsync(queue, expected: 1);

        // Assert
        Assert.Equal([1, 2, 3], handler.DeliveryCountsFor(RecordingHandler.TransientFailureId));
        Assert.Equal(["transient"], reasons);
    }

    [Fact]
    public async Task PermanentFailure_IsDeadLetteredWithoutRetry()
    {
        // Arrange
        var queue = UniqueName("permanent");
        var handler = new RecordingHandler();
        await using var adapter = await StartAdapterAsync(queue, handler);

        // Act
        await SendAsync(queue, Payload(RecordingHandler.PermanentFailureId));
        var reasons = await ReceiveDeadLetterReasonsAsync(queue, expected: 1);

        // Assert
        Assert.Equal([1], handler.DeliveryCountsFor(RecordingHandler.PermanentFailureId));
        Assert.Equal(["permanent"], reasons);
    }

    [Fact]
    public async Task MalformedMessage_IsDeadLetteredWithReason()
    {
        // Arrange
        var queue = UniqueName("malformed");
        await using var adapter = await StartAdapterAsync(queue, new RecordingHandler());

        // Act
        await SendAsync(queue, MalformedPayload);
        var reasons = await ReceiveDeadLetterReasonsAsync(queue, expected: 1);

        // Assert
        Assert.Equal(["DeserializationFailed"], reasons);
    }

    [Fact]
    public async Task SuccessfulMessage_IsProcessedOnceAndNotDeadLettered()
    {
        // Arrange
        var queue = UniqueName("success");
        var handler = new RecordingHandler();
        await using var adapter = await StartAdapterAsync(queue, handler);

        // Act
        await SendAsync(queue, Payload(RecordingHandler.SuccessId));
        var deliveries = await EventuallyAsync(
            () => Task.FromResult(handler.DeliveryCountsFor(RecordingHandler.SuccessId)), d => d.Count == 1);

        // Assert
        Assert.Equal([1], deliveries);
        Assert.Empty(await ReceiveDeadLetterReasonsAsync(queue, expected: 0, timeout: TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public async Task DisposeAsync_CalledTwiceAfterSubscribing_DoesNotThrow()
    {
        // Arrange
        // Both the consumer and the DI container dispose the adapter singleton at shutdown;
        // the second call used to hit CancelAsync on a disposed CancellationTokenSource.
        var adapter = await StartAdapterAsync(UniqueName("dispose"), new RecordingHandler());

        // Act
        await adapter.DisposeAsync();
        var secondDispose = await Record.ExceptionAsync(async () => await adapter.DisposeAsync());

        // Assert
        Assert.Null(secondDispose);
    }

    private async Task<SqsBrokerAdapter> StartAdapterAsync(string queue, RecordingHandler handler)
    {
        var adapter = new SqsBrokerAdapter(Logger<SqsBrokerAdapter>());
        await adapter.ConnectAsync(new MessageBrokerConfig
        {
            ConnectionString = fixture.ServiceUrl,
            Queue = queue,
            MaxRetries = MaxRetries,
            Concurrency = 2,
            ProviderSpecific =
            {
                ["Region"] = LocalStackFixture.Region,
                ["AccessKey"] = LocalStackFixture.AccessKey,
                ["SecretKey"] = LocalStackFixture.SecretKey
            }
        }, TestContext.Current.CancellationToken);
        await adapter.SubscribeAsync<TestMessage>(handler.HandleAsync, TestContext.Current.CancellationToken);
        return adapter;
    }

    private async Task SendAsync(string queue, string payload)
    {
        using var sqs = fixture.CreateClient();
        var queueUrl = (await sqs.GetQueueUrlAsync(queue, TestContext.Current.CancellationToken)).QueueUrl;
        await sqs.SendMessageAsync(queueUrl, payload, TestContext.Current.CancellationToken);
    }

    // Drains the dead-letter queue until `expected` messages arrive (or the timeout passes)
    // and returns their DeadLetterReason attributes.
    private async Task<List<string>> ReceiveDeadLetterReasonsAsync(string queue, int expected, TimeSpan? timeout = null)
    {
        using var sqs = fixture.CreateClient();
        var dlqUrl = (await sqs.GetQueueUrlAsync($"{queue}-dlq", TestContext.Current.CancellationToken)).QueueUrl;
        var reasons = new List<string>();

        await EventuallyAsync(async () =>
        {
            var response = await sqs.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = dlqUrl,
                MaxNumberOfMessages = 10,
                WaitTimeSeconds = 1,
                MessageAttributeNames = ["All"]
            }, TestContext.Current.CancellationToken);

            foreach (var message in response.Messages ?? [])
            {
                reasons.Add(message.MessageAttributes?.GetValueOrDefault("DeadLetterReason")?.StringValue ?? "(none)");
                await sqs.DeleteMessageAsync(dlqUrl, message.ReceiptHandle, TestContext.Current.CancellationToken);
            }

            return reasons.Count;
        }, count => count >= expected && expected > 0, timeout);

        return reasons;
    }
}
