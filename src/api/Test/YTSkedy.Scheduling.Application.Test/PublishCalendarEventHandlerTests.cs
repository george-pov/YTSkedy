using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.YouTube;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Test;

public class PublishCalendarEventHandlerTests
{
    private const string CalendarEventId = "20260615T170000Z";
    private static readonly DateTimeOffset NowUtc =
        new(2026, 06, 12, 00, 00, 00, TimeSpan.Zero);
    private static readonly DateTimeOffset FutureStartUtc =
        new(2026, 06, 15, 17, 00, 00, TimeSpan.Zero);

    [Fact]
    public async Task Publish_MissingEvent_ReturnsNotFoundWithoutPublishing()
    {
        var publisher = new FakeBroadcastPublisher("broadcast-123");
        var repository = new FakeCalendarEventRepository();
        var handler = CreateHandler(detail: null, publisher, repository);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(PublishCalendarEventOutcome.NotFound, result.Outcome);
        Assert.Equal(0, publisher.CallCount);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    [Fact]
    public async Task Publish_AlreadyPublished_ReturnsAlreadyPublishedWithoutPublishing()
    {
        var publisher = new FakeBroadcastPublisher("broadcast-123");
        var repository = new FakeCalendarEventRepository();
        var detail = CreateDetail(
            FutureStartUtc,
            CalendarEventStatus.Published,
            [new LocalizedDescription("en", "English title", "English description")]);
        var handler = CreateHandler(detail, publisher, repository);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(PublishCalendarEventOutcome.AlreadyPublished, result.Outcome);
        Assert.Equal(0, publisher.CallCount);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    [Fact]
    public async Task Publish_StartInPast_ReturnsStartInPastWithoutPublishing()
    {
        var publisher = new FakeBroadcastPublisher("broadcast-123");
        var repository = new FakeCalendarEventRepository();
        var pastStartUtc = NowUtc.AddHours(-1);
        var detail = CreateDetail(
            pastStartUtc,
            CalendarEventStatus.Draft,
            [new LocalizedDescription("en", "English title", "English description")]);
        var handler = CreateHandler(detail, publisher, repository);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(PublishCalendarEventOutcome.StartInPast, result.Outcome);
        Assert.Equal(0, publisher.CallCount);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    [Fact]
    public async Task Publish_NoEnglishDescription_ReturnsMissingEnglishWithoutPublishing()
    {
        var publisher = new FakeBroadcastPublisher("broadcast-123");
        var repository = new FakeCalendarEventRepository();
        var detail = CreateDetail(
            FutureStartUtc,
            CalendarEventStatus.Draft,
            [new LocalizedDescription("ru", "Russian title", "Russian description")]);
        var handler = CreateHandler(detail, publisher, repository);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(PublishCalendarEventOutcome.MissingEnglishDescription, result.Outcome);
        Assert.Equal(0, publisher.CallCount);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    [Fact]
    public async Task Publish_FutureDraftWithEnglish_PublishesAndMarksPublished()
    {
        var publisher = new FakeBroadcastPublisher("broadcast-123");
        var repository = new FakeCalendarEventRepository();
        var detail = CreateDetail(
            FutureStartUtc,
            CalendarEventStatus.Draft,
            [
                new LocalizedDescription("ru", "Russian title", "Russian description"),
                new LocalizedDescription("en", "English title", "English description")
            ]);
        var handler = CreateHandler(detail, publisher, repository);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(PublishCalendarEventOutcome.Published, result.Outcome);
        Assert.Equal("broadcast-123", result.YouTubeBroadcastId);

        Assert.Equal(1, publisher.CallCount);
        Assert.NotNull(publisher.Request);
        Assert.Equal("English title", publisher.Request!.Title);
        Assert.Equal("English description", publisher.Request.Description);
        Assert.Equal(FutureStartUtc, publisher.Request.ScheduledStartUtc);

        Assert.Equal(1, repository.UpdateCallCount);
        Assert.Equal(CalendarEventId, repository.UpdatedCalendarEventId);
        Assert.Equal(CalendarEventStatus.Published, repository.UpdatedStatus);
        Assert.Equal("broadcast-123", repository.UpdatedBroadcastId);
    }

    private static PublishCalendarEventHandler CreateHandler(
        CalendarEventDetail? detail,
        FakeBroadcastPublisher publisher,
        FakeCalendarEventRepository repository) =>
        new(
            new FakeCalendarEventReader(detail),
            repository,
            publisher,
            new FixedTimeProvider(NowUtc));

    private static CalendarEventDetail CreateDetail(
        DateTimeOffset scheduledStartUtc,
        CalendarEventStatus status,
        IReadOnlyList<LocalizedDescription> descriptions) =>
        new(CalendarEventId, scheduledStartUtc, descriptions, status);

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class FakeCalendarEventReader(CalendarEventDetail? detail) : ICalendarEventReader
    {
        public Task<IReadOnlyList<CalendarEventListItem>> ListByMonthAsync(
            CalendarEventMonthCriteria criteria,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<CalendarEventDetail?> GetByIdAsync(
            string calendarEventId,
            CancellationToken cancellationToken) =>
            Task.FromResult(detail);
    }

    private sealed class FakeCalendarEventRepository : ICalendarEventRepository
    {
        public int UpdateCallCount { get; private set; }

        public string? UpdatedCalendarEventId { get; private set; }

        public CalendarEventStatus? UpdatedStatus { get; private set; }

        public string? UpdatedBroadcastId { get; private set; }

        public Task<string> CreateAsync(
            CalendarEvent calendarEvent,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task UpdateStatusAsync(
            string calendarEventId,
            CalendarEventStatus status,
            string youTubeBroadcastId,
            CancellationToken cancellationToken)
        {
            UpdateCallCount++;
            UpdatedCalendarEventId = calendarEventId;
            UpdatedStatus = status;
            UpdatedBroadcastId = youTubeBroadcastId;

            return Task.CompletedTask;
        }
    }

    private sealed class FakeBroadcastPublisher(string broadcastId) : IYouTubeBroadcastPublisher
    {
        public int CallCount { get; private set; }

        public YouTubeBroadcastRequest? Request { get; private set; }

        public Task<string> PublishAsync(
            YouTubeBroadcastRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            Request = request;

            return Task.FromResult(broadcastId);
        }
    }
}
