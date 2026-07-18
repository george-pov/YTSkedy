using Azure.Data.Tables;
using Azure.Identity;
using Azure.Storage.Blobs;

namespace YTSkedy.AzureFunctions.Configuration;

internal static class AzureStorageClientFactory
{
    internal static TableServiceClient CreateTableServiceClient(
        AzureStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return new TableServiceClient(options.ConnectionString);
        }

        return new TableServiceClient(
            new Uri(options.TableServiceUri!, UriKind.Absolute),
            new DefaultAzureCredential());
    }

    internal static BlobServiceClient CreateBlobServiceClient(
        AzureStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return new BlobServiceClient(options.ConnectionString);
        }

        return new BlobServiceClient(
            new Uri(options.BlobServiceUri!, UriKind.Absolute),
            new DefaultAzureCredential());
    }
}
