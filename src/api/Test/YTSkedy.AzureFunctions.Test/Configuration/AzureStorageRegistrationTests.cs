using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YTSkedy.AzureFunctions.Configuration;
using YTSkedy.Infrastructure.Settings;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.Settings;

namespace YTSkedy.AzureFunctions.Test.Configuration;

public sealed class AzureStorageRegistrationTests
{
    [Fact]
    public void AddYTSkedyStorage_RegistersNamedClientsAndForwardsSettingsPorts()
    {
        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        services.AddYTSkedyStorage(Configuration(
            ("AzureStorage:ConnectionString", "UseDevelopmentStorage=true")));
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var calendarClient = provider.GetRequiredKeyedService<TableClient>(
            AzureStorageRegistration.CalendarEventsClientKey);
        var templateClient = provider.GetRequiredKeyedService<TableClient>(
            AzureStorageRegistration.TemplatesClientKey);
        var settingsReader = scope.ServiceProvider
            .GetRequiredService<IEventTextFieldsReader>();
        var startReader = scope.ServiceProvider
            .GetRequiredService<IStartDefaultsReader>();
        var modifier = scope.ServiceProvider
            .GetRequiredService<ICalendarEventDefaultsModifier>();

        Assert.Equal("CalendarEvents", calendarClient.Name);
        Assert.Equal("Templates", templateClient.Name);
        Assert.Same(settingsReader, startReader);
        Assert.Same(settingsReader, modifier);
        Assert.IsType<AzureCalendarEventDefaultsRepository>(settingsReader);
        Assert.Single(scope.ServiceProvider.GetServices<ICalendarEventReader>());
    }

    [Fact]
    public void AddYTSkedyStorage_ServiceUriMode_UsesConfiguredResourceNames()
    {
        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        services.AddYTSkedyStorage(Configuration(
            ("AzureStorage:TableServiceUri", "https://tables.example.test/"),
            ("AzureStorage:BlobServiceUri", "https://blobs.example.test/"),
            ("AzureStorage:PlatformsTableName", "CustomPlatforms")));
        using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredKeyedService<TableClient>(
            AzureStorageRegistration.PlatformsClientKey);

        Assert.Equal("CustomPlatforms", client.Name);
        Assert.Equal("tables.example.test", client.Uri.Host);
    }

    private static IConfiguration Configuration(
        params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(
                item => item.Key,
                item => (string?)item.Value))
            .Build();
}
