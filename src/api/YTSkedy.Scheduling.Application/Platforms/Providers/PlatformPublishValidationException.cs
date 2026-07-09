namespace YTSkedy.Scheduling.Application.Platforms.Providers;

/// <summary>
/// Indicates that provider-specific publish settings are not valid for the
/// current publish request and can be corrected by changing local settings.
/// </summary>
public sealed class PlatformPublishValidationException : Exception
{
    public PlatformPublishValidationException(string message)
        : base(message)
    {
    }

    public PlatformPublishValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
