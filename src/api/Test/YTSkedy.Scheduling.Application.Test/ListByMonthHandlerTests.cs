using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Test;

public class ListByMonthHandlerTests
{
    [Fact]
    public async Task ListByMonth_ReaderReturnsSortedCalendarEvents_PreservesReaderOrder()
    {
        var calendarEvents = new[]
        {
            CreateListItem("20260605T170000Z", new DateTime(2026, 06, 05, 10, 00, 00)),
            CreateListItem("20260606T170000Z", new DateTime(2026, 06, 06, 10, 00, 00))
        };
        var reader = new FakeCalendarEventReader(calendarEvents);
        var handler = new ListByMonthHandler(reader);
        var criteria = new CalendarEventMonthCriteria(2026, 6);

        var result = await handler.HandleAsync(criteria, CancellationToken.None);

        Assert.Equal(calendarEvents, result);
    }

    [Fact]
    public async Task ListByMonth_ReaderReturnsNoCalendarEvents_ReturnsEmptyList()
    {
        var reader = new FakeCalendarEventReader([]);
        var handler = new ListByMonthHandler(reader);
        var criteria = new CalendarEventMonthCriteria(2026, 6);

        var result = await handler.HandleAsync(criteria, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task HandleAsync_ValidCriteria_ForwardsCriteriaAndCancellationToken()
    {
        var reader = new FakeCalendarEventReader([]);
        var handler = new ListByMonthHandler(reader);
        var criteria = new CalendarEventMonthCriteria(2026, 6);
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        await handler.HandleAsync(criteria, cancellationToken);

        Assert.Equal(criteria, reader.Criteria);
        Assert.Equal(cancellationToken, reader.CancellationToken);
    }

    private static CalendarEventListItem CreateListItem(
        string calendarEventId,
        DateTime localDateTime) =>
        new(
            calendarEventId,
            new ScheduledStart(localDateTime, "America/Vancouver"),
            [
                new LocalizedDescription(
                    "en",
                    $"English stream {calendarEventId}",
                    $"Description for {calendarEventId}")
            ],
            CalendarEventStatus.Draft);

    private sealed class FakeCalendarEventReader(
        IReadOnlyList<CalendarEventListItem> calendarEvents) : ICalendarEventReader
    {
        public CalendarEventMonthCriteria? Criteria { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public Task<IReadOnlyList<CalendarEventListItem>> ListByMonthAsync(
            CalendarEventMonthCriteria criteria,
            CancellationToken cancellationToken)
        {
            Criteria = criteria;
            CancellationToken = cancellationToken;

            return Task.FromResult(calendarEvents);
        }

        public Task<CalendarEventDetail?> GetByIdAsync(
            string calendarEventId,
            CancellationToken cancellationToken) =>
            Task.FromResult<CalendarEventDetail?>(null);
    }
}
