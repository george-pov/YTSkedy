using System.Text.Json;
using YTSkedy.Infrastructure.CalendarEvents;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Infrastructure.Test.CalendarEvents;

public class CalendarEventReadMapperTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void ToListItemsForMonth_IncludedEntity_MapsCalendarEventFields()
    {
        var entity = CreateEntity(
            "20260605T170000Z",
            new DateTimeOffset(2026, 06, 05, 17, 00, 00, TimeSpan.Zero),
            "2026-06-05T10:00:00",
            [
                new LocalizedDescription("en", "English stream 1", null)
            ]);
        var criteria = new CalendarEventMonthCriteria(2026, 6);

        var result = CalendarEventReadMapper.ToListItemsForMonth(
            [entity],
            criteria);

        var calendarEvent = Assert.Single(result);
        Assert.Equal("20260605T170000Z", calendarEvent.CalendarEventId);
        Assert.Equal(new DateTime(2026, 06, 05, 10, 00, 00), calendarEvent.Start.LocalDateTime);
        Assert.Equal("America/Vancouver", calendarEvent.Start.TimeZoneId);
        var description = Assert.Single(calendarEvent.Descriptions);
        Assert.Equal("en", description.Language);
        Assert.Equal("English stream 1", description.Title);
        Assert.Null(description.Description);
    }

    [Fact]
    public void ToListItemsForMonth_MixedLocalMonths_FiltersByRequestedMonth()
    {
        var entities = new[]
        {
            CreateEntity(
                "20260531T153000Z",
                new DateTimeOffset(2026, 05, 31, 15, 30, 00, TimeSpan.Zero),
                "2026-06-01T00:30:00",
                timeZoneId: "Asia/Tokyo"),
            CreateEntity(
                "20260615T170000Z",
                new DateTimeOffset(2026, 06, 15, 17, 00, 00, TimeSpan.Zero),
                "2026-06-15T10:00:00"),
            CreateEntity(
                "20260701T063000Z",
                new DateTimeOffset(2026, 07, 01, 06, 30, 00, TimeSpan.Zero),
                "2026-06-30T23:30:00"),
            CreateEntity(
                "20260601T063000Z",
                new DateTimeOffset(2026, 06, 01, 06, 30, 00, TimeSpan.Zero),
                "2026-05-31T23:30:00"),
            CreateEntity(
                "20260701T073000Z",
                new DateTimeOffset(2026, 07, 01, 07, 30, 00, TimeSpan.Zero),
                "2026-07-01T00:30:00")
        };
        var criteria = new CalendarEventMonthCriteria(2026, 6);

        var result = CalendarEventReadMapper.ToListItemsForMonth(
            entities,
            criteria);

        Assert.Equal(
            [
                "20260531T153000Z",
                "20260615T170000Z",
                "20260701T063000Z"
            ],
            result.Select(calendarEvent => calendarEvent.CalendarEventId));
    }

    [Fact]
    public void ToListItemsForMonth_UnorderedEntities_SortsByScheduledStartUtcThenId()
    {
        var entities = new[]
        {
            CreateEntity(
                "20260606T170000Z",
                new DateTimeOffset(2026, 06, 06, 17, 00, 00, TimeSpan.Zero),
                "2026-06-06T10:00:00"),
            CreateEntity(
                "20260605T170000Z-B",
                new DateTimeOffset(2026, 06, 05, 17, 00, 00, TimeSpan.Zero),
                "2026-06-05T10:00:00"),
            CreateEntity(
                "20260605T170000Z-A",
                new DateTimeOffset(2026, 06, 05, 17, 00, 00, TimeSpan.Zero),
                "2026-06-05T10:00:00")
        };
        var criteria = new CalendarEventMonthCriteria(2026, 6);

        var result = CalendarEventReadMapper.ToListItemsForMonth(
            entities,
            criteria);

        Assert.Equal(
            [
                "20260605T170000Z-A",
                "20260605T170000Z-B",
                "20260606T170000Z"
            ],
            result.Select(calendarEvent => calendarEvent.CalendarEventId));
    }

    [Fact]
    public void ToListItemsForMonth_MalformedDescriptionsJson_ThrowsInvalidOperationException()
    {
        var entity = CreateEntity(
            "20260605T170000Z",
            new DateTimeOffset(2026, 06, 05, 17, 00, 00, TimeSpan.Zero),
            "2026-06-05T10:00:00");
        entity.DescriptionsJson = "{";
        var criteria = new CalendarEventMonthCriteria(2026, 6);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            CalendarEventReadMapper.ToListItemsForMonth(
                [entity],
                criteria));

        Assert.Contains("malformed descriptions JSON", exception.Message);
    }

    [Fact]
    public void GetPartitionKeysForLocalMonth_June2026_ReturnsAdjacentMonthKeys()
    {
        var criteria = new CalendarEventMonthCriteria(2026, 6);

        var result = CalendarEventReadMapper.GetPartitionKeysForLocalMonth(criteria);

        Assert.Equal(
            [
                "calendar-events-202605",
                "calendar-events-202606",
                "calendar-events-202607"
            ],
            result);
    }

    [Fact]
    public void GetPartitionKeysForLocalMonth_December9999_ReturnsRepresentableMonthKeys()
    {
        var criteria = new CalendarEventMonthCriteria(9999, 12);

        var result = CalendarEventReadMapper.GetPartitionKeysForLocalMonth(criteria);

        Assert.Equal(
            [
                "calendar-events-999911",
                "calendar-events-999912"
            ],
            result);
    }

    private static CalendarEventEntity CreateEntity(
        string calendarEventId,
        DateTimeOffset scheduledStartUtc,
        string localDateTime) =>
        CreateEntity(
            calendarEventId,
            scheduledStartUtc,
            localDateTime,
            [
                new LocalizedDescription(
                    "en",
                    $"English stream {calendarEventId}",
                    $"Description for {calendarEventId}")
            ]);

    private static CalendarEventEntity CreateEntity(
        string calendarEventId,
        DateTimeOffset scheduledStartUtc,
        string localDateTime,
        IReadOnlyList<LocalizedDescription>? descriptions = null,
        string timeZoneId = "America/Vancouver") =>
        new()
        {
            PartitionKey = AzureCalendarEventRepository.GetPartitionKey(scheduledStartUtc),
            RowKey = calendarEventId,
            CalendarEventId = calendarEventId,
            ScheduledStartUtc = scheduledStartUtc,
            LocalDateTime = localDateTime,
            TimeZoneId = timeZoneId,
            DescriptionsJson = JsonSerializer.Serialize(
                descriptions ??
                [
                    new LocalizedDescription(
                        "en",
                        $"English stream {calendarEventId}",
                        $"Description for {calendarEventId}")
                ],
                JsonOptions),
            CreatedUtc = new DateTimeOffset(2026, 01, 01, 00, 00, 00, TimeSpan.Zero)
        };
}
