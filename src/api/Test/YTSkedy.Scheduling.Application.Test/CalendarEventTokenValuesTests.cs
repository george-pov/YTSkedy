using YTSkedy.Scheduling.Application.Templates;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Test;

public class CalendarEventTokenValuesTests
{
    [Fact]
    public void From_EnglishAndRussianDescriptions_ReturnsTitleAndDescriptionTokens()
    {
        var calendarEvent = Event(
            new DateTime(2026, 6, 5, 10, 30, 0),
            [
                new LocalizedDescription("ru", "Russian title", "Russian description"),
                new LocalizedDescription("en", "English title", "English description")
            ]);

        var values = CalendarEventTokenValues.From(calendarEvent).Values;

        Assert.Equal("English title", values["title"]);
        Assert.Equal("English description", values["description"]);
        Assert.Equal("Russian title", values["titleRu"]);
        Assert.Equal("Russian description", values["descriptionRu"]);
    }

    [Fact]
    public void From_MissingOptionalDescriptions_ReturnsEmptyDescriptionTokens()
    {
        var calendarEvent = Event(
            new DateTime(2026, 6, 5, 10, 30, 0),
            [
                new LocalizedDescription("en", "English title", null),
                new LocalizedDescription("ru", "Russian title", null)
            ]);

        var values = CalendarEventTokenValues.From(calendarEvent).Values;

        Assert.Equal(string.Empty, values["description"]);
        Assert.Equal(string.Empty, values["descriptionRu"]);
    }

    [Fact]
    public void From_LocalStart_ReturnsDeterministicDateTokens()
    {
        var calendarEvent = Event(
            new DateTime(2026, 6, 5, 10, 30, 0),
            [new LocalizedDescription("en", "English title", null)]);

        var values = CalendarEventTokenValues.From(calendarEvent).Values;

        Assert.Equal("June 5, 2026", values["longDate"]);
        Assert.Equal("5 \u0438\u044e\u043d\u044f 2026", values["longDateRu"]);
        Assert.Equal("2026-06-05", values["shortDate"]);
        Assert.Equal("05.06.2026", values["shortDateRu"]);
    }

    [Fact]
    public void From_ScheduledStartUtcDiffers_UsesLocalDate()
    {
        var calendarEvent = new CalendarEventView(
            "calendar-event-id",
            new ScheduledStart(new DateTime(2026, 1, 2, 23, 30, 0), "America/Vancouver"),
            new DateTimeOffset(2030, 12, 31, 7, 30, 0, TimeSpan.Zero),
            [new LocalizedDescription("en", "English title", null)]);

        var values = CalendarEventTokenValues.From(calendarEvent).Values;

        Assert.Equal("January 2, 2026", values["longDate"]);
        Assert.Equal("2026-01-02", values["shortDate"]);
    }

    private static CalendarEventView Event(
        DateTime localStart,
        IReadOnlyList<LocalizedDescription> descriptions) =>
        new(
            "calendar-event-id",
            new ScheduledStart(localStart, "America/Vancouver"),
            new DateTimeOffset(2026, 6, 5, 17, 30, 0, TimeSpan.Zero),
            descriptions);
}
