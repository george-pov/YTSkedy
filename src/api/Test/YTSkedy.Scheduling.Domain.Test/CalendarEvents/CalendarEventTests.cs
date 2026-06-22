using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Domain.Test.CalendarEvents;

public class CalendarEventTests
{
    [Fact]
    public void Constructor_ValidInput_SetsProviderNeutralProperties()
    {
        var start = new ScheduledStart(
            new DateTime(2026, 6, 6, 10, 0, 0),
            "America/Vancouver");
        LocalizedDescription[] descriptions =
        [
            new("en", "English stream", "English description")
        ];

        var calendarEvent = new CalendarEvent(start, descriptions);

        Assert.Equal(start, calendarEvent.Start);
        Assert.Equal(descriptions, calendarEvent.Descriptions);
    }
}
