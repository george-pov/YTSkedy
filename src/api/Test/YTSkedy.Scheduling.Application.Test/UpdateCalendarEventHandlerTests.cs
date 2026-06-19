using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Test;

public class UpdateCalendarEventHandlerTests
{
    private const string CalendarEventId = "20260606T170000Z";
    private static readonly DateTimeOffset StartUtc =
        new(2026, 06, 06, 17, 00, 00, TimeSpan.Zero);

    private static readonly LocalizedDescription[] Descriptions =
    [
        new("en", "English title", "English description"),
        new("ru", "Russian title", "Russian description")
    ];

    [Fact]
    public async Task HandleAsync_DraftEvent_UpdatesDescriptionsAndReturnsUpdated()
    {
        var repository = new FakeCalendarEventRepository(updateResult: true);
        var handler = CreateHandler(CalendarEventStatus.Draft, repository);

        var result = await handler.HandleAsync(
            new UpdateDescriptionsCommand(CalendarEventId, Descriptions),
            CancellationToken.None);

        Assert.Equal(UpdateCalendarEventResult.Updated, result);
        Assert.Equal(1, repository.UpdateCallCount);
        Assert.Equal(CalendarEventId, repository.UpdatedCalendarEventId);
        Assert.Same(Descriptions, repository.UpdatedDescriptions);
    }

    [Fact]
    public async Task HandleAsync_MissingEvent_ReturnsNotFoundWithoutUpdating()
    {
        var repository = new FakeCalendarEventRepository(updateResult: true);
        var handler = new UpdateCalendarEventHandler(
            new FakeCalendarEventReader(detail: null),
            repository);

        var result = await handler.HandleAsync(
            new UpdateDescriptionsCommand(CalendarEventId, Descriptions),
            CancellationToken.None);

        Assert.Equal(UpdateCalendarEventResult.NotFound, result);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    [Fact]
    public async Task HandleAsync_PublishingEvent_ReturnsNotUpdatableWithoutUpdating()
    {
        var repository = new FakeCalendarEventRepository(updateResult: true);
        var handler = CreateHandler(CalendarEventStatus.Publishing, repository);

        var result = await handler.HandleAsync(
            new UpdateDescriptionsCommand(CalendarEventId, Descriptions),
            CancellationToken.None);

        Assert.Equal(UpdateCalendarEventResult.NotUpdatable, result);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    [Fact]
    public async Task HandleAsync_PublishedEvent_ReturnsNotUpdatableWithoutUpdating()
    {
        var repository = new FakeCalendarEventRepository(updateResult: true);
        var handler = CreateHandler(CalendarEventStatus.Published, repository);

        var result = await handler.HandleAsync(
            new UpdateDescriptionsCommand(CalendarEventId, Descriptions),
            CancellationToken.None);

        Assert.Equal(UpdateCalendarEventResult.NotUpdatable, result);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    [Fact]
    public async Task HandleAsync_DraftVanishedBeforeWrite_ReturnsNotFound()
    {
        // The event read as Draft but the conditional write found no row, so the
        // stale client is reported as a missing event.
        var repository = new FakeCalendarEventRepository(updateResult: false);
        var handler = CreateHandler(CalendarEventStatus.Draft, repository);

        var result = await handler.HandleAsync(
            new UpdateDescriptionsCommand(CalendarEventId, Descriptions),
            CancellationToken.None);

        Assert.Equal(UpdateCalendarEventResult.NotFound, result);
        Assert.Equal(1, repository.UpdateCallCount);
    }

    [Fact]
    public async Task HandleAsync_NullCommand_Throws()
    {
        var handler = CreateHandler(
            CalendarEventStatus.Draft,
            new FakeCalendarEventRepository(updateResult: true));

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!, CancellationToken.None));
    }

    private static UpdateCalendarEventHandler CreateHandler(
        CalendarEventStatus status,
        FakeCalendarEventRepository repository) =>
        new(
            new FakeCalendarEventReader(CreateDetail(status)),
            repository);

    private static CalendarEventView CreateDetail(CalendarEventStatus status) =>
        new(
            CalendarEventId,
            new ScheduledStart(StartUtc.UtcDateTime, "UTC"),
            StartUtc,
            [new LocalizedDescription("en", "English title", "English description")],
            status);

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

    private sealed class FakeCalendarEventRepository(bool updateResult) : ICalendarEventRepository
    {
        public int UpdateCallCount { get; private set; }

        public string? UpdatedCalendarEventId { get; private set; }

        public IReadOnlyList<LocalizedDescription>? UpdatedDescriptions { get; private set; }

        public Task<string> CreateAsync(
            CalendarEvent calendarEvent,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> UpdateDescriptionsAsync(
            string calendarEventId,
            IReadOnlyList<LocalizedDescription> descriptions,
            CancellationToken cancellationToken)
        {
            UpdateCallCount++;
            UpdatedCalendarEventId = calendarEventId;
            UpdatedDescriptions = descriptions;

            return Task.FromResult(updateResult);
        }

        public Task<bool> TryReserveForPublishingAsync(
            string calendarEventId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task MarkPublishedAsync(
            string calendarEventId,
            string youTubeBroadcastId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task ReleaseReservationAsync(
            string calendarEventId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<DeleteDraftCalendarEventResult> DeleteDraftAsync(
            string calendarEventId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<DeleteCalendarEventRowResult> DeleteAsync(
            string calendarEventId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
