using System.Text.Json;
using YTSkedy.Infrastructure.CalendarEvents;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Infrastructure.Test.CalendarEvents;

public class CalendarEventViewMapperTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void ToViewsForMonth_IncludedEntity_MapsCalendarEventFields()
    {
        var entity = CreateEntity(
            "6f9619ff8b864fb5bdfd4f5c2f2f16a1",
            new DateTimeOffset(2026, 06, 05, 17, 00, 00, TimeSpan.Zero),
            "2026-06-05T10:00:00",
            [new LocalizedDescription("en", "English stream 1", null)]);
        var criteria = new CalendarEventMonthCriteria(2026, 6);

        var result = CalendarEventViewMapper.ToViewsForMonth([entity], criteria);

        var calendarEvent = Assert.Single(result);
        Assert.Equal("6f9619ff8b864fb5bdfd4f5c2f2f16a1", calendarEvent.CalendarEventId);
        Assert.Equal(new DateTime(2026, 06, 05, 10, 00, 00), calendarEvent.Start.LocalDateTime);
        Assert.Equal("America/Vancouver", calendarEvent.Start.TimeZoneId);
        var description = Assert.Single(calendarEvent.Descriptions);
        Assert.Equal("en", description.Language);
        Assert.Equal("English stream 1", description.Title);
        Assert.Null(description.Description);
    }

    [Fact]
    public void ToViewsForMonth_MixedLocalMonths_FiltersByRequestedMonth()
    {
        var entities = new[]
        {
            CreateEntity(
                "11111111111111111111111111111111",
                new DateTimeOffset(2026, 05, 31, 15, 30, 00, TimeSpan.Zero),
                "2026-06-01T00:30:00",
                timeZoneId: "Asia/Tokyo"),
            CreateEntity(
                "22222222222222222222222222222222",
                new DateTimeOffset(2026, 06, 15, 17, 00, 00, TimeSpan.Zero),
                "2026-06-15T10:00:00"),
            CreateEntity(
                "33333333333333333333333333333333",
                new DateTimeOffset(2026, 07, 01, 06, 30, 00, TimeSpan.Zero),
                "2026-06-30T23:30:00"),
            CreateEntity(
                "44444444444444444444444444444444",
                new DateTimeOffset(2026, 06, 01, 06, 30, 00, TimeSpan.Zero),
                "2026-05-31T23:30:00"),
            CreateEntity(
                "55555555555555555555555555555555",
                new DateTimeOffset(2026, 07, 01, 07, 30, 00, TimeSpan.Zero),
                "2026-07-01T00:30:00")
        };
        var criteria = new CalendarEventMonthCriteria(2026, 6);

        var result = CalendarEventViewMapper.ToViewsForMonth(entities, criteria);

        Assert.Equal(
            [
                "11111111111111111111111111111111",
                "22222222222222222222222222222222",
                "33333333333333333333333333333333"
            ],
            result.Select(calendarEvent => calendarEvent.CalendarEventId));
    }

    [Fact]
    public void ToViewsForMonth_UnorderedEntities_SortsByScheduledStartUtcThenId()
    {
        var entities = new[]
        {
            CreateEntity(
                "cccccccccccccccccccccccccccccccc",
                new DateTimeOffset(2026, 06, 06, 17, 00, 00, TimeSpan.Zero),
                "2026-06-06T10:00:00"),
            CreateEntity(
                "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
                new DateTimeOffset(2026, 06, 05, 17, 00, 00, TimeSpan.Zero),
                "2026-06-05T10:00:00"),
            CreateEntity(
                "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                new DateTimeOffset(2026, 06, 05, 17, 00, 00, TimeSpan.Zero),
                "2026-06-05T10:00:00")
        };
        var criteria = new CalendarEventMonthCriteria(2026, 6);

        var result = CalendarEventViewMapper.ToViewsForMonth(entities, criteria);

        Assert.Equal(
            [
                "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
                "cccccccccccccccccccccccccccccccc"
            ],
            result.Select(calendarEvent => calendarEvent.CalendarEventId));
    }

    [Fact]
    public void ToViewsForMonth_MalformedDescriptionsJson_ThrowsInvalidOperationException()
    {
        var entity = CreateEntity(
            "6f9619ff8b864fb5bdfd4f5c2f2f16a1",
            new DateTimeOffset(2026, 06, 05, 17, 00, 00, TimeSpan.Zero),
            "2026-06-05T10:00:00");
        entity.DescriptionsJson = "{";
        var criteria = new CalendarEventMonthCriteria(2026, 6);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            CalendarEventViewMapper.ToViewsForMonth([entity], criteria));

        Assert.Contains("malformed descriptions JSON", exception.Message);
    }

    [Fact]
    public void ToViews_MixedLocalMonths_MapsEveryEntityWithoutFiltering()
    {
        var entities = new[]
        {
            CreateEntity(
                "22222222222222222222222222222222",
                new DateTimeOffset(2026, 06, 15, 17, 00, 00, TimeSpan.Zero),
                "2026-06-15T10:00:00"),
            CreateEntity(
                "77777777777777777777777777777777",
                new DateTimeOffset(2026, 07, 15, 17, 00, 00, TimeSpan.Zero),
                "2026-07-15T10:00:00"),
            CreateEntity(
                "88888888888888888888888888888888",
                new DateTimeOffset(2025, 11, 15, 17, 00, 00, TimeSpan.Zero),
                "2025-11-15T09:00:00")
        };

        var result = CalendarEventViewMapper.ToViews(entities);

        Assert.Equal(
            [
                "22222222222222222222222222222222",
                "77777777777777777777777777777777",
                "88888888888888888888888888888888"
            ],
            result.Select(calendarEvent => calendarEvent.CalendarEventId));
    }

    [Fact]
    public void ToViews_Entity_MapsCalendarEventFields()
    {
        var entity = CreateEntity(
            "6f9619ff8b864fb5bdfd4f5c2f2f16a1",
            new DateTimeOffset(2026, 06, 05, 17, 00, 00, TimeSpan.Zero),
            "2026-06-05T10:00:00",
            [new LocalizedDescription("en", "English stream 1", null)]);

        var result = CalendarEventViewMapper.ToViews([entity]);

        var calendarEvent = Assert.Single(result);
        Assert.Equal("6f9619ff8b864fb5bdfd4f5c2f2f16a1", calendarEvent.CalendarEventId);
        Assert.Equal(new DateTime(2026, 06, 05, 10, 00, 00), calendarEvent.Start.LocalDateTime);
        Assert.Equal("America/Vancouver", calendarEvent.Start.TimeZoneId);
        var description = Assert.Single(calendarEvent.Descriptions);
        Assert.Equal("English stream 1", description.Title);
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
            PartitionKey = CalendarEventPartitionKey.ForInstant(scheduledStartUtc),
            RowKey = CalendarEventStorageKey.RowKeyForScheduledStart(scheduledStartUtc),
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
