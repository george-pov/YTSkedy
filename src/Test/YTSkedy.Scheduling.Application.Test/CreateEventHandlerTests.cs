using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Test;

public class CreateEventHandlerTests
{
    [Fact]
    public async Task HandleAsync_creates_calendar_event_and_returns_event_id()
    {
        var repository = new StubCalendarEventRepository("1001");
        var handler = new CreateEventHandler(repository);
        var start = new ScheduledStart(
            new DateTime(2026, 06, 05, 10, 00, 00),
            "America/Vancouver");
        var descriptions = new[]
        {
            new LocalizedDescription("ru", "Russian stream 1", null),
            new LocalizedDescription("en", "English stream 1", "Description for stream 1 in English")
        };
        var command = new CreateEventCommand(start, descriptions);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal("1001", result.EventId);
        Assert.NotNull(repository.CreatedEvent);
        Assert.Equal(start, repository.CreatedEvent.Start);
        Assert.Equal(descriptions, repository.CreatedEvent.Descriptions);
    }

    private sealed class StubCalendarEventRepository(string eventId) : ICalendarEventRepository
    {
        public CalendarEvent? CreatedEvent { get; private set; }

        public Task<string> CreateAsync(
            CalendarEvent calendarEvent,
            CancellationToken cancellationToken)
        {
            CreatedEvent = calendarEvent;

            return Task.FromResult(eventId);
        }
    }
}
