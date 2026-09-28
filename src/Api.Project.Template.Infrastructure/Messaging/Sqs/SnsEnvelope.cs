using System.Text.Json;

namespace Api.Project.Template.Infrastructure.Messaging.Sqs;

/// <summary>
/// Unwraps SNS notifications. When an SQS queue subscribes to an SNS topic without raw message delivery,
/// each message body is an SNS envelope — <c>{"Type":"Notification","TopicArn":…,"Message":"&lt;payload&gt;",
/// "MessageAttributes":{"Name":{"Type":"String","Value":…}}}</c> — rather than the published payload.
/// </summary>
internal static class SnsEnvelope
{
    /// <summary>
    /// Returns true (with the inner payload and its message attributes) when <paramref name="body"/> is an
    /// SNS notification envelope; otherwise false, and the body should be used as-is.
    /// </summary>
    public static bool TryUnwrap(string body, out string payload, out Dictionary<string, string> attributes)
    {
        payload = body;
        attributes = [];

        // Cheap pre-check before parsing: envelopes are JSON objects
        if (string.IsNullOrEmpty(body) || body.TrimStart() is not ['{', ..])
            return false;

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            // Property names are case-sensitive and all three must be present, so a regular payload
            // is never mistaken for an envelope.
            if (!root.TryGetProperty("Type", out var type) || type.ValueKind != JsonValueKind.String || type.GetString() != "Notification" ||
                !root.TryGetProperty("TopicArn", out var topicArn) || topicArn.ValueKind != JsonValueKind.String ||
                !root.TryGetProperty("Message", out var message) || message.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            payload = message.GetString()!;

            if (root.TryGetProperty("MessageAttributes", out var messageAttributes) && messageAttributes.ValueKind == JsonValueKind.Object)
            {
                foreach (var attribute in messageAttributes.EnumerateObject())
                {
                    if (attribute.Value.ValueKind == JsonValueKind.Object &&
                        attribute.Value.TryGetProperty("Value", out var value) &&
                        value.ValueKind == JsonValueKind.String)
                    {
                        attributes[attribute.Name] = value.GetString()!;
                    }
                }
            }

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
