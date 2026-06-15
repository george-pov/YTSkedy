using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Test;

public class UpdateCalendarEventHandlerTests
{
    private const string CalendarEventId = "20260606T170000Z";

    private static readonly LocalizedDescription[] Descriptions =
    [
        new("en", "English title", "English description"),
        new("ru", "Russian title", "Russian description")
    ];

    [Fact]
    public async Task HandleAsync_ExistingEvent_UpdatesDescriptionsAndReturnsTrue()
    {
        var repository = new FakeCalendarEventRepository(updateResult: true);
        var handler = new UpdateCalendarEventHandler(repository);

        var result = await handler.HandleAsync(
            new UpdateCalendarEventDescriptionsCommand(CalendarEventId, Descriptions),
            CancellationToken.None);

        Assert.True(result);
        Assert.Equal(CalendarEventId, repository.UpdatedCalendarEventId);
        Assert.Same(Descriptions, repository.UpdatedDescriptions);
    }

    [Fact]
    public async Task HandleAsync_MissingEvent_ReturnsFalse()
    {
        var repository = new FakeCalendarEventRepository(updateResult: false);
        var handler = new UpdateCalendarEventHandler(repository);

        var result = await handler.HandleAsync(
            new UpdateCalendarEventDescriptionsCommand(CalendarEventId, Descriptions),
            CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task HandleAsync_NullCommand_Throws()
    {
        var handler = new UpdateCalendarEventHandler(
            new FakeCalendarEventRepository(updateResult: true));

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!, CancellationToken.None));
    }

    private sealed class FakeCalendarEventRepository(bool updateResult) : ICalendarEventRepository
    {
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
    }
}
