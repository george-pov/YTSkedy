using System.Text.Json;
using YTSkedy.Infrastructure.CalendarEvents;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Infrastructure.Test.CalendarEvents;

public class CalendarEventViewMapperTests
{
    [Fact]
    public void ToListRecordsForMonth_IncludedEntity_MapsCalendarEventFieldsAndPublishedIds()
    {
        var entity = CreateEntity(
            "6f9619ff8b864fb5bdfd4f5c2f2f16a1",
            new DateTimeOffset(2026, 06, 05, 17, 00, 00, TimeSpan.Zero),
            "2026-06-05T10:00:00",
            Text("English stream 1", "Event description"));
        var criteria = new CalendarEventMonthCriteria(2026, 6);

        entity.PublishedPlatformIdsJson = "[\"platform-b\",\"platform-a\"]";

        var result = CalendarEventViewMapper.ToListRecordsForMonth([entity], criteria);

        var record = Assert.Single(result);
        var calendarEvent = record.Event;
        Assert.Equal("6f9619ff8b864fb5bdfd4f5c2f2f16a1", calendarEvent.CalendarEventId);
        Assert.Equal(new DateTime(2026, 06, 05, 10, 00, 00), calendarEvent.Start.LocalDateTime);
        Assert.Equal("America/Vancouver", calendarEvent.Start.TimeZoneId);
        Assert.Collection(
            calendarEvent.Text.Fields,
            first =>
            {
                Assert.Equal("text1", first.FieldKey);
                Assert.Equal("Title", first.Label);
                Assert.Equal(EventTextType.ShortText, first.Type);
                Assert.Equal(50, first.MaxLength);
            },
            second =>
            {
                Assert.Equal("text2", second.FieldKey);
                Assert.Equal("Description", second.Label);
                Assert.Equal(EventTextType.LongText, second.Type);
                Assert.Equal(2500, second.MaxLength);
            });
        Assert.Equal(
            ["English stream 1", "Event description"],
            calendarEvent.Text.Values.Select(value => value.Value));
        Assert.Equal(
            ["platform-a", "platform-b"],
            record.PublishedPlatformIds.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void ToListRecordsForMonth_MixedLocalMonths_FiltersByRequestedMonth()
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

        var result = CalendarEventViewMapper.ToListRecordsForMonth(entities, criteria);

        Assert.Equal(
            [
                "11111111111111111111111111111111",
                "22222222222222222222222222222222",
                "33333333333333333333333333333333"
            ],
            result.Select(record => record.Event.CalendarEventId));
    }

    [Fact]
    public void ToListRecordsForMonth_UnorderedEntities_SortsByScheduledStartUtcThenId()
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

        var result = CalendarEventViewMapper.ToListRecordsForMonth(entities, criteria);

        Assert.Equal(
            [
                "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
                "cccccccccccccccccccccccccccccccc"
            ],
            result.Select(record => record.Event.CalendarEventId));
    }

    [Fact]
    public void ToListRecordsForMonth_MalformedTextJson_ThrowsInvalidOperationException()
    {
        var entity = CreateEntity(
            "6f9619ff8b864fb5bdfd4f5c2f2f16a1",
            new DateTimeOffset(2026, 06, 05, 17, 00, 00, TimeSpan.Zero),
            "2026-06-05T10:00:00");
        entity.TextJson = "{";
        var criteria = new CalendarEventMonthCriteria(2026, 6);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            CalendarEventViewMapper.ToListRecordsForMonth([entity], criteria));

        Assert.Contains("malformed text JSON", exception.Message);
    }

    [Fact]
    public void ToListRecords_MixedLocalMonths_MapsEveryEntityWithoutFiltering()
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

        var result = CalendarEventViewMapper.ToListRecords(entities);

        Assert.Equal(
            [
                "22222222222222222222222222222222",
                "77777777777777777777777777777777",
                "88888888888888888888888888888888"
            ],
            result.Select(record => record.Event.CalendarEventId));
    }

    [Fact]
    public void ToListRecords_Entity_MapsCalendarEventFields()
    {
        var entity = CreateEntity(
            "6f9619ff8b864fb5bdfd4f5c2f2f16a1",
            new DateTimeOffset(2026, 06, 05, 17, 00, 00, TimeSpan.Zero),
            "2026-06-05T10:00:00",
            Text("English stream 1", "Event description"));

        var result = CalendarEventViewMapper.ToListRecords([entity]);

        var calendarEvent = Assert.Single(result).Event;
        Assert.Equal("6f9619ff8b864fb5bdfd4f5c2f2f16a1", calendarEvent.CalendarEventId);
        Assert.Equal(new DateTime(2026, 06, 05, 10, 00, 00), calendarEvent.Start.LocalDateTime);
        Assert.Equal("America/Vancouver", calendarEvent.Start.TimeZoneId);
        Assert.Equal("English stream 1", calendarEvent.Text.ValueFor("text1"));
    }

    [Fact]
    public void SerializeText_TextSnapshot_WritesStoredShape()
    {
        var json = CalendarEventViewMapper.SerializeText(Text("English stream 1", "Event description"));

        using var document = JsonDocument.Parse(json);
        var fields = document.RootElement.GetProperty("fields");
        var values = document.RootElement.GetProperty("values");
        Assert.Equal("text1", fields[0].GetProperty("fieldKey").GetString());
        Assert.Equal("Title", fields[0].GetProperty("label").GetString());
        Assert.Equal("ShortText", fields[0].GetProperty("type").GetString());
        Assert.Equal(50, fields[0].GetProperty("maxLength").GetInt32());
        Assert.Equal("text1", values[0].GetProperty("fieldKey").GetString());
        Assert.Equal("English stream 1", values[0].GetProperty("value").GetString());
    }

    private static CalendarEventEntity CreateEntity(
        string calendarEventId,
        DateTimeOffset scheduledStartUtc,
        string localDateTime) =>
        CreateEntity(
            calendarEventId,
            scheduledStartUtc,
            localDateTime,
            text: null);

    private static CalendarEventEntity CreateEntity(
        string calendarEventId,
        DateTimeOffset scheduledStartUtc,
        string localDateTime,
        EventTextSnapshot? text = null,
        string timeZoneId = "America/Vancouver") =>
        new()
        {
            PartitionKey = CalendarEventStorageKey.PartitionKey,
            RowKey = CalendarEventStorageKey.RowKeyFor(calendarEventId),
            CalendarEventId = calendarEventId,
            ScheduledStartUtc = scheduledStartUtc,
            LocalDateTime = localDateTime,
            TimeZoneId = timeZoneId,
            TextJson = CalendarEventViewMapper.SerializeText(
                text ?? Text(
                    $"English stream {calendarEventId}",
                    $"Description for {calendarEventId}")),
            PublishedPlatformIdsJson = "[]",
            CreatedUtc = new DateTimeOffset(2026, 01, 01, 00, 00, 00, TimeSpan.Zero)
        };

    private static EventTextSnapshot Text(
        string title,
        string description) =>
        EventTextSnapshot.Create(
            EventTextFields.Default,
            [
                new EventTextValue("text1", title),
                new EventTextValue("text2", description)
            ]);
}
