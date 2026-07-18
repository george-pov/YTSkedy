using Microsoft.Extensions.Options;

namespace YTSkedy.AzureFunctions.Configuration;

internal sealed class AzureStorageOptionsValidator : IValidateOptions<AzureStorageOptions>
{
    public ValidateOptionsResult Validate(string? name, AzureStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();
        var hasConnectionString = !string.IsNullOrWhiteSpace(options.ConnectionString);
        var hasTableServiceUri = !string.IsNullOrWhiteSpace(options.TableServiceUri);
        var hasBlobServiceUri = !string.IsNullOrWhiteSpace(options.BlobServiceUri);
        var hasAnyServiceUri = hasTableServiceUri || hasBlobServiceUri;

        if (hasConnectionString == hasAnyServiceUri)
        {
            failures.Add(
                "Configure exactly one AzureStorage authentication mode: " +
                "AzureStorage:ConnectionString, or both AzureStorage:TableServiceUri " +
                "and AzureStorage:BlobServiceUri.");
        }
        else if (!hasConnectionString && !(hasTableServiceUri && hasBlobServiceUri))
        {
            failures.Add(
                "AzureStorage:TableServiceUri and AzureStorage:BlobServiceUri " +
                "must both be configured for service URI authentication.");
        }

        ValidateServiceUri(
            options.TableServiceUri,
            "AzureStorage:TableServiceUri",
            failures);
        ValidateServiceUri(
            options.BlobServiceUri,
            "AzureStorage:BlobServiceUri",
            failures);

        ValidateName(
            options.CalendarEventsTableName,
            "AzureStorage:CalendarEventsTableName",
            failures);
        ValidateName(
            options.TemplatesTableName,
            "AzureStorage:TemplatesTableName",
            failures);
        ValidateName(
            options.ApplicationSettingsTableName,
            "AzureStorage:ApplicationSettingsTableName",
            failures);
        ValidateName(
            options.PlatformsTableName,
            "AzureStorage:PlatformsTableName",
            failures);
        ValidateName(
            options.PlatformPublicationsTableName,
            "AzureStorage:PlatformPublicationsTableName",
            failures);
        ValidateName(
            options.ThumbnailsContainerName,
            "AzureStorage:ThumbnailsContainerName",
            failures);

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateServiceUri(
        string? configuredUri,
        string key,
        ICollection<string> failures)
    {
        if (string.IsNullOrWhiteSpace(configuredUri))
        {
            return;
        }

        if (!Uri.TryCreate(configuredUri, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            failures.Add($"{key} must be an absolute HTTPS URI.");
        }
    }

    private static void ValidateName(
        string configuredName,
        string key,
        ICollection<string> failures)
    {
        if (string.IsNullOrWhiteSpace(configuredName))
        {
            failures.Add($"{key} must not be blank.");
        }
    }
}
