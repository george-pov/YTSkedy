using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Test;

public class CreateCalendarEventHandlerTests
{
    [Fact]
    public async Task CreateCalendarEvent_ValidCommand_CreatesCalendarEventAndReturnsCalendarEventId()
    {
        var modifier = new FakeCalendarEventModifier("1001");
        var handler = new CreateCalendarEventHandler(modifier);
        var start = new ScheduledStart(
            new DateTime(2026, 06, 05, 10, 00, 00),
            "America/Vancouver");
        var descriptions = new[]
        {
            new LocalizedDescription("ru", "Russian stream 1", null),
            new LocalizedDescription("en", "English stream 1", "Description for stream 1 in English")
        };
        var command = new CreateCalendarEventCommand(start, descriptions);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal("1001", result.CalendarEventId);
        var createdCalendarEvent = modifier.CreatedCalendarEvent;
        Assert.NotNull(createdCalendarEvent);
        Assert.Equal(start, createdCalendarEvent!.Start);
        Assert.Equal(descriptions, createdCalendarEvent.Descriptions);
    }

    private sealed class FakeCalendarEventModifier(string calendarEventId) : ICalendarEventModifier
    {
        public CalendarEvent? CreatedCalendarEvent { get; private set; }

        public Task<string> CreateAsync(
            CalendarEvent calendarEvent,
            CancellationToken cancellationToken)
        {
            CreatedCalendarEvent = calendarEvent;

            return Task.FromResult(calendarEventId);
        }

        public Task<bool> UpdateDescriptionsAsync(
            string calendarEventId,
            IReadOnlyList<LocalizedDescription> descriptions,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task DeleteAsync(
            string calendarEventId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
