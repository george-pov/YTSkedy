using Microsoft.Extensions.Configuration;

namespace YTSkedy.AzureFunctions.Configuration;

internal static class AzureStorageConfiguration
{
    internal const string ThumbnailsContainerNameSetting =
        "AzureStorage:ThumbnailsContainerName";

    internal const string DefaultThumbnailsContainerName =
        "calendar-event-thumbnails";

    internal static string GetThumbnailsContainerName(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var containerName = configuration[ThumbnailsContainerNameSetting];
        return string.IsNullOrWhiteSpace(containerName)
            ? DefaultThumbnailsContainerName
            : containerName;
    }
}
