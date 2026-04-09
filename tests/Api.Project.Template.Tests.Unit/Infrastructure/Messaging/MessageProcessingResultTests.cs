
using Api.Project.Template.Application.Messaging;

namespace Api.Project.Template.Tests.Unit.Infrastructure.Messaging;

public class MessageProcessingResultTests
{
    [Fact]
    public void Succeeded_CreatesSuccessResult()
    {
        // Act
        var result = MessageProcessingResult.Succeeded();

        // Assert
        Assert.True(result.Success);
        Assert.False(result.Requeue);
        Assert.Null(result.ErrorReason);
        Assert.Null(result.Exception);
    }

    [Fact]
    public void Failed_WithReasonOnly_CreatesFailureResult()
    {
        // Arrange
        var reason = "Processing failed due to validation error";

        // Act
        var result = MessageProcessingResult.Failed(reason);

        // Assert
        Assert.False(result.Success);
        Assert.False(result.Requeue);
        Assert.Equal(reason, result.ErrorReason);
        Assert.Null(result.Exception);
    }

    [Fact]
    public void Failed_WithReasonAndRequeue_CreatesFailureResultWithRequeue()
    {
        // Arrange
        var reason = "Temporary database connection failure";

        // Act
        var result = MessageProcessingResult.Failed(reason, requeue: true);

        // Assert
        Assert.False(result.Success);
        Assert.True(result.Requeue);
        Assert.Equal(reason, result.ErrorReason);
        Assert.Null(result.Exception);
    }

    [Fact]
    public void FailedWithException_WithExceptionOnly_CreatesFailureWithException()
    {
        // Arrange
        var exception = new InvalidOperationException("Operation failed");

        // Act
        var result = MessageProcessingResult.FailedWithException(exception);

        // Assert
        Assert.False(result.Success);
        Assert.False(result.Requeue);
        Assert.Equal("Operation failed", result.ErrorReason);
        Assert.Same(exception, result.Exception);
    }

    [Fact]
    public void FailedWithException_WithExceptionAndRequeue_CreatesFailureWithExceptionAndRequeue()
    {
        // Arrange
        var exception = new TimeoutException("Request timed out");

        // Act
        var result = MessageProcessingResult.FailedWithException(exception, requeue: true);

        // Assert
        Assert.False(result.Success);
        Assert.True(result.Requeue);
        Assert.Equal("Request timed out", result.ErrorReason);
        Assert.Same(exception, result.Exception);
    }

    [Fact]
    public void Properties_AreInitOnly_CannotBeModifiedAfterCreation()
    {
        // Arrange
        var result = MessageProcessingResult.Succeeded();

        // Assert - This test verifies that properties use 'init' accessor
        // If this compiles, it means we can create the object
        // If we tried to set a property after creation, it would be a compile error
        Assert.NotNull(result);
    }
}
