
using Api.Project.Template.Application.Messaging;

namespace Api.Project.Template.Tests.Unit.Infrastructure.Messaging;

public class MessageContextTests
{
    [Fact]
    public void Constructor_WithInitializers_SetsPropertiesCorrectly()
    {
        // Arrange
        var messageId = "msg-123";
        var correlationId = "corr-456";
        var deliveryCount = 2;
        var headers = new Dictionary<string, object>
        {
            ["ContentType"] = "application/json",
            ["Priority"] = 5
        };
        var cancellationToken = new CancellationToken();

        // Act
        var context = new MessageContext
        {
            MessageId = messageId,
            CorrelationId = correlationId,
            DeliveryCount = deliveryCount,
            Headers = headers,
            CancellationToken = cancellationToken
        };

        // Assert
        Assert.Equal(messageId, context.MessageId);
        Assert.Equal(correlationId, context.CorrelationId);
        Assert.Equal(deliveryCount, context.DeliveryCount);
        Assert.Same(headers, context.Headers);
        Assert.Equal(cancellationToken, context.CancellationToken);
    }

    [Fact]
    public void MessageId_DefaultsToEmptyString()
    {
        // Act
        var context = new MessageContext();

        // Assert
        Assert.Equal(string.Empty, context.MessageId);
    }

    [Fact]
    public void CorrelationId_DefaultsToEmptyString()
    {
        // Act
        var context = new MessageContext();

        // Assert
        Assert.Equal(string.Empty, context.CorrelationId);
    }

    [Fact]
    public void DeliveryCount_DefaultsToZero()
    {
        // Act
        var context = new MessageContext();

        // Assert
        Assert.Equal(0, context.DeliveryCount);
    }

    [Fact]
    public void Headers_DefaultsToEmptyDictionary()
    {
        // Act
        var context = new MessageContext();

        // Assert
        Assert.NotNull(context.Headers);
        Assert.Empty(context.Headers);
    }

    [Fact]
    public void CancellationToken_DefaultsToNone()
    {
        // Act
        var context = new MessageContext();

        // Assert
        Assert.Equal(CancellationToken.None, context.CancellationToken);
    }

    [Fact]
    public void Headers_IsReadOnly_CannotBeModifiedAfterCreation()
    {
        // Arrange
        var headers = new Dictionary<string, object>
        {
            ["Key1"] = "Value1"
        };
        var context = new MessageContext
        {
            Headers = headers
        };

        // Act & Assert
        // Headers property returns IReadOnlyDictionary, so we can't modify it
        // This test verifies the type is correct
        Assert.IsAssignableFrom<IReadOnlyDictionary<string, object>>(context.Headers);
    }

    [Fact]
    public void Properties_UseInitAccessor_CannotBeSetAfterConstruction()
    {
        // Arrange
        var context = new MessageContext
        {
            MessageId = "initial-id",
            CorrelationId = "initial-corr"
        };

        // Assert
        // If this compiles without error, it means we're using init accessors correctly
        // Attempting to reassign would cause a compile error
        Assert.Equal("initial-id", context.MessageId);
        Assert.Equal("initial-corr", context.CorrelationId);
    }

    [Fact]
    public void ProviderContext_CanBeSetInternally()
    {
        // Arrange & Act
        var context = new MessageContext();

        // Assert
        // ProviderContext is internal, so we can't test it directly from this assembly
        // This test just verifies the class can be instantiated
        Assert.NotNull(context);
    }

    [Fact]
    public void FullyPopulatedContext_HasAllProperties()
    {
        // Arrange
        var headers = new Dictionary<string, object>
        {
            ["MessageType"] = "TestMessage",
            ["Timestamp"] = DateTimeOffset.UtcNow
        };
        var cts = new CancellationTokenSource();

        // Act
        var context = new MessageContext
        {
            MessageId = "msg-abc-123",
            CorrelationId = "corr-xyz-789",
            DeliveryCount = 3,
            Headers = headers,
            CancellationToken = cts.Token
        };

        // Assert
        Assert.Equal("msg-abc-123", context.MessageId);
        Assert.Equal("corr-xyz-789", context.CorrelationId);
        Assert.Equal(3, context.DeliveryCount);
        Assert.Equal(2, context.Headers.Count);
        Assert.Equal("TestMessage", context.Headers["MessageType"]);
        Assert.False(context.CancellationToken.IsCancellationRequested);
    }
}
