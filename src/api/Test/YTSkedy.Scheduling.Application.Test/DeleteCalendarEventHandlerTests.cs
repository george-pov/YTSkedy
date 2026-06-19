using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.YouTube;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Test;

public class DeleteCalendarEventHandlerTests
{
    private const string CalendarEventId = "20260615T170000Z";
    private const string BroadcastId = "broadcast-123";
    private static readonly DateTimeOffset NowUtc =
        new(2026, 06, 12, 00, 00, 00, TimeSpan.Zero);
    private static readonly DateTimeOffset FutureStartUtc =
        new(2026, 06, 15, 17, 00, 00, TimeSpan.Zero);
    private static readonly DateTimeOffset PastStartUtc =
        new(2026, 06, 10, 17, 00, 00, TimeSpan.Zero);

    [Fact]
    public async Task Delete_MissingEvent_ReturnsNotFoundWithoutSideEffects()
    {
        var repository = new FakeCalendarEventRepository();
        var deleter = new FakeYouTubeDeleter();
        var handler = CreateHandler(detail: null, repository, deleter);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(DeleteCalendarEventResult.NotFound, result);
        Assert.Equal(0, deleter.CallCount);
        Assert.Equal(0, repository.DeleteDraftCallCount);
        Assert.Equal(0, repository.DeleteCallCount);
    }

    [Fact]
    public async Task Delete_MalformedId_ReturnsNotFoundWithoutSideEffects()
    {
        var repository = new FakeCalendarEventRepository();
        var deleter = new FakeYouTubeDeleter();

        // The reader returns null for an unparseable id, exactly as it does for a
        // missing one, so the handler reports NotFound without any id-format check.
        var handler = CreateHandler(detail: null, repository, deleter);

        var result = await handler.HandleAsync("not-a-valid-id", CancellationToken.None);

        Assert.Equal(DeleteCalendarEventResult.NotFound, result);
        Assert.Equal(0, deleter.CallCount);
        Assert.Equal(0, repository.DeleteDraftCallCount);
        Assert.Equal(0, repository.DeleteCallCount);
    }

    [Fact]
    public async Task Delete_FutureDraft_DeletesViaDraftPathWithoutYouTube()
    {
        var repository = new FakeCalendarEventRepository
        {
            DeleteDraftResult = DeleteDraftCalendarEventResult.Deleted
        };
        var deleter = new FakeYouTubeDeleter();
        var detail = CreateDetail(CalendarEventStatus.Draft, FutureStartUtc);
        var handler = CreateHandler(detail, repository, deleter);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(DeleteCalendarEventResult.Deleted, result);
        Assert.Equal(1, repository.DeleteDraftCallCount);
        Assert.Equal(CalendarEventId, repository.DeletedDraftCalendarEventId);
        Assert.Equal(0, repository.DeleteCallCount);
        Assert.Equal(0, deleter.CallCount);
    }

    [Fact]
    public async Task Delete_PastDraft_DeletesViaDraftPathWithoutYouTube()
    {
        // Past Draft rows have no YouTube broadcast attached and stay deletable
        // as stale local cleanup.
        var repository = new FakeCalendarEventRepository
        {
            DeleteDraftResult = DeleteDraftCalendarEventResult.Deleted
        };
        var deleter = new FakeYouTubeDeleter();
        var detail = CreateDetail(CalendarEventStatus.Draft, PastStartUtc);
        var handler = CreateHandler(detail, repository, deleter);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(DeleteCalendarEventResult.Deleted, result);
        Assert.Equal(1, repository.DeleteDraftCallCount);
        Assert.Equal(0, repository.DeleteCallCount);
        Assert.Equal(0, deleter.CallCount);
    }

    [Fact]
    public async Task Delete_DraftRepositoryNotDeletable_ReturnsNotDeletable()
    {
        var repository = new FakeCalendarEventRepository
        {
            DeleteDraftResult = DeleteDraftCalendarEventResult.NotDeletable
        };
        var deleter = new FakeYouTubeDeleter();
        var detail = CreateDetail(CalendarEventStatus.Draft, FutureStartUtc);
        var handler = CreateHandler(detail, repository, deleter);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(DeleteCalendarEventResult.NotDeletable, result);
        Assert.Equal(1, repository.DeleteDraftCallCount);
    }

    [Fact]
    public async Task Delete_DraftRepositoryNotFound_ReturnsNotFound()
    {
        var repository = new FakeCalendarEventRepository
        {
            DeleteDraftResult = DeleteDraftCalendarEventResult.NotFound
        };
        var deleter = new FakeYouTubeDeleter();
        var detail = CreateDetail(CalendarEventStatus.Draft, FutureStartUtc);
        var handler = CreateHandler(detail, repository, deleter);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(DeleteCalendarEventResult.NotFound, result);
        Assert.Equal(1, repository.DeleteDraftCallCount);
    }

    [Fact]
    public async Task Delete_Publishing_ReturnsNotDeletableWithoutSideEffects()
    {
        var repository = new FakeCalendarEventRepository();
        var deleter = new FakeYouTubeDeleter();
        var detail = CreateDetail(CalendarEventStatus.Publishing, FutureStartUtc, BroadcastId);
        var handler = CreateHandler(detail, repository, deleter);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(DeleteCalendarEventResult.NotDeletable, result);
        Assert.Equal(0, deleter.CallCount);
        Assert.Equal(0, repository.DeleteCallCount);
        Assert.Equal(0, repository.DeleteDraftCallCount);
    }

    [Fact]
    public async Task Delete_PastPublished_ReturnsNotDeletableWithoutCallingYouTube()
    {
        var repository = new FakeCalendarEventRepository();
        var deleter = new FakeYouTubeDeleter();
        var detail = CreateDetail(CalendarEventStatus.Published, PastStartUtc, BroadcastId);
        var handler = CreateHandler(detail, repository, deleter);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(DeleteCalendarEventResult.NotDeletable, result);
        Assert.Equal(0, deleter.CallCount);
        Assert.Equal(0, repository.DeleteCallCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Delete_FuturePublishedMissingBroadcastId_ReturnsMissingYouTubeBroadcastId(
        string? broadcastId)
    {
        var repository = new FakeCalendarEventRepository();
        var deleter = new FakeYouTubeDeleter();
        var detail = CreateDetail(CalendarEventStatus.Published, FutureStartUtc, broadcastId);
        var handler = CreateHandler(detail, repository, deleter);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(DeleteCalendarEventResult.MissingYouTubeBroadcastId, result);
        Assert.Equal(0, deleter.CallCount);
        Assert.Equal(0, repository.DeleteCallCount);
    }

    [Fact]
    public async Task Delete_FuturePublished_DeletesYouTubeBeforeRowAndReturnsDeleted()
    {
        var operations = new List<string>();
        var repository = new FakeCalendarEventRepository(operations)
        {
            DeleteRowResult = DeleteCalendarEventRowResult.Deleted
        };
        var deleter = new FakeYouTubeDeleter(operations)
        {
            Result = YouTubeDeleteResult.Deleted
        };
        var detail = CreateDetail(CalendarEventStatus.Published, FutureStartUtc, BroadcastId);
        var handler = CreateHandler(detail, repository, deleter);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(DeleteCalendarEventResult.Deleted, result);
        Assert.Equal(1, deleter.CallCount);
        Assert.Equal(BroadcastId, deleter.DeletedBroadcastId);
        Assert.Equal(1, repository.DeleteCallCount);
        Assert.Equal(CalendarEventId, repository.DeletedCalendarEventId);
        Assert.Equal(0, repository.DeleteDraftCallCount);
        Assert.Equal(new[] { "youtube-delete", "row-delete" }, operations);
    }

    [Fact]
    public async Task Delete_FuturePublishedYouTubeNotFound_StillDeletesRowAndReturnsDeleted()
    {
        var repository = new FakeCalendarEventRepository
        {
            DeleteRowResult = DeleteCalendarEventRowResult.Deleted
        };
        var deleter = new FakeYouTubeDeleter
        {
            Result = YouTubeDeleteResult.NotFound
        };
        var detail = CreateDetail(CalendarEventStatus.Published, FutureStartUtc, BroadcastId);
        var handler = CreateHandler(detail, repository, deleter);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(DeleteCalendarEventResult.Deleted, result);
        Assert.Equal(1, deleter.CallCount);
        Assert.Equal(1, repository.DeleteCallCount);
    }

    [Fact]
    public async Task Delete_FuturePublishedYouTubeFails_KeepsRowAndReturnsYouTubeDeleteFailed()
    {
        var repository = new FakeCalendarEventRepository();
        var deleter = new FakeYouTubeDeleter
        {
            Failure = new YouTubeDeleteException("YouTube delete failed")
        };
        var detail = CreateDetail(CalendarEventStatus.Published, FutureStartUtc, BroadcastId);
        var handler = CreateHandler(detail, repository, deleter);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(DeleteCalendarEventResult.YouTubeDeleteFailed, result);
        Assert.Equal(1, deleter.CallCount);
        Assert.Equal(0, repository.DeleteCallCount);
        Assert.Equal(0, repository.DeleteDraftCallCount);
    }

    [Fact]
    public async Task Delete_FuturePublishedLocalRowGoneAfterYouTube_ReturnsDeleted()
    {
        // The local row disappeared after successful YouTube cleanup. Both the
        // external and local resources are gone, so the result is success.
        var repository = new FakeCalendarEventRepository
        {
            DeleteRowResult = DeleteCalendarEventRowResult.NotFound
        };
        var deleter = new FakeYouTubeDeleter
        {
            Result = YouTubeDeleteResult.Deleted
        };
        var detail = CreateDetail(CalendarEventStatus.Published, FutureStartUtc, BroadcastId);
        var handler = CreateHandler(detail, repository, deleter);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(DeleteCalendarEventResult.Deleted, result);
        Assert.Equal(1, repository.DeleteCallCount);
    }

    [Fact]
    public async Task Delete_FuturePublished_DeletesRowByIdWithoutRecheckingStatusOrReReading()
    {
        // Published cleanup uses the id-only delete (not the Draft-guarded path)
        // and takes a single application read: no fresh snapshot before YouTube.
        var repository = new FakeCalendarEventRepository
        {
            DeleteRowResult = DeleteCalendarEventRowResult.Deleted
        };
        var deleter = new FakeYouTubeDeleter
        {
            Result = YouTubeDeleteResult.Deleted
        };
        var detail = CreateDetail(CalendarEventStatus.Published, FutureStartUtc, BroadcastId);
        var reader = new FakeCalendarEventReader(detail);
        var handler = new DeleteCalendarEventHandler(
            reader,
            repository,
            deleter,
            new FixedTimeProvider(NowUtc));

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(DeleteCalendarEventResult.Deleted, result);
        Assert.Equal(1, reader.GetByIdCallCount);
        Assert.Equal(1, repository.DeleteCallCount);
        Assert.Equal(0, repository.DeleteDraftCallCount);
    }

    [Theory]
    [InlineData(CalendarEventStatus.Draft, true, null, true)]
    [InlineData(CalendarEventStatus.Draft, false, null, true)]
    [InlineData(CalendarEventStatus.Published, true, BroadcastId, true)]
    [InlineData(CalendarEventStatus.Published, false, BroadcastId, false)]
    [InlineData(CalendarEventStatus.Published, true, null, false)]
    [InlineData(CalendarEventStatus.Publishing, true, BroadcastId, false)]
    public void CanDelete_MatchesDeleteEligibilityPolicy(
        CalendarEventStatus status,
        bool future,
        string? broadcastId,
        bool expected)
    {
        var item = CreateView(
            status,
            future ? FutureStartUtc : PastStartUtc,
            broadcastId);

        Assert.Equal(expected, item.CanDelete(NowUtc));
    }

    [Theory]
    [InlineData(CalendarEventStatus.Draft, true)]
    [InlineData(CalendarEventStatus.Publishing, false)]
    [InlineData(CalendarEventStatus.Published, false)]
    public void CanUpdate_TrueOnlyForDraft(CalendarEventStatus status, bool expected)
    {
        var item = CreateView(status, FutureStartUtc, BroadcastId);

        Assert.Equal(expected, item.CanUpdate());
    }

    [Fact]
    public void CanPublish_FutureDraftWithEnglishDescription_IsTrue()
    {
        var item = CreateView(CalendarEventStatus.Draft, FutureStartUtc);

        Assert.True(item.CanPublish(NowUtc));
    }

    [Fact]
    public void CanPublish_PastDraft_IsFalse()
    {
        var item = CreateView(CalendarEventStatus.Draft, PastStartUtc);

        Assert.False(item.CanPublish(NowUtc));
    }

    [Fact]
    public void CanPublish_DraftWithoutEnglishDescription_IsFalse()
    {
        var item = CreateView(
            CalendarEventStatus.Draft,
            FutureStartUtc,
            descriptions: [new LocalizedDescription("ru", "Russian title", "Russian description")]);

        Assert.False(item.CanPublish(NowUtc));
    }

    [Theory]
    [InlineData(CalendarEventStatus.Publishing)]
    [InlineData(CalendarEventStatus.Published)]
    public void CanPublish_NonDraft_IsFalse(CalendarEventStatus status)
    {
        var item = CreateView(status, FutureStartUtc, BroadcastId);

        Assert.False(item.CanPublish(NowUtc));
    }

    private static DeleteCalendarEventHandler CreateHandler(
        CalendarEventView? detail,
        FakeCalendarEventRepository repository,
        FakeYouTubeDeleter deleter) =>
        new(
            new FakeCalendarEventReader(detail),
            repository,
            deleter,
            new FixedTimeProvider(NowUtc));

    private static CalendarEventView CreateDetail(
        CalendarEventStatus status,
        DateTimeOffset scheduledStartUtc,
        string? youTubeBroadcastId = null) =>
        new(
            CalendarEventId,
            new ScheduledStart(scheduledStartUtc.UtcDateTime, "UTC"),
            scheduledStartUtc,
            [new LocalizedDescription("en", "English title", "English description")],
            status,
            youTubeBroadcastId);

    private static CalendarEventView CreateView(
        CalendarEventStatus status,
        DateTimeOffset scheduledStartUtc,
        string? youTubeBroadcastId = null,
        IReadOnlyList<LocalizedDescription>? descriptions = null) =>
        new(
            CalendarEventId,
            new ScheduledStart(new DateTime(2026, 06, 15, 10, 00, 00), "America/Vancouver"),
            scheduledStartUtc,
            descriptions ?? [new LocalizedDescription("en", "English title", "English description")],
            status,
            youTubeBroadcastId);

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class FakeCalendarEventReader(CalendarEventView? detail) : ICalendarEventReader
    {
        public int GetByIdCallCount { get; private set; }

        public Task<IReadOnlyList<CalendarEventView>> ListAsync(
            CalendarEventMonthCriteria? criteria,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<CalendarEventView?> GetByIdAsync(
            string calendarEventId,
            CancellationToken cancellationToken)
        {
            GetByIdCallCount++;

            return Task.FromResult(detail);
        }
    }

    private sealed class FakeYouTubeDeleter(List<string>? operations = null)
        : IYouTubeDeleter
    {
        public YouTubeDeleteResult Result { get; init; } =
            YouTubeDeleteResult.Deleted;

        public Exception? Failure { get; init; }

        public int CallCount { get; private set; }

        public string? DeletedBroadcastId { get; private set; }

        public Task<YouTubeDeleteResult> DeleteAsync(
            string youTubeBroadcastId,
            CancellationToken cancellationToken)
        {
            CallCount++;
            DeletedBroadcastId = youTubeBroadcastId;
            operations?.Add("youtube-delete");

            return Failure is null
                ? Task.FromResult(Result)
                : Task.FromException<YouTubeDeleteResult>(Failure);
        }
    }

    private sealed class FakeCalendarEventRepository(List<string>? operations = null)
        : ICalendarEventRepository
    {
        public DeleteDraftCalendarEventResult DeleteDraftResult { get; init; } =
            DeleteDraftCalendarEventResult.Deleted;

        public DeleteCalendarEventRowResult DeleteRowResult { get; init; } =
            DeleteCalendarEventRowResult.Deleted;

        public int DeleteDraftCallCount { get; private set; }

        public int DeleteCallCount { get; private set; }

        public string? DeletedDraftCalendarEventId { get; private set; }

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
            CancellationToken cancellationToken)
        {
            DeleteDraftCallCount++;
            DeletedDraftCalendarEventId = calendarEventId;
            operations?.Add("draft-delete");

            return Task.FromResult(DeleteDraftResult);
        }

        public Task<DeleteCalendarEventRowResult> DeleteAsync(
            string calendarEventId,
            CancellationToken cancellationToken)
        {
            DeleteCallCount++;
            DeletedCalendarEventId = calendarEventId;
            operations?.Add("row-delete");

            return Task.FromResult(DeleteRowResult);
        }
    }
}
