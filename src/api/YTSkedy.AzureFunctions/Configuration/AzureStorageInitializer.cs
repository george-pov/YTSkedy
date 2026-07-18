using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace YTSkedy.AzureFunctions.Configuration;

internal sealed class AzureStorageInitializer(
    TableServiceClient tableServiceClient,
    BlobContainerClient thumbnailContainer,
    IOptions<AzureStorageOptions> options) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var storage = options.Value;
        if (!storage.CreateResourcesIfMissing)
        {
            return;
        }

        foreach (var tableName in TableNames(storage))
        {
            await tableServiceClient.CreateTableIfNotExistsAsync(
                tableName,
                cancellationToken);
        }

        await thumbnailContainer.CreateIfNotExistsAsync(
            cancellationToken: cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) =>
        Task.CompletedTask;

    private static string[] TableNames(AzureStorageOptions options) =>
    [
        options.CalendarEventsTableName,
        options.TemplatesTableName,
        options.ApplicationSettingsTableName,
        options.PlatformsTableName,
        options.PlatformPublicationsTableName
    ];
}
