namespace YTSkedy.Scheduling.Application.Platforms;

/// <summary>
/// Raised by an <see cref="IPlatformPublisher"/> when the external publish fails,
/// including when the provider's credentials are not configured. The publish use
/// case maps this to a <c>502 Bad Gateway</c> external-dependency failure and
/// releases the reservation. The message must never contain secrets, tokens, or
/// raw provider credentials.
/// </summary>
public sealed class PlatformPublishException : Exception
{
    public PlatformPublishException(string message)
        : base(message)
    {
    }

    public PlatformPublishException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
