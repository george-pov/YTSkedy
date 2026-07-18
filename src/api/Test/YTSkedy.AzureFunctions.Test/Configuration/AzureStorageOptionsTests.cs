using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using YTSkedy.AzureFunctions.Configuration;

namespace YTSkedy.AzureFunctions.Test.Configuration;

public sealed class AzureStorageOptionsTests
{
    private readonly AzureStorageOptionsValidator _validator = new();

    [Fact]
    public void Defaults_UseExistingResourceNames()
    {
        var options = new AzureStorageOptions();

        Assert.Equal("CalendarEvents", options.CalendarEventsTableName);
        Assert.Equal("Templates", options.TemplatesTableName);
        Assert.Equal("ApplicationSettings", options.ApplicationSettingsTableName);
        Assert.Equal("Platforms", options.PlatformsTableName);
        Assert.Equal("PlatformPublications", options.PlatformPublicationsTableName);
        Assert.Equal("calendar-event-thumbnails", options.ThumbnailsContainerName);
    }

    [Fact]
    public void Validate_ConnectionStringMode_Succeeds()
    {
        var result = _validator.Validate(
            null,
            new AzureStorageOptions
            {
                ConnectionString = "UseDevelopmentStorage=true"
            });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_ServiceUriMode_Succeeds()
    {
        var result = _validator.Validate(
            null,
            new AzureStorageOptions
            {
                TableServiceUri = "https://storage.example.test/",
                BlobServiceUri = "https://storage.example.test/"
            });

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(true, "https://storage.example.test/", "https://storage.example.test/")]
    [InlineData(false, "https://storage.example.test/", null)]
    [InlineData(false, "relative", "https://storage.example.test/")]
    [InlineData(false, "http://storage.example.test/", "https://storage.example.test/")]
    public void Validate_InvalidAuthenticationMode_FailsWithoutValues(
        bool includeConnectionString,
        string? tableServiceUri,
        string? blobServiceUri)
    {
        const string secret = "AccountKey=do-not-report";
        var result = _validator.Validate(
            null,
            new AzureStorageOptions
            {
                ConnectionString = includeConnectionString ? secret : null,
                TableServiceUri = tableServiceUri,
                BlobServiceUri = blobServiceUri
            });

        Assert.False(result.Succeeded);
        Assert.DoesNotContain(secret, string.Join(" ", result.Failures ?? []));
    }

    [Fact]
    public void LocalSample_UsesExplicitApplicationStorage()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "local.settings.sample.json"),
                optional: false)
            .Build();

        Assert.Equal(
            "UseDevelopmentStorage=true",
            configuration["Values:AzureStorage:ConnectionString"]);
        Assert.Equal(
            "true",
            configuration["Values:AzureStorage:CreateResourcesIfMissing"]);
        Assert.Equal(
            "UseDevelopmentStorage=true",
            configuration["Values:AzureWebJobsStorage"]);
    }
}
