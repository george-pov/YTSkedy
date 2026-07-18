namespace YTSkedy.AzureFunctions.Configuration;

internal sealed class AzureStorageOptions
{
    internal const string SectionName = "AzureStorage";

    public string? ConnectionString { get; init; }

    public string? TableServiceUri { get; init; }

    public string? BlobServiceUri { get; init; }

    public string CalendarEventsTableName { get; init; } = "CalendarEvents";

    public string TemplatesTableName { get; init; } = "Templates";

    public string ApplicationSettingsTableName { get; init; } = "ApplicationSettings";

    public string PlatformsTableName { get; init; } = "Platforms";

    public string PlatformPublicationsTableName { get; init; } = "PlatformPublications";

    public string ThumbnailsContainerName { get; init; } = "calendar-event-thumbnails";

    public bool CreateResourcesIfMissing { get; init; }
}
