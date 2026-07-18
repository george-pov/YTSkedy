using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using YTSkedy.AzureFunctions.Configuration;

namespace YTSkedy.AzureFunctions.Test.Configuration;

public sealed class AzureStorageInitializerTests
{
    [Fact]
    public async Task StartAsync_Enabled_CreatesEachConfiguredResourceOnce()
    {
        var tables = new Mock<TableServiceClient>();
        tables
            .Setup(client => client.CreateTableIfNotExistsAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Azure.Response<Azure.Data.Tables.Models.TableItem>)null!);
        var container = new Mock<BlobContainerClient>();
        container
            .Setup(client => client.CreateIfNotExistsAsync(
                PublicAccessType.None,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Azure.Response<BlobContainerInfo>)null!);
        var options = new AzureStorageOptions
        {
            ConnectionString = "UseDevelopmentStorage=true",
            CreateResourcesIfMissing = true
        };
        var initializer = new AzureStorageInitializer(
            tables.Object,
            container.Object,
            Options.Create(options));

        await initializer.StartAsync(CancellationToken.None);

        foreach (var tableName in new[]
                 {
                     options.CalendarEventsTableName,
                     options.TemplatesTableName,
                     options.ApplicationSettingsTableName,
                     options.PlatformsTableName,
                     options.PlatformPublicationsTableName
                 })
        {
            tables.Verify(client => client.CreateTableIfNotExistsAsync(
                tableName,
                CancellationToken.None), Times.Once());
        }
        container.Verify(client => client.CreateIfNotExistsAsync(
            PublicAccessType.None,
            null,
            null,
            CancellationToken.None), Times.Once());
    }

    [Fact]
    public async Task StartAsync_Disabled_PerformsNoCreateCalls()
    {
        var tables = new Mock<TableServiceClient>();
        var container = new Mock<BlobContainerClient>();
        var initializer = new AzureStorageInitializer(
            tables.Object,
            container.Object,
            Options.Create(new AzureStorageOptions
            {
                ConnectionString = "UseDevelopmentStorage=true"
            }));

        await initializer.StartAsync(CancellationToken.None);

        Assert.Empty(tables.Invocations);
        Assert.Empty(container.Invocations);
    }
}
