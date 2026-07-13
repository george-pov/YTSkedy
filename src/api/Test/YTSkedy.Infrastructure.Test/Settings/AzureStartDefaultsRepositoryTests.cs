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
    public async Task GetAsync_StoredDefaults_ReturnsValues()
    {
        var table = new ApplicationSettingsTableClient();
        var repository = new AzureStartDefaultsRepository(table);
        var stored = new StartDefaults(null, new TimeOnly(9, 30), null);
        table.Seed(new ApplicationSettingsEntity
        {
            PartitionKey = ApplicationSettingsKey.PartitionKey,
            RowKey = ApplicationSettingsKey.StartDefaultsRowKey,
            ValueJson = StartDefaultsSerializer.Serialize(stored)
        });

        var result = await repository.GetAsync(CancellationToken.None);

        Assert.Equal(stored, result);
    }
}
