using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.CalendarEvents.Starts;
using YTSkedy.Scheduling.Application.Settings;
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
        var events = new Mock<ICalendarEventReader>();
        events
            .Setup(reader => reader.ListAsync(null, cancellationToken))
            .ReturnsAsync([
                new CalendarEventListRecord(
                    ApplicationTestData.CalendarEvent(scheduledStartUtc: occupied),
                    new HashSet<string>(StringComparer.Ordinal))
            ]);
        var defaults = new Mock<IStartDefaultsReader>();
        defaults
            .Setup(reader => reader.GetAsync(cancellationToken))
            .ReturnsAsync(new StartDefaults(
                DayOfWeek.Sunday,
                new TimeOnly(10, 0),
                "America/Vancouver"));
        var handler = new GetDefaultStartHandler(
            defaults.Object,
            events.Object,
            new FixedTimeProvider(DateTimeOffset.Parse("2026-07-12T16:00:00+00:00")));

        var result = await handler.HandleAsync(null, cancellationToken);

        Assert.Equal(new DateOnly(2026, 7, 19), result.LocalDate);
        events.Verify(reader => reader.ListAsync(null, cancellationToken));
        defaults.Verify(reader => reader.GetAsync(cancellationToken));
    }
}
