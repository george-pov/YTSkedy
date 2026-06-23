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
    public async Task HandleAsync_ExistingEvent_UpdatesDescriptionsAndReturnsUpdated()
    {
        var modifier = new FakeCalendarEventModifier(updateResult: true);
        var handler = CreateHandler(CreateDetail(), modifier);

        var result = await handler.HandleAsync(
            new UpdateDescriptionsCommand(CalendarEventId, Descriptions),
            CancellationToken.None);

        Assert.Equal(UpdateCalendarEventResult.Updated, result);
        Assert.Equal(1, modifier.UpdateCallCount);
        Assert.Equal(CalendarEventId, modifier.UpdatedCalendarEventId);
        Assert.Same(Descriptions, modifier.UpdatedDescriptions);
    }

    [Fact]
    public async Task HandleAsync_MissingEvent_ReturnsNotFoundWithoutUpdating()
    {
        var modifier = new FakeCalendarEventModifier(updateResult: true);
        var handler = CreateHandler(detail: null, modifier);

        var result = await handler.HandleAsync(
            new UpdateDescriptionsCommand(CalendarEventId, Descriptions),
            CancellationToken.None);

        Assert.Equal(UpdateCalendarEventResult.NotFound, result);
        Assert.Equal(0, modifier.UpdateCallCount);
    }

    [Fact]
    public async Task HandleAsync_RowVanishedBeforeWrite_ReturnsNotFound()
    {
        var modifier = new FakeCalendarEventModifier(updateResult: false);
        var handler = CreateHandler(CreateDetail(), modifier);

        var result = await handler.HandleAsync(
            new UpdateDescriptionsCommand(CalendarEventId, Descriptions),
            CancellationToken.None);

        Assert.Equal(UpdateCalendarEventResult.NotFound, result);
        Assert.Equal(1, modifier.UpdateCallCount);
    }

    [Fact]
    public async Task HandleAsync_NullCommand_Throws()
    {
        var handler = CreateHandler(
            CreateDetail(),
            new FakeCalendarEventModifier(updateResult: true));

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!, CancellationToken.None));
    }

    private static UpdateCalendarEventHandler CreateHandler(
        CalendarEventView? detail,
        FakeCalendarEventModifier modifier) =>
        new(new FakeCalendarEventReader(detail), modifier);

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

    private sealed class FakeCalendarEventModifier(bool updateResult) : ICalendarEventModifier
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

        public Task DeleteAsync(
            string calendarEventId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}