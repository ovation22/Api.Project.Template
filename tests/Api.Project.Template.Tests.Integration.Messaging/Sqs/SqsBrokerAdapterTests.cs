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
    public async Task StoppingMidMessage_ReturnsMessageToQueueInsteadOfDeadLettering()
    {
        // Arrange
        var queue = UniqueName("stop");
        var handler = new SlowHandler();
        using var stopping = new CancellationTokenSource();
        var adapter = await ConnectAdapterAsync(queue);
        await adapter.SubscribeAsync<TestMessage>(handler.HandleAsync, stopping.Token);
        await SendAsync(queue, Payload(1));
        await handler.Started.Task.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);

        // Act
        await stopping.CancelAsync();
        await adapter.DisposeAsync();
        // Usually visible again right away. If a long-poll receive that was being cancelled picked it up
        // first, it's in flight until its visibility timeout instead — either way it's still in the queue.
        var counts = await EventuallyAsync(() => CountAsync(queue), c => c.Visible == 1, TimeSpan.FromSeconds(10));

        // Assert
        Assert.Equal(1, counts.Visible + counts.InFlight);
        Assert.Empty(await ReceiveDeadLetterReasonsAsync(queue, expected: 0, timeout: TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public async Task StoppingWhileHandlerIsRunning_WaitsForItAndDeletesTheMessage()
    {
        // Arrange
        // The handler takes 3s and ignores cancellation — it finishes its work during shutdown.
        var queue = UniqueName("drain");
        var handler = new SlowHandler(TimeSpan.FromSeconds(3));
        using var stopping = new CancellationTokenSource();
        var adapter = await ConnectAdapterAsync(queue);
        await adapter.SubscribeAsync<TestMessage>(handler.HandleAsync, stopping.Token);
        await SendAsync(queue, Payload(1));
        await handler.Started.Task.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);

        // Act
        await stopping.CancelAsync();
        await adapter.DisposeAsync();

        // Assert
        // Dispose waited for the handler, and the processed message was deleted (not left in flight).
        Assert.Equal(1, handler.Completed);
        Assert.Equal((0, 0), await CountAsync(queue));
    }

    [Fact]
    public async Task SlowHandler_ExtendsVisibility_SoTheMessageIsProcessedOnce()
    {
        // Arrange
        // A 2s visibility timeout and a 5s handler: without extension the message reappears mid-processing
        // and a free handler slot picks it up again.
        var queue = UniqueName("slow");
        using (var sqs = fixture.CreateClient())
        {
            await sqs.CreateQueueAsync(new CreateQueueRequest
            {
                QueueName = queue,
                Attributes = new Dictionary<string, string> { ["VisibilityTimeout"] = "2" }
            }, TestContext.Current.CancellationToken);
        }

        var handler = new SlowHandler(TimeSpan.FromSeconds(5));
        await using var adapter = await ConnectAdapterAsync(queue);
        await adapter.SubscribeAsync<TestMessage>(handler.HandleAsync, TestContext.Current.CancellationToken);

        // Act
        await SendAsync(queue, Payload(1));
        await EventuallyAsync(() => Task.FromResult(handler.Completed), c => c == 1);
        var remaining = await EventuallyAsync(() => CountAsync(queue), c => c == (0, 0), TimeSpan.FromSeconds(5));

        // Assert
        Assert.Equal(1, handler.Calls);
        Assert.Equal((0, 0), remaining);
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
        var adapter = await ConnectAdapterAsync(queue);
        await adapter.SubscribeAsync<TestMessage>(handler.HandleAsync, TestContext.Current.CancellationToken);
        return adapter;
    }

    // Visible and in-flight (received but not yet deleted) message counts.
    private async Task<(int Visible, int InFlight)> CountAsync(string queue)
    {
        using var sqs = fixture.CreateClient();
        var queueUrl = (await sqs.GetQueueUrlAsync(queue, TestContext.Current.CancellationToken)).QueueUrl;
        var attributes = (await sqs.GetQueueAttributesAsync(new GetQueueAttributesRequest
        {
            QueueUrl = queueUrl,
            AttributeNames = ["ApproximateNumberOfMessages", "ApproximateNumberOfMessagesNotVisible"]
        }, TestContext.Current.CancellationToken)).Attributes;

        return (int.Parse(attributes["ApproximateNumberOfMessages"]), int.Parse(attributes["ApproximateNumberOfMessagesNotVisible"]));
    }

    private async Task<SqsBrokerAdapter> ConnectAdapterAsync(string queue)
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
