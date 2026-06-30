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
        var text = EventTextSnapshot.Create(
            EventTextFields.Default,
            [
                new EventTextValue("text1", "English stream"),
                new EventTextValue("text2", "English description")
            ]);

        var calendarEvent = new CalendarEvent(start, text);

        Assert.Equal(start, calendarEvent.Start);
        Assert.Equal(text, calendarEvent.Text);
    }
}
