using YTSkedy.Scheduling.Application.CalendarEvents.Starts;
using YTSkedy.Scheduling.Domain.CalendarEvents;
using YTSkedy.TestSupport;

namespace YTSkedy.Scheduling.Application.Test.CalendarEvents.Starts;

public sealed class GetDefaultStartHandlerTests
{
    [Fact]
    public async Task HandleAsync_LoadsAllEventsAndProjectsOccupiedStarts()
    {
        var cancellationToken = new CancellationTokenSource().Token;
        var occupied = DateTimeOffset.Parse("2026-07-12T17:00:00+00:00");
        var events = new FakeCalendarEventReader(
            [ApplicationTestData.CalendarEvent(scheduledStartUtc: occupied)]);
        var defaults = new FakeStartDefaultsStore(
            new StartDefaults(DayOfWeek.Sunday, new TimeOnly(10, 0), "America/Vancouver"));
        var handler = new GetDefaultStartHandler(
            defaults,
            events,
            new FixedTimeProvider(DateTimeOffset.Parse("2026-07-12T16:00:00+00:00")));

        var result = await handler.HandleAsync(null, cancellationToken);

        Assert.Equal(new DateOnly(2026, 7, 19), result.LocalDate);
        Assert.True(events.ListCalled);
        Assert.Null(events.Criteria);
        Assert.Equal(cancellationToken, events.CancellationToken);
        Assert.Equal(cancellationToken, defaults.CancellationToken);
    }
}
