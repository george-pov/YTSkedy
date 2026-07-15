using Microsoft.Extensions.Logging;
using static YTSkedy.Scheduling.Application.Test.PublishHandlerScenario;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Platforms.Providers;
using YTSkedy.Scheduling.Application.Platforms.Publications;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Test;

public class PublishHandlerLifecycleTests
{
    [Fact]
    public async Task HandleAsync_AttemptConflict_ReturnsPublishInProgress()
    {
        var repository = new PublishFakePublicationRepository
        {
            StartResult = StartPublicationResult.Conflict
        };
        var publicationIndex = new FakeCalendarEventPublicationIndexWriter();
        var handler = CreateHandler(
            Event(FutureStart),
            Platform(),
            new PublishFakePublisher(),
            repository: repository,
            publicationIndex: publicationIndex);

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.PublishInProgress, result.Status);
        Assert.Empty(publicationIndex.AddCalls);
    }

    [Fact]
    public async Task HandleAsync_ProviderFailure_MarksFailedWithoutExternalId()
    {
        var repository = new PublishFakePublicationRepository();
        var publisher = new PublishFakePublisher
        {
            Throws = new PlatformPublishException("provider down")
        };
        var publicationIndex = new FakeCalendarEventPublicationIndexWriter();
        var handler = CreateHandler(
            Event(FutureStart),
            Platform(),
            publisher,
            repository: repository,
            publicationIndex: publicationIndex);

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.Failed, result.Status);
        Assert.True(repository.MarkFailedCalled);
        Assert.Null(repository.FailedExternalResourceId);
        Assert.False(repository.ReleaseCalled);
        Assert.False(repository.MarkPublishedCalled);
        Assert.Empty(publicationIndex.AddCalls);
    }

    [Fact]
    public async Task HandleAsync_ProviderFailureWithExternalId_MarksFailedWithExternalId()
    {
        var repository = new PublishFakePublicationRepository();
        var publisher = new PublishFakePublisher
        {
            Throws = new PlatformPublishException(
                "metadata update failed",
                "yt-created-broadcast-id")
        };
        var handler = CreateHandler(
            Event(FutureStart),
            Platform(),
            publisher,
            repository: repository);

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.Failed, result.Status);
        Assert.Equal("yt-created-broadcast-id", repository.FailedExternalResourceId);
        Assert.False(repository.ReleaseCalled);
    }

    [Fact]
    public async Task HandleAsync_ProviderValidationFailure_MarksFailed()
    {
        var repository = new PublishFakePublicationRepository();
        var publisher = new PublishFakePublisher
        {
            Throws = new PlatformPublishValidationException("invalid provider settings")
        };
        var publicationIndex = new FakeCalendarEventPublicationIndexWriter();
        var handler = CreateHandler(
            Event(FutureStart),
            Platform(),
            publisher,
            repository: repository,
            publicationIndex: publicationIndex);

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.Failed, result.Status);
        Assert.True(repository.MarkFailedCalled);
        Assert.False(repository.ReleaseCalled);
        Assert.False(repository.MarkPublishedCalled);
        Assert.Empty(publicationIndex.AddCalls);
    }

    [Fact]
    public async Task HandleAsync_FinalizeReturnsNull_MarksFailedWithProviderId()
    {
        var repository = new PublishFakePublicationRepository { MarkPublishedResult = null };
        var publisher = new PublishFakePublisher
        {
            Result = new PlatformPublishResult("yt-broadcast-id")
        };
        var publicationIndex = new FakeCalendarEventPublicationIndexWriter();
        var handler = CreateHandler(
            Event(FutureStart),
            Platform(),
            publisher,
            repository: repository,
            publicationIndex: publicationIndex);

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.Failed, result.Status);
        Assert.True(repository.MarkFailedCalled);
        Assert.Equal("yt-broadcast-id", repository.FailedExternalResourceId);
        Assert.False(repository.ReleaseCalled);
        Assert.Empty(publicationIndex.AddCalls);
    }

    [Fact]
    public async Task HandleAsync_FinalizeAndMarkFailedCannotWrite_ReturnsFinalizeFailed()
    {
        var repository = new PublishFakePublicationRepository
        {
            MarkPublishedResult = null,
            MarkFailedOutcome = MarkFailedResult.Changed
        };
        var handler = CreateHandler(
            Event(FutureStart),
            Platform(),
            new PublishFakePublisher(),
            repository: repository);

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.FinalizeFailed, result.Status);
        Assert.True(repository.MarkFailedCalled);
    }

    [Fact]
    public async Task HandleAsync_FinalizeThrows_MarksFailedWithProviderId()
    {
        var repository = new PublishFakePublicationRepository
        {
            MarkPublishedThrows = new InvalidOperationException("storage unavailable")
        };
        var handler = CreateHandler(
            Event(FutureStart),
            Platform(),
            new PublishFakePublisher(),
            repository: repository);

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.Failed, result.Status);
        Assert.Equal("yt-broadcast-id", repository.FailedExternalResourceId);
    }

    [Fact]
    public async Task HandleAsync_MarkFailedThrows_ReturnsFinalizeFailed()
    {
        var repository = new PublishFakePublicationRepository
        {
            MarkFailedThrows = new InvalidOperationException("storage unavailable")
        };
        var publisher = new PublishFakePublisher
        {
            Throws = new PlatformPublishException("provider down")
        };
        var handler = CreateHandler(
            Event(FutureStart),
            Platform(),
            publisher,
            repository: repository);

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.FinalizeFailed, result.Status);
    }

    [Fact]
    public async Task HandleAsync_PostStartOperationCanceled_AttemptsFailedFinalization()
    {
        var repository = new PublishFakePublicationRepository();
        var publisher = new PublishFakePublisher
        {
            Throws = new OperationCanceledException()
        };
        var handler = CreateHandler(
            Event(FutureStart),
            Platform(),
            publisher,
            repository: repository);

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.Failed, result.Status);
        Assert.True(repository.Started);
        Assert.True(repository.MarkFailedCalled);
        Assert.False(repository.ReleaseCalled);
    }

    [Fact]
    public async Task HandleAsync_RequestCanceledBeforeStart_DoesNotCreateAttempt()
    {
        using var request = new CancellationTokenSource();
        request.Cancel();
        var repository = new PublishFakePublicationRepository();
        var executionScopes = new PublishFakeExecutionScopeFactory();
        var handler = CreateHandler(
            Event(FutureStart),
            Platform(),
            new PublishFakePublisher(),
            repository: repository,
            executionScopes: executionScopes);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            handler.HandleAsync(
                new PublishCommand(CalendarEventId, PlatformId),
                request.Token));

        Assert.False(repository.Started);
        Assert.False(executionScopes.CreateCalled);
    }

    [Fact]
    public async Task HandleAsync_RequestCanceledAfterStart_UsesServerOwnedTokensAndPublishes()
    {
        using var request = new CancellationTokenSource();
        using var operation = new CancellationTokenSource();
        using var finalization = new CancellationTokenSource();
        var repository = new PublishFakePublicationRepository();
        var publisher = new PublishFakePublisher { OnPublish = request.Cancel };
        var executionScopes = new PublishFakeExecutionScopeFactory();
        executionScopes.Scope.OperationToken = operation.Token;
        executionScopes.Scope.FinalizationToken = finalization.Token;
        var handler = CreateHandler(
            Event(FutureStart),
            Platform(),
            publisher,
            repository: repository,
            executionScopes: executionScopes);

        var result = await handler.HandleAsync(
            new PublishCommand(CalendarEventId, PlatformId),
            request.Token);

        Assert.Equal(PublishResultStatus.Published, result.Status);
        Assert.True(request.IsCancellationRequested);
        Assert.Equal(operation.Token, repository.StartToken);
        Assert.Equal(operation.Token, publisher.CancellationToken);
        Assert.Equal(operation.Token, repository.CheckpointToken);
        Assert.Equal(finalization.Token, repository.MarkPublishedToken);
        Assert.NotEqual(request.Token, repository.StartToken);
    }

    [Fact]
    public async Task HandleAsync_ProviderTimeout_RecordsFailedWithBoundedFinalization()
    {
        using var finalization = new CancellationTokenSource();
        var repository = new PublishFakePublicationRepository();
        var publisher = new PublishFakePublisher
        {
            Throws = new PlatformPublishException(
                "provider timeout",
                externalResourceId: null,
                failureKind: PlatformPublishFailureKind.Timeout)
        };
        var executionScopes = new PublishFakeExecutionScopeFactory();
        executionScopes.Scope.FinalizationToken = finalization.Token;
        var handler = CreateHandler(
            Event(FutureStart),
            Platform(),
            publisher,
            repository: repository,
            executionScopes: executionScopes);

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.Failed, result.Status);
        Assert.Equal(finalization.Token, repository.MarkFailedToken);
    }

    [Fact]
    public async Task HandleAsync_CheckpointRace_StopsPublishAndRecordsFailed()
    {
        var repository = new PublishFakePublicationRepository
        {
            SaveExternalResourceIdOutcome = SaveExternalResourceIdResult.Changed
        };
        var handler = CreateHandler(
            Event(FutureStart),
            Platform(),
            new PublishFakePublisher(),
            repository: repository);

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.Failed, result.Status);
        Assert.True(repository.SaveExternalResourceIdCalled);
        Assert.True(repository.MarkFailedCalled);
        Assert.Equal("yt-broadcast-id", repository.FailedExternalResourceId);
        Assert.False(repository.MarkPublishedCalled);
    }

    [Fact]
    public async Task HandleAsync_CheckpointCancellation_RetriesKnownIdDuringFinalization()
    {
        var repository = new PublishFakePublicationRepository
        {
            CheckpointThrows = new OperationCanceledException()
        };
        var handler = CreateHandler(
            Event(FutureStart),
            Platform(),
            new PublishFakePublisher(),
            repository: repository);

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.Failed, result.Status);
        Assert.Equal("yt-broadcast-id", repository.FailedExternalResourceId);
        Assert.False(repository.MarkPublishedCalled);
    }

    [Fact]
    public async Task HandleAsync_FinalizationTimeout_ReturnsFinalizeFailedAndLogsCritical()
    {
        var repository = new PublishFakePublicationRepository();
        var executionScopes = new PublishFakeExecutionScopeFactory();
        executionScopes.Scope.FinalizationThrows = new OperationCanceledException();
        var logger = new CapturingLogger<PublishHandler>();
        var handler = CreateHandler(
            Event(FutureStart),
            Platform(),
            new PublishFakePublisher(),
            repository: repository,
            logger: logger,
            executionScopes: executionScopes);

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.FinalizeFailed, result.Status);
        Assert.Equal(2, executionScopes.Scope.FinalizationCalls);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Critical);
    }

    [Fact]
    public async Task HandleAsync_PublishedFollowUpCancellation_DoesNotReopenFinalRow()
    {
        var repository = new PublishFakePublicationRepository();
        var publicationIndex = new FakeCalendarEventPublicationIndexWriter
        {
            AddException = new OperationCanceledException()
        };
        var handler = CreateHandler(
            Event(FutureStart),
            Platform(),
            new PublishFakePublisher(),
            repository: repository,
            publicationIndex: publicationIndex);

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.Published, result.Status);
        Assert.True(repository.MarkPublishedCalled);
        Assert.False(repository.MarkFailedCalled);
    }

    [Fact]
    public async Task HandleAsync_Success_StartsPublishesFinalizesAndReturnsPublished()
    {
        var publishedUtc = new DateTimeOffset(2026, 6, 22, 12, 0, 5, TimeSpan.Zero);
        var repository = new PublishFakePublicationRepository
        {
            MarkPublishedResult = publishedUtc
        };
        var publisher = new PublishFakePublisher
        {
            Result = new PlatformPublishResult("yt-broadcast-id")
        };
        var publicationIndex = new FakeCalendarEventPublicationIndexWriter();
        var handler = CreateHandler(
            Event(FutureStart),
            Platform(),
            publisher,
            repository: repository,
            publicationIndex: publicationIndex);

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.Published, result.Status);
        Assert.NotNull(result.Platform);
        Assert.Equal(PlatformId, result.Platform!.PlatformId);
        Assert.Equal("Main YouTube channel", result.Platform.PlatformName);
        Assert.Equal(PlatformType.YouTube, result.Platform.PlatformType);
        Assert.Equal(PublishStatus.Published, result.Platform.Status);
        Assert.Equal("yt-broadcast-id", result.Platform.ExternalResourceId);
        Assert.Equal(ThumbnailPublishStatus.NotConfigured, result.Platform.ThumbnailStatus);
        Assert.Equal(publishedUtc, result.Platform.PublishedUtc);
        Assert.Null(result.Platform.PlatformDeletedUtc);
        Assert.False(result.Platform.CanPublish);
        Assert.True(result.Platform.CanDeletePublication);
        Assert.True(result.Platform.CanPreviewPublishingContent);

        Assert.True(repository.Started);
        Assert.Equal("yt-broadcast-id", repository.MarkedExternalResourceId);
        Assert.False(repository.ReleaseCalled);
        Assert.Equal("English title", repository.StartedAttempt!.ContentSnapshot.Title);
        Assert.Equal("English description", repository.StartedAttempt.ContentSnapshot.Description);

        Assert.Equal("English title", publisher.Request!.Title);
        Assert.Equal("English description", publisher.Request.Description);
        Assert.Equal(FutureStart, publisher.Request.ScheduledStartUtc);
        Assert.Same(YouTubePublishSettings, publisher.Request.PublishSettings);
        Assert.Equal([(CalendarEventId, PlatformId)], publicationIndex.AddCalls);
    }

    [Fact]
    public async Task HandleAsync_PublicationIndexReturnsFalse_LogsAndReturnsPublished()
    {
        var publicationIndex = new FakeCalendarEventPublicationIndexWriter
        {
            AddResult = false
        };
        var logger = new CapturingLogger<PublicationIndexUpdater>();
        var handler = CreateHandler(
            Event(FutureStart),
            Platform(),
            new PublishFakePublisher(),
            publicationIndex: publicationIndex,
            publicationIndexLogger: logger);

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.Published, result.Status);
        Assert.Equal([(CalendarEventId, PlatformId)], publicationIndex.AddCalls);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains("AddPublishedPlatform", entry.Message, StringComparison.Ordinal);
        Assert.Contains(CalendarEventId, entry.Message, StringComparison.Ordinal);
        Assert.Contains(PlatformId, entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_PublicationIndexThrows_LogsAndReturnsPublished()
    {
        var publicationIndex = new FakeCalendarEventPublicationIndexWriter
        {
            AddException = new InvalidOperationException("storage unavailable")
        };
        var logger = new CapturingLogger<PublicationIndexUpdater>();
        var handler = CreateHandler(
            Event(FutureStart),
            Platform(),
            new PublishFakePublisher(),
            publicationIndex: publicationIndex,
            publicationIndexLogger: logger);

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.Published, result.Status);
        Assert.Equal([(CalendarEventId, PlatformId)], publicationIndex.AddCalls);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains("AddPublishedPlatform", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_WordPressSuccess_ReturnsWordPressPlatformAndPostId()
    {
        var publishedUtc = new DateTimeOffset(2026, 6, 22, 12, 0, 5, TimeSpan.Zero);
        var repository = new PublishFakePublicationRepository
        {
            MarkPublishedResult = publishedUtc
        };
        var publisher = new PublishFakePublisher(
            PlatformType.WordPress,
            new PlatformPublishResult("123"));
        var handler = CreateHandler(
            Event(FutureStart),
            Platform(
                "Company blog",
                PlatformType.WordPress,
                WordPressPublishSettings),
            publisher,
            repository: repository);

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.Published, result.Status);
        Assert.NotNull(result.Platform);
        Assert.Equal("Company blog", result.Platform!.PlatformName);
        Assert.Equal(PlatformType.WordPress, result.Platform.PlatformType);
        Assert.Equal(publishedUtc, result.Platform.PublishedUtc);
        Assert.Equal("123", result.Platform.ExternalResourceId);
        Assert.Null(result.Platform.ThumbnailStatus);
        Assert.False(result.Platform.CanPublish);
        Assert.True(result.Platform.CanDeletePublication);
        Assert.True(result.Platform.CanPreviewPublishingContent);

        Assert.Equal("123", repository.MarkedExternalResourceId);
        Assert.Same(WordPressPublishSettings, publisher.Request!.PublishSettings);
    }
}
