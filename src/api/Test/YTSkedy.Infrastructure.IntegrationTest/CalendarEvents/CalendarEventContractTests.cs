using Azure;
using Azure.Data.Tables;
using YTSkedy.Infrastructure.CalendarEvents;
using YTSkedy.Infrastructure.IntegrationTest.TestSupport;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Domain.CalendarEvents;
using YTSkedy.Scheduling.TestSupport;
using YTSkedy.TestSupport;

namespace YTSkedy.Infrastructure.IntegrationTest.CalendarEvents;

[Collection(AzuriteTableCollection.Name)]
public sealed class CalendarEventContractTests(AzuriteTableFixture fixture)
{
    [AzuriteFact]
    public async Task CalendarRepository_CRUDMonthAndConcurrencyContracts_WorkAgainstAzurite()
    {
        var table = await fixture.CreateTableAsync("Calendar");
        var repository = Repository(table);
        var eventId = await repository.CreateAsync(
            CalendarEvent(new DateTime(2026, 6, 1, 0, 0, 0)),
            new DateTimeOffset(2026, 6, 1, 7, 0, 0, TimeSpan.Zero),
            CancellationToken.None);
        await repository.CreateAsync(
            CalendarEvent(new DateTime(2026, 7, 1, 0, 0, 0)),
            new DateTimeOffset(2026, 7, 1, 7, 0, 0, TimeSpan.Zero),
            CancellationToken.None);

        var month = await repository.ListAsync(
            new CalendarEventMonthCriteria(2026, 6),
            CancellationToken.None);
        var read = await repository.GetByIdAsync(eventId, CancellationToken.None);
        var update = await repository.UpdateAsync(
            eventId,
            CalendarEvent(new DateTime(2026, 6, 30, 23, 59, 59)),
            new DateTimeOffset(2026, 7, 1, 6, 59, 59, TimeSpan.Zero),
            CancellationToken.None);

        Assert.Single(month);
        Assert.NotNull(read);
        Assert.Equal(CalendarEventChangeResult.Applied, update);

        var address = new TableEntity(
            "calendar-events",
            $"event-{eventId}");
        var duplicate = await Assert.ThrowsAsync<RequestFailedException>(
            () => table.AddEntityAsync(address));
        Assert.Equal(409, duplicate.Status);

        var first = await table.GetEntityAsync<TableEntity>(
            "calendar-events",
            $"event-{eventId}");
        var second = await table.GetEntityAsync<TableEntity>(
            "calendar-events",
            $"event-{eventId}");
        first.Value["TimeZoneId"] = "UTC";
        await table.UpdateEntityAsync(first.Value, first.Value.ETag, TableUpdateMode.Merge);
        second.Value["TimeZoneId"] = "Europe/London";
        var stale = await Assert.ThrowsAsync<RequestFailedException>(
            () => table.UpdateEntityAsync(
                second.Value,
                second.Value.ETag,
                TableUpdateMode.Merge));
        Assert.Equal(412, stale.Status);

        await repository.DeleteAsync(eventId, CancellationToken.None);
        Assert.Null(await repository.GetByIdAsync(eventId, CancellationToken.None));
    }

    [AzuriteFact]
    public async Task CalendarRepository_MissingTable_ReadsAreEmpty()
    {
        var repository = Repository(fixture.MissingTable("MissingCalendar"));

        Assert.Empty(await repository.ListAsync(null, CancellationToken.None));
        Assert.Null(await repository.GetByIdAsync(
            SchedulingSampleIds.CalendarEventId,
            CancellationToken.None));
    }

    private static AzureCalendarEventRepository Repository(TableClient table) =>
        new(table, new FixedTimeProvider(SchedulingSampleTimes.Now));

    private static CalendarEvent CalendarEvent(DateTime localStart) =>
        new(
            new ScheduledStart(localStart, "America/Vancouver"),
            SchedulingSamples.Text());
}
