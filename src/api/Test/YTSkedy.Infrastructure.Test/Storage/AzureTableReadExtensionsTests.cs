using YTSkedy.Infrastructure.CalendarEvents;
using YTSkedy.Infrastructure.Storage;
using YTSkedy.Infrastructure.Test.TestSupport;

namespace YTSkedy.Infrastructure.Test.Storage;

public sealed class AzureTableReadExtensionsTests
{
    [Fact]
    public async Task GetEntityOrNullAsync_PresentAndAbsentRows_ReturnExpectedValues()
    {
        var table = new CalendarEventTableClient();
        var entity = Entity();
        table.Seed(entity);

        var present = await table.GetEntityOrNullAsync<CalendarEventEntity>(
            entity.PartitionKey,
            entity.RowKey,
            CancellationToken.None);
        var absent = await table.GetEntityOrNullAsync<CalendarEventEntity>(
            entity.PartitionKey,
            "event-missing",
            CancellationToken.None);

        Assert.NotNull(present);
        Assert.Null(absent);
    }

    [Fact]
    public async Task ListEntitiesAsync_FilterAndProjection_ArePassedToClient()
    {
        var table = new CalendarEventTableClient();
        table.Seed(Entity());

        var result = await table.ListEntitiesAsync<CalendarEventEntity>(
            CalendarEventStorageKey.PartitionFilter(),
            [nameof(CalendarEventEntity.RowKey)],
            CancellationToken.None);

        Assert.Single(result);
        Assert.Equal([nameof(CalendarEventEntity.RowKey)], table.LastQuerySelect);
    }

    [Fact]
    public async Task AnyEntityAsync_UsesOneResultAndKeyOnlyProjection()
    {
        var table = new CalendarEventTableClient();
        table.Seed(Entity());

        var result = await table.AnyEntityAsync<CalendarEventEntity>(
            CalendarEventStorageKey.PartitionFilter(),
            CancellationToken.None);

        Assert.True(result);
        Assert.Equal(1, table.LastQueryMaxPerPage);
        Assert.Equal([nameof(CalendarEventEntity.PartitionKey)], table.LastQuerySelect);
        Assert.Equal(1, table.QueryCallCount);
    }

    private static CalendarEventEntity Entity() =>
        new()
        {
            PartitionKey = CalendarEventStorageKey.PartitionKey,
            RowKey = "event-6f9619ff8b864fb5bdfd4f5c2f2f16a1",
            CalendarEventId = "6f9619ff8b864fb5bdfd4f5c2f2f16a1",
            ScheduledStartUtc = new DateTimeOffset(
                2026,
                6,
                15,
                17,
                0,
                0,
                TimeSpan.Zero),
            LocalDateTime = "2026-06-15T10:00:00",
            TimeZoneId = "America/Vancouver",
            TextJson = "{}",
            PublishedPlatformIdsJson = "[]",
            CreatedUtc = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero)
        };
}
