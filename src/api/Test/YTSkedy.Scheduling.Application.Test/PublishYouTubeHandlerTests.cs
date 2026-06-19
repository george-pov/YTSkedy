using Microsoft.Extensions.Logging.Abstractions;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.YouTube;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Test;

public class PublishYouTubeHandlerTests
{
    private const string CalendarEventId = "20260615T170000Z";
    private static readonly DateTimeOffset NowUtc =
        new(2026, 06, 12, 00, 00, 00, TimeSpan.Zero);
    private static readonly DateTimeOffset FutureStartUtc =
        new(2026, 06, 15, 17, 00, 00, TimeSpan.Zero);

    [Fact]
    public async Task Publish_MissingEvent_ReturnsNotFoundWithoutPublishing()
    {
        var publisher = new FakeYouTubePublisher("broadcast-123");
        var repository = new FakeCalendarEventRepository();
        var handler = CreateHandler(detail: null, publisher, repository);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(PublishYouTubeStatus.NotFound, result.Status);
        Assert.Equal(0, publisher.CallCount);
        Assert.Equal(0, repository.ReserveCallCount);
        Assert.Equal(0, repository.MarkPublishedCallCount);
    }

    [Fact]
    public async Task Publish_AlreadyPublished_ReturnsAlreadyPublishedWithoutPublishing()
    {
        var publisher = new FakeYouTubePublisher("broadcast-123");
        var repository = new FakeCalendarEventRepository();
        var detail = CreateDetail(
            FutureStartUtc,
            CalendarEventStatus.Published,
            [new LocalizedDescription("en", "English title", "English description")]);
        var handler = CreateHandler(detail, publisher, repository);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(PublishYouTubeStatus.AlreadyPublished, result.Status);
        Assert.Equal(0, publisher.CallCount);
        Assert.Equal(0, repository.ReserveCallCount);
        Assert.Equal(0, repository.MarkPublishedCallCount);
    }

    [Fact]
    public async Task Publish_StartInPast_ReturnsStartInPastWithoutPublishing()
    {
        var publisher = new FakeYouTubePublisher("broadcast-123");
        var repository = new FakeCalendarEventRepository();
        var pastStartUtc = NowUtc.AddHours(-1);
        var detail = CreateDetail(
            pastStartUtc,
            CalendarEventStatus.Draft,
            [new LocalizedDescription("en", "English title", "English description")]);
        var handler = CreateHandler(detail, publisher, repository);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(PublishYouTubeStatus.StartInPast, result.Status);
        Assert.Equal(0, publisher.CallCount);
        Assert.Equal(0, repository.ReserveCallCount);
        Assert.Equal(0, repository.MarkPublishedCallCount);
    }

    [Fact]
    public async Task Publish_NoEnglishDescription_ReturnsMissingEnglishWithoutPublishing()
    {
        var publisher = new FakeYouTubePublisher("broadcast-123");
        var repository = new FakeCalendarEventRepository();
        var detail = CreateDetail(
            FutureStartUtc,
            CalendarEventStatus.Draft,
            [new LocalizedDescription("ru", "Russian title", "Russian description")]);
        var handler = CreateHandler(detail, publisher, repository);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(PublishYouTubeStatus.MissingEnglishDescription, result.Status);
        Assert.Equal(0, publisher.CallCount);
        Assert.Equal(0, repository.ReserveCallCount);
        Assert.Equal(0, repository.MarkPublishedCallCount);
    }

    [Fact]
    public async Task Publish_FutureDraftWithEnglish_PublishesAndMarksPublished()
    {
        var publisher = new FakeYouTubePublisher("broadcast-123");
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

        Assert.Equal(PublishYouTubeStatus.Published, result.Status);
        Assert.Equal("broadcast-123", result.YouTubeBroadcastId);

        Assert.Equal(1, publisher.CallCount);
        Assert.NotNull(publisher.Request);
        Assert.Equal("English title", publisher.Request!.Title);
        Assert.Equal("English description", publisher.Request.Description);
        Assert.Equal(FutureStartUtc, publisher.Request.ScheduledStartUtc);

        Assert.Equal(1, repository.ReserveCallCount);
        Assert.Equal(1, repository.MarkPublishedCallCount);
        Assert.Equal(0, repository.ReleaseCallCount);
        Assert.Equal(CalendarEventId, repository.MarkedCalendarEventId);
        Assert.Equal("broadcast-123", repository.MarkedBroadcastId);
    }

    [Fact]
    public async Task Publish_ReservationLost_ReturnsAlreadyPublishedWithoutPublishing()
    {
        var publisher = new FakeYouTubePublisher("broadcast-123");
        var repository = new FakeCalendarEventRepository { ReserveResult = false };
        var detail = CreateDetail(
            FutureStartUtc,
            CalendarEventStatus.Draft,
            [new LocalizedDescription("en", "English title", "English description")]);
        var handler = CreateHandler(detail, publisher, repository);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(PublishYouTubeStatus.AlreadyPublished, result.Status);
        Assert.Equal(1, repository.ReserveCallCount);
        Assert.Equal(0, publisher.CallCount);
        Assert.Equal(0, repository.MarkPublishedCallCount);
        Assert.Equal(0, repository.ReleaseCallCount);
    }

    [Fact]
    public async Task Publish_BroadcastFailsAfterReservation_ReleasesReservationAndThrows()
    {
        var publisher = new FakeYouTubePublisher(
            "broadcast-123",
            new InvalidOperationException("YouTube insert failed"));
        var repository = new FakeCalendarEventRepository();
        var detail = CreateDetail(
            FutureStartUtc,
            CalendarEventStatus.Draft,
            [new LocalizedDescription("en", "English title", "English description")]);
        var handler = CreateHandler(detail, publisher, repository);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(CalendarEventId, CancellationToken.None));

        Assert.Equal(1, repository.ReserveCallCount);
        Assert.Equal(1, publisher.CallCount);
        Assert.Equal(1, repository.ReleaseCallCount);
        Assert.Equal(0, repository.MarkPublishedCallCount);
    }

    [Fact]
    public async Task Publish_FinalizeFailsThenSucceeds_PublishesWithoutCompensating()
    {
        var publisher = new FakeYouTubePublisher("broadcast-123");
        var repository = new FakeCalendarEventRepository
        {
            MarkPublishedFault = new InvalidOperationException("transient storage fault"),
            MarkPublishedFaultCount = 1
        };
        var deleter = new FakeYouTubeDeleter();
        var detail = CreateDetail(
            FutureStartUtc,
            CalendarEventStatus.Draft,
            [new LocalizedDescription("en", "English title", "English description")]);
        var handler = CreateHandler(detail, publisher, repository, deleter);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(PublishYouTubeStatus.Published, result.Status);
        Assert.Equal("broadcast-123", result.YouTubeBroadcastId);
        Assert.Equal(2, repository.MarkPublishedCallCount);
        Assert.Equal(0, deleter.CallCount);
        Assert.Equal(0, repository.ReleaseCallCount);
    }

    [Fact]
    public async Task Publish_FinalizeFailsAndStillPublishing_DeletesBroadcastReleasesAndThrows()
    {
        var publisher = new FakeYouTubePublisher("broadcast-123");
        var markFault = new InvalidOperationException("storage unavailable");
        var repository = new FakeCalendarEventRepository
        {
            MarkPublishedFault = markFault,
            MarkPublishedFaultCount = int.MaxValue
        };
        var deleter = new FakeYouTubeDeleter();
        var detail = CreateDetail(
            FutureStartUtc,
            CalendarEventStatus.Draft,
            [new LocalizedDescription("en", "English title", "English description")]);
        var reread = CreateDetail(
            FutureStartUtc,
            CalendarEventStatus.Publishing,
            [new LocalizedDescription("en", "English title", "English description")]);
        var handler = CreateHandler(detail, publisher, repository, deleter, reread);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(CalendarEventId, CancellationToken.None));

        Assert.Same(markFault, exception);
        Assert.Equal(3, repository.MarkPublishedCallCount);
        Assert.Equal(1, deleter.CallCount);
        Assert.Equal("broadcast-123", deleter.DeletedBroadcastId);
        Assert.Equal(1, repository.ReleaseCallCount);
    }

    [Fact]
    public async Task Publish_FinalizeFailsButRowAlreadyPublished_ReturnsPublishedWithoutCompensating()
    {
        var publisher = new FakeYouTubePublisher("broadcast-123");
        var repository = new FakeCalendarEventRepository
        {
            MarkPublishedFault = new InvalidOperationException("lost acknowledgement"),
            MarkPublishedFaultCount = int.MaxValue
        };
        var deleter = new FakeYouTubeDeleter();
        var detail = CreateDetail(
            FutureStartUtc,
            CalendarEventStatus.Draft,
            [new LocalizedDescription("en", "English title", "English description")]);
        var reread = CreateDetail(
            FutureStartUtc,
            CalendarEventStatus.Published,
            [new LocalizedDescription("en", "English title", "English description")]);
        var handler = CreateHandler(detail, publisher, repository, deleter, reread);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(PublishYouTubeStatus.Published, result.Status);
        Assert.Equal("broadcast-123", result.YouTubeBroadcastId);
        Assert.Equal(0, deleter.CallCount);
        Assert.Equal(0, repository.ReleaseCallCount);
    }

    [Fact]
    public async Task Publish_FinalizeFailsAndBroadcastDeleteFails_KeepsReservationAndThrows()
    {
        var publisher = new FakeYouTubePublisher("broadcast-123");
        var markFault = new InvalidOperationException("storage unavailable");
        var repository = new FakeCalendarEventRepository
        {
            MarkPublishedFault = markFault,
            MarkPublishedFaultCount = int.MaxValue
        };
        var deleter = new FakeYouTubeDeleter(
            new YouTubeDeleteException("YouTube delete failed"));
        var detail = CreateDetail(
            FutureStartUtc,
            CalendarEventStatus.Draft,
            [new LocalizedDescription("en", "English title", "English description")]);
        var reread = CreateDetail(
            FutureStartUtc,
            CalendarEventStatus.Publishing,
            [new LocalizedDescription("en", "English title", "English description")]);
        var handler = CreateHandler(detail, publisher, repository, deleter, reread);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(CalendarEventId, CancellationToken.None));

        Assert.Same(markFault, exception);
        Assert.Equal(1, deleter.CallCount);
        Assert.Equal(0, repository.ReleaseCallCount);
    }

    private static PublishYouTubeHandler CreateHandler(
        CalendarEventView? detail,
        FakeYouTubePublisher publisher,
        FakeCalendarEventRepository repository,
        FakeYouTubeDeleter? deleter = null,
        CalendarEventView? rereadDetail = null) =>
        new(
            new FakeCalendarEventReader(detail, rereadDetail),
            repository,
            publisher,
            deleter ?? new FakeYouTubeDeleter(),
            new FixedTimeProvider(NowUtc),
            NullLogger<PublishYouTubeHandler>.Instance);

    private static CalendarEventView CreateDetail(
        DateTimeOffset scheduledStartUtc,
        CalendarEventStatus status,
        IReadOnlyList<LocalizedDescription> descriptions) =>
        new(
            CalendarEventId,
            new ScheduledStart(scheduledStartUtc.UtcDateTime, "UTC"),
            scheduledStartUtc,
            descriptions,
            status);

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class FakeCalendarEventReader(
        CalendarEventView? detail,
        CalendarEventView? rereadDetail = null) : ICalendarEventReader
    {
        private bool _firstReadDone;

        public Task<IReadOnlyList<CalendarEventView>> ListAsync(
            CalendarEventMonthCriteria? criteria,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<CalendarEventView?> GetByIdAsync(
            string calendarEventId,
            CancellationToken cancellationToken)
        {
            // The first read drives the handler guards. Later reads model the
            // state the compensation path observes after a failed finalize.
            if (!_firstReadDone)
            {
                _firstReadDone = true;

                return Task.FromResult(detail);
            }

            return Task.FromResult(rereadDetail ?? detail);
        }
    }

    private sealed class FakeCalendarEventRepository : ICalendarEventRepository
    {
        public bool ReserveResult { get; init; } = true;

        public int ReserveCallCount { get; private set; }

        public int MarkPublishedCallCount { get; private set; }

        public int ReleaseCallCount { get; private set; }

        public string? MarkedCalendarEventId { get; private set; }

        public string? MarkedBroadcastId { get; private set; }

        public Exception? MarkPublishedFault { get; init; }

        public int MarkPublishedFaultCount { get; init; }

        public Task<string> CreateAsync(
            CalendarEvent calendarEvent,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> UpdateDescriptionsAsync(
            string calendarEventId,
            IReadOnlyList<LocalizedDescription> descriptions,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> TryReserveForPublishingAsync(
            string calendarEventId,
            CancellationToken cancellationToken)
        {
            ReserveCallCount++;

            return Task.FromResult(ReserveResult);
        }

        public Task MarkPublishedAsync(
            string calendarEventId,
            string youTubeBroadcastId,
            CancellationToken cancellationToken)
        {
            MarkPublishedCallCount++;

            if (MarkPublishedFault is not null &&
                MarkPublishedCallCount <= MarkPublishedFaultCount)
            {
                return Task.FromException(MarkPublishedFault);
            }

            MarkedCalendarEventId = calendarEventId;
            MarkedBroadcastId = youTubeBroadcastId;

            return Task.CompletedTask;
        }

        public Task ReleaseReservationAsync(
            string calendarEventId,
            CancellationToken cancellationToken)
        {
            ReleaseCallCount++;

            return Task.CompletedTask;
        }

        public Task<DeleteDraftCalendarEventResult> DeleteDraftAsync(
            string calendarEventId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<DeleteCalendarEventRowResult> DeleteAsync(
            string calendarEventId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeYouTubePublisher(string broadcastId, Exception? failure = null)
        : IYouTubePublisher
    {
        public int CallCount { get; private set; }

        public YouTubeRequest? Request { get; private set; }

        public Task<string> PublishAsync(
            YouTubeRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            Request = request;

            return failure is null
                ? Task.FromResult(broadcastId)
                : Task.FromException<string>(failure);
        }
    }

    private sealed class FakeYouTubeDeleter(Exception? failure = null) : IYouTubeDeleter
    {
        public int CallCount { get; private set; }

        public string? DeletedBroadcastId { get; private set; }

        public Task<YouTubeDeleteResult> DeleteAsync(
            string youTubeBroadcastId,
            CancellationToken cancellationToken)
        {
            CallCount++;
            DeletedBroadcastId = youTubeBroadcastId;

            return failure is null
                ? Task.FromResult(YouTubeDeleteResult.Deleted)
                : Task.FromException<YouTubeDeleteResult>(failure);
        }
    }
}
