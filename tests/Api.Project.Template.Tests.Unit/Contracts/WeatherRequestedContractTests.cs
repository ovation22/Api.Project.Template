using System.Text.Json;
using Api.Project.Template.Application.Features.Weather.Events;
using Api.Project.Template.Worker;

namespace Api.Project.Template.Tests.Unit.Contracts;

/// <summary>
/// The Worker's <see cref="WeatherRequested"/> is its own copy of the API's
/// <see cref="WeatherForecastRequestedEvent"/>, so the two services stay independent.
/// These tests catch the copies drifting apart — which otherwise deserializes to default values
/// and is processed "successfully".
/// </summary>
[Trait("Category", "Contracts")]
public class WeatherRequestedContractTests
{
    // Mirrors the publishers' and broker adapters' JSON settings
    private static readonly JsonSerializerOptions PublisherOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly JsonSerializerOptions ConsumerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void PublishedEvent_DeserializesIntoTheWorkersMessage_WithEveryField()
    {
        // Arrange
        var published = new WeatherForecastRequestedEvent(3, 25, new DateTimeOffset(2026, 9, 28, 12, 30, 0, TimeSpan.Zero));

        // Act
        var json = JsonSerializer.Serialize(published, PublisherOptions);
        var received = JsonSerializer.Deserialize<WeatherRequested>(json, ConsumerOptions);

        // Assert
        Assert.NotNull(received);
        Assert.Equal(published.Page, received.Page);
        Assert.Equal(published.Size, received.Size);
        Assert.Equal(published.RequestedAt, received.RequestedAt);
    }

    [Fact]
    public void EveryWorkerMessageProperty_ExistsOnThePublishedEvent_WithTheSameType()
    {
        // Arrange
        var eventProperties = typeof(WeatherForecastRequestedEvent).GetProperties()
            .ToDictionary(p => p.Name, p => p.PropertyType, StringComparer.OrdinalIgnoreCase);

        // Act
        var mismatches = typeof(WeatherRequested).GetProperties()
            .Where(p => !eventProperties.TryGetValue(p.Name, out var type) || type != p.PropertyType)
            .Select(p => $"{p.Name} ({p.PropertyType.Name})")
            .ToList();

        // Assert
        Assert.True(mismatches.Count == 0,
            $"WeatherRequested properties missing from or typed differently on WeatherForecastRequestedEvent: {string.Join(", ", mismatches)}");
    }
}
