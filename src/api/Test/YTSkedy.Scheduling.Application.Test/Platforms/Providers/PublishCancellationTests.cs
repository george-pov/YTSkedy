using YTSkedy.Scheduling.Application.Platforms.Providers;

namespace YTSkedy.Scheduling.Application.Test.Platforms.Providers;

public sealed class PublishCancellationTests
{
    [Fact]
    public void IsCallerCancellation_CanceledCallerToken_ReturnsTrue()
    {
        using var caller = new CancellationTokenSource();
        caller.Cancel();

        Assert.True(PublishCancellationClassifier.IsCallerCancellation(
            new OperationCanceledException(caller.Token),
            caller.Token));
    }

    [Fact]
    public void ToPublishException_DependencyTimeout_ClassifiesTimeout()
    {
        var timeout = new TaskCanceledException(
            "dependency timeout",
            new TimeoutException("deadline"));

        var result = PublishCancellationClassifier.ToPublishException(
            timeout,
            "WordPress",
            "event-id");

        Assert.Equal(PlatformPublishFailureKind.Timeout, result.FailureKind);
        Assert.Same(timeout, result.InnerException);
    }

    [Fact]
    public void ToPublishException_UnsignaledCancellation_ClassifiesUnexpected()
    {
        var cancellation = new OperationCanceledException();

        var result = PublishCancellationClassifier.ToPublishException(
            cancellation,
            "YouTube",
            "event-id");

        Assert.Equal(PlatformPublishFailureKind.UnexpectedCancellation, result.FailureKind);
    }

    [Fact]
    public void PlatformPublishException_OrdinaryFailure_DefaultsToProviderFailure()
    {
        var result = new PlatformPublishException("provider failed");

        Assert.Equal(PlatformPublishFailureKind.ProviderFailure, result.FailureKind);
    }
}
