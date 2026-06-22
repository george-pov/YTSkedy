using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Test;

public class DeleteCalendarEventHandlerTests
{
    private const string CalendarEventId = "20260615T170000Z";
    private static readonly DateTimeOffset StartUtc =
        new(2026, 06, 15, 17, 00, 00, TimeSpan.Zero);

    [Fact]
    public async Task Delete_MissingEvent_ReturnsNotFoundWithoutDeleting()
    {
        var repository = new FakeCalendarEventRepository();
        var handler = CreateHandler(detail: null, repository);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(DeleteCalendarEventResult.NotFound, result);
        Assert.Equal(0, repository.DeleteCallCount);
    }

    [Fact]
    public async Task Delete_ExistingEvent_DeletesRowAndReturnsDeleted()
    {
        var repository = new FakeCalendarEventRepository();
        var handler = CreateHandler(CreateDetail(), repository);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(DeleteCalendarEventResult.Deleted, result);
        Assert.Equal(1, repository.DeleteCallCount);
        Assert.Equal(CalendarEventId, repository.DeletedCalendarEventId);
    }

    [Fact]
    public async Task Delete_BlankId_Throws()
    {
        var handler = CreateHandler(CreateDetail(), new FakeCalendarEventRepository());

        await Assert.ThrowsAsync<ArgumentException>(
            () => handler.HandleAsync("   ", CancellationToken.None));
    }

    private static DeleteCalendarEventHandler CreateHandler(
        CalendarEventView? detail,
        FakeCalendarEventRepository repository) =>
        new(new FakeCalendarEventReader(detail), repository);

    private static CalendarEventView CreateDetail() =>
        new(
            CalendarEventId,
            new ScheduledStart(StartUtc.UtcDateTime, "UTC"),
            StartUtc,
            [new LocalizedDescription("en", "English title", "English description")]);

    private sealed class FakeCalendarEventReader(CalendarEventView? detail) : ICalendarEventReader
    {
        public Task<IReadOnlyList<CalendarEventView>> ListAsync(
            CalendarEventMonthCriteria? criteria,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<CalendarEventView?> GetByIdAsync(
            string calendarEventId,
            CancellationToken cancellationToken) =>
            Task.FromResult(detail);
    }

    private sealed class FakeCalendarEventRepository : ICalendarEventRepository
    {
        public int DeleteCallCount { get; private set; }

        public string? DeletedCalendarEventId { get; private set; }

        public Task<string> CreateAsync(
            CalendarEvent calendarEvent,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> UpdateDescriptionsAsync(
            string calendarEventId,
            IReadOnlyList<LocalizedDescription> descriptions,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task DeleteAsync(
            string calendarEventId,
            CancellationToken cancellationToken)
        {
            DeleteCallCount++;
            DeletedCalendarEventId = calendarEventId;

            return Task.CompletedTask;
        }
    }
}