using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.CalendarEvents;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Test;

public class DeleteCalendarEventHandlerTests
{
    private const string CalendarEventId = "20260615T170000Z";
    private static readonly DateTimeOffset StartUtc =
        new(2026, 06, 15, 17, 00, 00, TimeSpan.Zero);

    [Fact]
    public async Task Delete_MissingEvent_ReturnsNotFoundWithoutDeleting()
    {
        var modifier = new FakeCalendarEventModifier();
        var handler = CreateHandler(detail: null, modifier);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(DeleteCalendarEventResult.NotFound, result);
        Assert.Equal(0, modifier.DeleteCallCount);
    }

    [Fact]
    public async Task Delete_ExistingEvent_DeletesRowAndReturnsDeleted()
    {
        var modifier = new FakeCalendarEventModifier();
        var handler = CreateHandler(CreateDetail(), modifier);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(DeleteCalendarEventResult.Deleted, result);
        Assert.Equal(1, modifier.DeleteCallCount);
        Assert.Equal(CalendarEventId, modifier.DeletedCalendarEventId);
    }

    [Fact]
    public async Task Delete_EventWithPlatformPublications_ReturnsConflictWithoutDeleting()
    {
        var modifier = new FakeCalendarEventModifier();
        var handler = CreateHandler(CreateDetail(), modifier, [Publication()]);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(DeleteCalendarEventResult.HasPlatformPublications, result);
        Assert.Equal(0, modifier.DeleteCallCount);
    }

    [Fact]
    public async Task Delete_BlankId_Throws()
    {
        var handler = CreateHandler(CreateDetail(), new FakeCalendarEventModifier());

        await Assert.ThrowsAsync<ArgumentException>(
            () => handler.HandleAsync("   ", CancellationToken.None));
    }

    private static DeleteCalendarEventHandler CreateHandler(
        CalendarEventView? detail,
        FakeCalendarEventModifier modifier,
        IReadOnlyList<PlatformPublication>? publications = null) =>
        new(
            new FakeCalendarEventReader(detail),
            new FakePlatformPublicationReader(publications ?? []),
            modifier);

    private static CalendarEventView CreateDetail() =>
        new(
            CalendarEventId,
            new ScheduledStart(StartUtc.UtcDateTime, "UTC"),
            StartUtc,
            [new LocalizedDescription("en", "English title", "English description")]);

    private static PlatformPublication Publication() =>
        new(
            CalendarEventId,
            "platform-1",
            "Main YouTube channel",
            PlatformType.YouTube,
            PublishStatus.Published,
            "external-1",
            StartUtc,
            null,
            StartUtc);

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

    private sealed class FakePlatformPublicationReader(
        IReadOnlyList<PlatformPublication> publications) : IPlatformPublicationReader
    {
        public Task<IReadOnlyList<PlatformPublication>> ListByEventAsync(
            string calendarEventId,
            CancellationToken cancellationToken) =>
            Task.FromResult(publications);

        public Task<PlatformPublication?> GetAsync(
            string calendarEventId,
            string platformId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<PlatformPublication>> ListPublishingByPlatformAsync(
            string platformId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeCalendarEventModifier : ICalendarEventModifier
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
