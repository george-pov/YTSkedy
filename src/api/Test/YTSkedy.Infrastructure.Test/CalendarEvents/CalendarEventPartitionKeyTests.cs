using YTSkedy.Infrastructure.CalendarEvents;
using YTSkedy.Scheduling.Application.CalendarEvents;

namespace YTSkedy.Infrastructure.Test.CalendarEvents;

public class CalendarEventPartitionKeyTests
{
    [Fact]
    public void ForInstant_ScheduledStart_ReturnsUtcMonthKey()
    {
        var scheduledStartUtc = new DateTimeOffset(2026, 06, 15, 17, 00, 00, TimeSpan.Zero);

        var result = CalendarEventPartitionKey.ForInstant(scheduledStartUtc);

        Assert.Equal("calendar-events-202606", result);
    }

    [Fact]
    public void ForLocalMonth_June2026_ReturnsAdjacentMonthKeys()
    {
        var criteria = new CalendarEventMonthCriteria(2026, 6);

        var result = CalendarEventPartitionKey.ForLocalMonth(criteria);

        Assert.Equal(
            [
                "calendar-events-202605",
                "calendar-events-202606",
                "calendar-events-202607"
            ],
            result);
    }

    [Fact]
    public void ForLocalMonth_December9999_ReturnsRepresentableMonthKeys()
    {
        var criteria = new CalendarEventMonthCriteria(9999, 12);

        var result = CalendarEventPartitionKey.ForLocalMonth(criteria);

        Assert.Equal(
            [
                "calendar-events-999911",
                "calendar-events-999912"
            ],
            result);
    }
}
