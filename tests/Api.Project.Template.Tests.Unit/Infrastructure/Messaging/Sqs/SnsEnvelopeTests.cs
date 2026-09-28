using Api.Project.Template.Infrastructure.Messaging.Sqs;

namespace Api.Project.Template.Tests.Unit.Infrastructure.Messaging.Sqs;

[Trait("Category", "SnsEnvelope")]
public class SnsEnvelopeTests
{
    [Fact]
    public void TryUnwrap_SnsNotification_ReturnsInnerPayloadAndAttributes()
    {
        // Arrange
        const string body = """
            {
              "Type": "Notification",
              "MessageId": "1",
              "TopicArn": "arn:aws:sns:us-east-1:000000000000:events",
              "Message": "{\"id\":42}",
              "MessageAttributes": {
                "CorrelationId": { "Type": "String", "Value": "abc-123" },
                "MessageType": { "Type": "String", "Value": "WeatherRequested" }
              }
            }
            """;

        // Act
        var unwrapped = SnsEnvelope.TryUnwrap(body, out var payload, out var attributes);

        // Assert
        Assert.True(unwrapped);
        Assert.Equal("""{"id":42}""", payload);
        Assert.Equal("abc-123", attributes["CorrelationId"]);
        Assert.Equal("WeatherRequested", attributes["MessageType"]);
    }

    [Fact]
    public void TryUnwrap_PlainPayload_ReturnsFalseAndLeavesBodyUnchanged()
    {
        // Arrange
        const string body = """{"id":42}""";

        // Act
        var unwrapped = SnsEnvelope.TryUnwrap(body, out var payload, out var attributes);

        // Assert
        Assert.False(unwrapped);
        Assert.Equal(body, payload);
        Assert.Empty(attributes);
    }

    [Fact]
    public void TryUnwrap_PayloadThatOnlyLooksLikeAnEnvelope_ReturnsFalse()
    {
        // Arrange
        // A real message with Type/Message fields but no TopicArn must not be unwrapped
        const string body = """{"Type":"Notification","Message":"hello"}""";

        // Act
        var unwrapped = SnsEnvelope.TryUnwrap(body, out var payload, out _);

        // Assert
        Assert.False(unwrapped);
        Assert.Equal(body, payload);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{ broken")]
    [InlineData("[1,2,3]")]
    public void TryUnwrap_NonEnvelopeBody_ReturnsFalse(string body)
    {
        // Act
        var unwrapped = SnsEnvelope.TryUnwrap(body, out var payload, out _);

        // Assert
        Assert.False(unwrapped);
        Assert.Equal(body, payload);
    }
}
