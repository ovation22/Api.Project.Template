using Api.Project.Template.Infrastructure.Messaging;

namespace Api.Project.Template.Tests.Unit.Infrastructure.Messaging;

[Trait("Category", "NullMessagePublisher")]
public class NullMessagePublisherTests
{
    [Fact]
    public async Task PublishAsync_CompletesWithoutDoingAnything()
    {
        // Arrange
        var publisher = new NullMessagePublisher();

        // Act
        var task = publisher.PublishAsync(new { Id = 1 }, cancellationToken: TestContext.Current.CancellationToken);
        await task;

        // Assert
        Assert.True(task.IsCompletedSuccessfully);
    }
}
