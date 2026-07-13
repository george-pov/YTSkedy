using YTSkedy.Infrastructure.Settings;
using YTSkedy.Infrastructure.Test.TestSupport;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Infrastructure.Test.Settings;

public sealed class AzureStartDefaultsRepositoryTests
{
    [Fact]
    public async Task GetAsync_MissingRow_ReturnsEmptyDefaults()
    {
        var repository = new AzureStartDefaultsRepository(new ApplicationSettingsTableClient());

        var result = await repository.GetAsync(CancellationToken.None);

        Assert.Equal(StartDefaults.Empty, result);
    }

    [Fact]
    public async Task SaveAsync_ThenGetAsync_ReplacesAndClearsOneSettingsRow()
    {
        var table = new ApplicationSettingsTableClient();
        var repository = new AzureStartDefaultsRepository(table);
        await repository.SaveAsync(
            new StartDefaults(DayOfWeek.Friday, new TimeOnly(14, 0), "UTC"),
            CancellationToken.None);
        await repository.SaveAsync(
            new StartDefaults(null, new TimeOnly(9, 30), null),
            CancellationToken.None);

        var result = await repository.GetAsync(CancellationToken.None);
        var entity = Assert.Single(table.Entities.Values);

        Assert.Equal(new StartDefaults(null, new TimeOnly(9, 30), null), result);
        Assert.Equal(ApplicationSettingsKey.PartitionKey, entity.PartitionKey);
        Assert.Equal(ApplicationSettingsKey.StartDefaultsRowKey, entity.RowKey);
        Assert.True(table.CreateIfNotExistsCalled);
    }
}
