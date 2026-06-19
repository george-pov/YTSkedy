using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Test;

public class GetCalendarEventHandlerTests
{
    private const string CalendarEventId = "20260606T170000Z";

    [Fact]
    public async Task HandleAsync_ExistingEvent_ReturnsTheReadModel()
    {
        var item = CreateView(CalendarEventId);
        var reader = new FakeCalendarEventReader(item);
        var handler = new GetCalendarEventHandler(reader);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Same(item, result);
        Assert.Equal(CalendarEventId, reader.RequestedId);
    }

    [Fact]
    public async Task HandleAsync_MissingEvent_ReturnsNull()
    {
        var reader = new FakeCalendarEventReader(null);
        var handler = new GetCalendarEventHandler(reader);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Null(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_BlankId_Throws(string calendarEventId)
    {
        var handler = new GetCalendarEventHandler(new FakeCalendarEventReader(null));

        await Assert.ThrowsAsync<ArgumentException>(
            () => handler.HandleAsync(calendarEventId, CancellationToken.None));
    }

    private static CalendarEventView CreateView(string calendarEventId) =>
        new(
            calendarEventId,
            new ScheduledStart(new DateTime(2026, 6, 6, 10, 0, 0), "America/Vancouver"),
            new DateTimeOffset(2026, 6, 6, 17, 0, 0, TimeSpan.Zero),
            [new LocalizedDescription("en", "English stream 1", null)],
            CalendarEventStatus.Draft);

    private sealed class FakeCalendarEventReader(CalendarEventView? item)
        : ICalendarEventReader
    {
        public string? RequestedId { get; private set; }

        public Task<IReadOnlyList<CalendarEventView>> ListAsync(
            CalendarEventMonthCriteria? criteria,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<CalendarEventView?> GetByIdAsync(
            string calendarEventId,
            CancellationToken cancellationToken)
        {
            RequestedId = calendarEventId;

            return Task.FromResult(item);
        }
    }
}
