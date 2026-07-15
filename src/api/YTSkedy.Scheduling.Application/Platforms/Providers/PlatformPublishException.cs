namespace YTSkedy.Scheduling.Application.Platforms.Providers;

/// <summary>
/// Raised by an <see cref="IPlatformPublisher"/> when the external publish fails,
/// including when the provider's credentials are not configured. The publish use
/// case maps this to a <c>502 Bad Gateway</c> external-dependency failure. When
/// the provider created a resource before a later step failed,
/// <see cref="ExternalResourceId"/> carries its safe provider id for operator
/// troubleshooting. The message must never contain secrets, tokens, or raw
/// provider credentials.
/// </summary>
public sealed class PlatformPublishException : Exception
{
    public PlatformPublishException(string message)
        : this(
            message,
            externalResourceId: null,
            PlatformPublishFailureKind.ProviderFailure,
            innerException: null)
    {
    }

    public PlatformPublishException(string message, Exception innerException)
        : this(
            message,
            externalResourceId: null,
            PlatformPublishFailureKind.ProviderFailure,
            innerException)
    {
    }

    public PlatformPublishException(
        string message,
        string? externalResourceId,
        Exception? innerException = null)
        : this(
            message,
            externalResourceId,
            PlatformPublishFailureKind.ProviderFailure,
            innerException)
    {
    }

    public PlatformPublishException(
        string message,
        string? externalResourceId,
        PlatformPublishFailureKind failureKind,
        Exception? innerException = null)
        : base(message, innerException)
    {
        ExternalResourceId = string.IsNullOrWhiteSpace(externalResourceId)
            ? null
            : externalResourceId.Trim();
        FailureKind = failureKind;
    }

    public string? ExternalResourceId { get; }

    public PlatformPublishFailureKind FailureKind { get; }
}
