using Microsoft.Extensions.Configuration;
using YTSkedy.AzureFunctions.Configuration;

namespace YTSkedy.AzureFunctions.Test.Configuration;

public sealed class AzureStorageConfigurationTests
{
    [Fact]
    public void GetThumbnailsContainerName_MissingSetting_ReturnsValidDefault()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([])
            .Build();

        var containerName = AzureStorageConfiguration.GetThumbnailsContainerName(configuration);

        Assert.Equal("calendar-event-thumbnails", containerName);
    }

    [Fact]
    public void GetThumbnailsContainerName_ConfiguredSetting_ReturnsConfiguredValue()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [AzureStorageConfiguration.ThumbnailsContainerNameSetting] =
                    "custom-thumbnails"
            })
            .Build();

        var containerName = AzureStorageConfiguration.GetThumbnailsContainerName(configuration);

        Assert.Equal("custom-thumbnails", containerName);
    }
}
