using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;

namespace YTSkedy.AzureFunctions.Configuration;

internal static class AzureStorageClientFactory
{
    internal static TableClient CreateTableClient(
        IConfiguration configuration,
        string tableNameSetting,
        string defaultTableName) =>
        new(
            GetConnectionString(configuration, "Azure Table Storage"),
            GetConfiguredName(configuration, tableNameSetting, defaultTableName));

    internal static BlobContainerClient CreateBlobContainerClient(
        IConfiguration configuration,
        string containerName) =>
        new(GetConnectionString(configuration, "Azure Blob Storage"), containerName);

    private static string GetConnectionString(
        IConfiguration configuration,
        string storageDescription)
    {
        var connectionString =
            configuration["AzureStorage:ConnectionString"] ??
            configuration["AzureWebJobsStorage"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"{storageDescription} connection string is not configured.");
        }

        return connectionString;
    }

    private static string GetConfiguredName(
        IConfiguration configuration,
        string settingName,
        string defaultName)
    {
        var configuredName = configuration[settingName];
        return string.IsNullOrWhiteSpace(configuredName)
            ? defaultName
            : configuredName;
    }
}
