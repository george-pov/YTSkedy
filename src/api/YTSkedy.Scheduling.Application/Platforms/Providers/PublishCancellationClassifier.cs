namespace YTSkedy.Scheduling.Application.Platforms.Providers;

public static class PublishCancellationClassifier
{
    public static bool IsCallerCancellation(
        OperationCanceledException exception,
        CancellationToken callerToken)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return callerToken.IsCancellationRequested;
    }

    public static PlatformPublishException ToPublishException(
        OperationCanceledException exception,
        string providerName,
        string calendarEventId,
        string? externalResourceId = null)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);

        var failureKind = IsDependencyTimeout(exception)
            ? PlatformPublishFailureKind.Timeout
            : PlatformPublishFailureKind.UnexpectedCancellation;
        var reason = failureKind == PlatformPublishFailureKind.Timeout
            ? "timed out"
            : "was canceled unexpectedly";

        return new PlatformPublishException(
            $"Publishing calendar event '{calendarEventId}' to {providerName} {reason}.",
            externalResourceId,
            failureKind,
            exception);
    }

    private static bool IsDependencyTimeout(OperationCanceledException exception) =>
        exception.InnerException is TimeoutException;
}
