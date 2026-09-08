using Microsoft.Extensions.Logging;
using static YTSkedy.Scheduling.Application.Test.PublishHandlerScenario;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Platforms.Providers;
using YTSkedy.Scheduling.Application.Platforms.Publications;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.TestSupport;

namespace YTSkedy.Scheduling.Application.Test;

public class PublishHandlerLifecycleTests
{
    private readonly PublishHandlerScenario _scenario = new();

    [Fact]
    public async Task HandleAsync_AttemptConflict_ReturnsPublishInProgress()
    {
        var scenario = _scenario;
        scenario.PublicationAttempts
            .Setup(candidate => candidate.StartPublishingAsync(
                It.IsAny<PlatformPublicationAttempt>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(StartPublicationResult.Conflict);

        var result = await scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.PublishInProgress, result.Status);
        VerifyNoIndexAdd();
    }

    [Fact]
    public async Task HandleAsync_ProviderFailure_MarksFailedWithoutExternalId()
    {
        var scenario = _scenario;
        SetPublisherException(new PlatformPublishException("provider down"));

        var result = await scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.Failed, result.Status);
        VerifyMarkedFailed(externalResourceId: null);
        VerifyNoReleaseOrPublished();
        VerifyNoIndexAdd();
    }

    [Fact]
    public async Task HandleAsync_WordPressRateLimit_PreservesFailureAndAttemptId()
    {
        var scenario = _scenario;
        var retryAfterUtc = Now.AddMinutes(1);
        SetPublisherException(
            new PlatformPublishException(
                new PlatformPublishFailure(
                    PlatformPublishFailureCodes.WordPressRateLimited,
                    "WordPress limited publishing requests.",
                    "create_post",
                    429,
                    "imunify_rate_limited",
                    retryAfterUtc,
                    VerificationRequired: true)));

        var result = await scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.Failed, result.Status);
        Assert.NotNull(result.Failure);
        Assert.Equal(PlatformPublishFailureCodes.WordPressRateLimited, result.Failure!.Code);
        Assert.Equal(429, result.Failure.ProviderStatus);
        Assert.Equal("imunify_rate_limited", result.Failure.ProviderErrorCode);
        Assert.Equal(retryAfterUtc, result.Failure.RetryAfterUtc);
        Assert.Equal(Now, result.Failure.FailedUtc);
        Assert.NotNull(result.Failure.AttemptId);
        Assert.Equal(32, result.Failure.AttemptId.Length);
        scenario.PublicationAttempts.Verify(candidate => candidate.MarkFailedAsync(
            CalendarEventId,
            PlatformId,
            null,
            It.Is<PublicationFailure>(failure =>
                failure.Code == PlatformPublishFailureCodes.WordPressRateLimited &&
                failure.AttemptId == result.Failure.AttemptId),
            It.IsAny<CancellationToken>()));
        scenario.PublicationAttempts.Verify(candidate => candidate.StartPublishingAsync(
            It.Is<PlatformPublicationAttempt>(attempt =>
                attempt.AttemptId == result.Failure.AttemptId),
            It.IsAny<CancellationToken>()));
        scenario.Publisher.Verify(candidate => candidate.PublishAsync(
            It.Is<PlatformPublishRequest>(request =>
                request.AttemptId == result.Failure.AttemptId),
            It.IsAny<IPlatformPublishCheckpoint>(),
            It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task HandleAsync_ProviderFailureWithExternalId_MarksFailedWithExternalId()
    {
        var scenario = _scenario;
        SetPublisherException(
            new PlatformPublishException(
                "metadata update failed",
                "yt-created-broadcast-id"));

        var result = await scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.Failed, result.Status);
        VerifyMarkedFailed("yt-created-broadcast-id");
        VerifyNoRelease();
    }

    [Fact]
    public async Task HandleAsync_ProviderValidationFailure_MarksFailed()
    {
        var scenario = _scenario;
        SetPublisherException(
            new PlatformPublishValidationException("invalid provider settings"));

        var result = await scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.Failed, result.Status);
        VerifyMarkedFailed(externalResourceId: null);
        VerifyNoReleaseOrPublished();
        VerifyNoIndexAdd();
    }

    [Fact]
    public async Task HandleAsync_FinalizeReturnsNull_MarksFailedWithProviderId()
    {
        var scenario = _scenario;
        scenario.PublicationAttempts
            .Setup(candidate => candidate.MarkPublishedAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((DateTimeOffset?)null);

        var result = await scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.Failed, result.Status);
        VerifyMarkedFailed(ExternalResourceId);
        VerifyNoRelease();
        VerifyNoIndexAdd();
    }

    [Fact]
    public async Task HandleAsync_FinalizeAndMarkFailedCannotWrite_ReturnsFinalizeFailed()
    {
        var scenario = _scenario;
        scenario.PublicationAttempts
            .Setup(candidate => candidate.MarkPublishedAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((DateTimeOffset?)null);
        scenario.PublicationAttempts
            .Setup(candidate => candidate.MarkFailedAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<PublicationFailure>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(MarkFailedResult.Changed);

        var result = await scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.FinalizeFailed, result.Status);
        VerifyMarkedFailed(ExternalResourceId);
    }

    [Fact]
    public async Task HandleAsync_FinalizeThrows_MarksFailedWithProviderId()
    {
        var scenario = _scenario;
        scenario.PublicationAttempts
            .Setup(candidate => candidate.MarkPublishedAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("storage unavailable"));

        var result = await scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.Failed, result.Status);
        VerifyMarkedFailed(ExternalResourceId);
    }

    [Fact]
    public async Task HandleAsync_MarkFailedThrows_ReturnsFinalizeFailed()
    {
        var scenario = _scenario;
        SetPublisherException(new PlatformPublishException("provider down"));
        scenario.PublicationAttempts
            .Setup(candidate => candidate.MarkFailedAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<PublicationFailure>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("storage unavailable"));

        var result = await scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.FinalizeFailed, result.Status);
    }

    [Fact]
    public async Task HandleAsync_PostStartOperationCanceled_AttemptsFailedFinalization()
    {
        var scenario = _scenario;
        SetPublisherException(new OperationCanceledException());

        var result = await scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.Failed, result.Status);
        scenario.PublicationAttempts.Verify(candidate => candidate.StartPublishingAsync(
            It.IsAny<PlatformPublicationAttempt>(),
            It.IsAny<CancellationToken>()));
        VerifyMarkedFailed(externalResourceId: null);
        VerifyNoRelease();
    }

    [Fact]
    public async Task HandleAsync_RequestCanceledBeforeStart_DoesNotCreateAttempt()
    {
        using var request = new CancellationTokenSource();
        request.Cancel();
        var scenario = _scenario;

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            scenario.HandleAsync(request.Token));

        scenario.PublicationAttempts.Verify(candidate => candidate.StartPublishingAsync(
            It.IsAny<PlatformPublicationAttempt>(),
            It.IsAny<CancellationToken>()), Times.Never());
        Assert.False(scenario.ExecutionScopes.CreateCalled);
    }

    [Fact]
    public async Task HandleAsync_RequestCanceledAfterStart_UsesServerOwnedTokensAndPublishes()
    {
        using var request = new CancellationTokenSource();
        using var operation = new CancellationTokenSource();
        using var finalization = new CancellationTokenSource();
        var scenario = _scenario;
        scenario.ExecutionScopes.Scope.OperationToken = operation.Token;
        scenario.ExecutionScopes.Scope.FinalizationToken = finalization.Token;
        scenario.Publisher
            .Setup(candidate => candidate.PublishAsync(
                It.IsAny<PlatformPublishRequest>(),
                It.IsAny<IPlatformPublishCheckpoint>(),
                operation.Token))
            .Returns<PlatformPublishRequest, IPlatformPublishCheckpoint, CancellationToken>(
                async (_, checkpoint, cancellationToken) =>
                {
                    request.Cancel();
                    await checkpoint.SaveExternalResourceIdAsync(
                        ExternalResourceId,
                        cancellationToken);
                    return new PlatformPublishResult(ExternalResourceId);
                });

        var result = await scenario.HandleAsync(request.Token);

        Assert.Equal(PublishResultStatus.Published, result.Status);
        Assert.True(request.IsCancellationRequested);
        scenario.PublicationAttempts.Verify(candidate => candidate.StartPublishingAsync(
            It.IsAny<PlatformPublicationAttempt>(),
            operation.Token));
        scenario.Publisher.Verify(candidate => candidate.PublishAsync(
            It.IsAny<PlatformPublishRequest>(),
            It.IsAny<IPlatformPublishCheckpoint>(),
            operation.Token));
        scenario.PublicationAttempts.Verify(candidate => candidate.SaveExternalResourceIdAsync(
            CalendarEventId,
            PlatformId,
            ExternalResourceId,
            operation.Token));
        scenario.PublicationAttempts.Verify(candidate => candidate.MarkPublishedAsync(
            CalendarEventId,
            PlatformId,
            ExternalResourceId,
            finalization.Token));
        Assert.NotEqual(request.Token, operation.Token);
    }

    [Fact]
    public async Task HandleAsync_ProviderTimeout_RecordsFailedWithBoundedFinalization()
    {
        using var finalization = new CancellationTokenSource();
        var scenario = _scenario;
        scenario.ExecutionScopes.Scope.FinalizationToken = finalization.Token;
        SetPublisherException(
            new PlatformPublishException(
                "provider timeout",
                externalResourceId: null,
                failureKind: PlatformPublishFailureKind.Timeout));

        var result = await scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.Failed, result.Status);
        scenario.PublicationAttempts.Verify(candidate => candidate.MarkFailedAsync(
            CalendarEventId,
            PlatformId,
            null,
            It.IsAny<PublicationFailure>(),
            finalization.Token));
    }

    [Fact]
    public async Task HandleAsync_CheckpointRace_StopsPublishAndRecordsFailed()
    {
        var scenario = _scenario;
        scenario.PublicationAttempts
            .Setup(candidate => candidate.SaveExternalResourceIdAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(SaveExternalResourceIdResult.Changed);

        var result = await scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.Failed, result.Status);
        scenario.PublicationAttempts.Verify(candidate => candidate.SaveExternalResourceIdAsync(
            CalendarEventId,
            PlatformId,
            ExternalResourceId,
            It.IsAny<CancellationToken>()));
        VerifyMarkedFailed(ExternalResourceId);
        VerifyNoMarkPublished();
    }

    [Fact]
    public async Task HandleAsync_CheckpointCancellation_RetriesKnownIdDuringFinalization()
    {
        var scenario = _scenario;
        scenario.PublicationAttempts
            .Setup(candidate => candidate.SaveExternalResourceIdAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var result = await scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.Failed, result.Status);
        VerifyMarkedFailed(ExternalResourceId);
        VerifyNoMarkPublished();
    }

    [Fact]
    public async Task HandleAsync_FinalizationTimeout_ReturnsFinalizeFailedAndLogsCritical()
    {
        var scenario = _scenario;
        scenario.ExecutionScopes.Scope.FinalizationThrows = new OperationCanceledException();

        var result = await scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.FinalizeFailed, result.Status);
        Assert.Equal(2, scenario.ExecutionScopes.Scope.FinalizationCalls);
        Assert.Contains(
            scenario.Logger.GetLogEntries(),
            entry => entry.Level == LogLevel.Critical);
    }

    [Fact]
    public async Task HandleAsync_PublishedFollowUpCancellation_DoesNotReopenFinalRow()
    {
        var scenario = _scenario;
        scenario.PublicationIndex
            .Setup(candidate => candidate.AddPublishedPlatformAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var result = await scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.Published, result.Status);
        scenario.PublicationAttempts.Verify(candidate => candidate.MarkPublishedAsync(
            CalendarEventId,
            PlatformId,
            ExternalResourceId,
            It.IsAny<CancellationToken>()));
        scenario.PublicationAttempts.Verify(candidate => candidate.MarkFailedAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<PublicationFailure>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task HandleAsync_Success_StartsPublishesFinalizesAndReturnsPublished()
    {
        var scenario = _scenario;

        var result = await scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.Published, result.Status);
        Assert.NotNull(result.Platform);
        Assert.Equal(PlatformId, result.Platform!.PlatformId);
        Assert.Equal("Main YouTube channel", result.Platform.PlatformName);
        Assert.Equal(PlatformType.YouTube, result.Platform.PlatformType);
        Assert.Equal(PublishStatus.Published, result.Platform.Status);
        Assert.Equal(ExternalResourceId, result.Platform.ExternalResourceId);
        Assert.Equal(ThumbnailPublishStatus.NotConfigured, result.Platform.ThumbnailStatus);
        Assert.Equal(DefaultPublishedUtc, result.Platform.PublishedUtc);
        Assert.Null(result.Platform.PlatformDeletedUtc);
        Assert.False(result.Platform.CanPublish);
        Assert.True(result.Platform.CanDeletePublication);
        Assert.True(result.Platform.CanPreviewPublishingContent);

        scenario.PublicationAttempts.Verify(candidate => candidate.StartPublishingAsync(
            It.Is<PlatformPublicationAttempt>(attempt =>
                attempt.ContentSnapshot.Title == "English title" &&
                attempt.ContentSnapshot.Description == "English description"),
            It.IsAny<CancellationToken>()));
        scenario.PublicationAttempts.Verify(candidate => candidate.MarkPublishedAsync(
            CalendarEventId,
            PlatformId,
            ExternalResourceId,
            It.IsAny<CancellationToken>()));
        VerifyNoRelease();
        scenario.Publisher.Verify(candidate => candidate.PublishAsync(
            It.Is<PlatformPublishRequest>(request =>
                request.Title == "English title" &&
                request.Description == "English description" &&
                request.ScheduledStartUtc == FutureStart &&
                ReferenceEquals(request.PublishSettings, YouTubePublishSettings)),
            It.IsAny<IPlatformPublishCheckpoint>(),
            It.IsAny<CancellationToken>()));
        scenario.PublicationIndex.Verify(candidate => candidate.AddPublishedPlatformAsync(
            CalendarEventId,
            PlatformId,
            It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task HandleAsync_PublicationIndexReturnsFalse_LogsAndReturnsPublished()
    {
        var scenario = _scenario;
        scenario.PublicationIndex
            .Setup(candidate => candidate.AddPublishedPlatformAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.Published, result.Status);
        scenario.PublicationIndex.Verify(candidate => candidate.AddPublishedPlatformAsync(
            CalendarEventId,
            PlatformId,
            It.IsAny<CancellationToken>()));
        var entry = Assert.Single(scenario.PublicationIndexLogger.GetLogEntries());
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains("AddPublishedPlatform", entry.Message, StringComparison.Ordinal);
        Assert.Contains(CalendarEventId, entry.Message, StringComparison.Ordinal);
        Assert.Contains(PlatformId, entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_PublicationIndexThrows_LogsAndReturnsPublished()
    {
        var scenario = _scenario;
        scenario.PublicationIndex
            .Setup(candidate => candidate.AddPublishedPlatformAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("storage unavailable"));

        var result = await scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.Published, result.Status);
        scenario.PublicationIndex.Verify(candidate => candidate.AddPublishedPlatformAsync(
            CalendarEventId,
            PlatformId,
            It.IsAny<CancellationToken>()));
        var entry = Assert.Single(scenario.PublicationIndexLogger.GetLogEntries());
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains("AddPublishedPlatform", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_WordPressSuccess_ReturnsWordPressPlatformAndPostId()
    {
        var wordpressPlatform = Platform(
            "Company blog",
            PlatformType.WordPress,
            WordPressPublishSettings);
        var scenario = _scenario;
        scenario.SelectedPlatform = wordpressPlatform;
        scenario.ActivePlatforms = [wordpressPlatform];
        scenario.Publisher.SetupGet(candidate => candidate.Type).Returns(PlatformType.WordPress);
        scenario.Publisher
            .Setup(candidate => candidate.PublishAsync(
                It.IsAny<PlatformPublishRequest>(),
                It.IsAny<IPlatformPublishCheckpoint>(),
                It.IsAny<CancellationToken>()))
            .Returns<PlatformPublishRequest, IPlatformPublishCheckpoint, CancellationToken>(
                async (_, checkpoint, cancellationToken) =>
                {
                    await checkpoint.SaveExternalResourceIdAsync("123", cancellationToken);
                    return new PlatformPublishResult("123");
                });

        var result = await scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.Published, result.Status);
        Assert.NotNull(result.Platform);
        Assert.Equal("Company blog", result.Platform!.PlatformName);
        Assert.Equal(PlatformType.WordPress, result.Platform.PlatformType);
        Assert.Equal(DefaultPublishedUtc, result.Platform.PublishedUtc);
        Assert.Equal("123", result.Platform.ExternalResourceId);
        Assert.Null(result.Platform.ThumbnailStatus);
        Assert.False(result.Platform.CanPublish);
        Assert.True(result.Platform.CanDeletePublication);
        Assert.True(result.Platform.CanPreviewPublishingContent);
        scenario.PublicationAttempts.Verify(candidate => candidate.MarkPublishedAsync(
            CalendarEventId,
            PlatformId,
            "123",
            It.IsAny<CancellationToken>()));
        scenario.Publisher.Verify(candidate => candidate.PublishAsync(
            It.Is<PlatformPublishRequest>(request =>
                ReferenceEquals(request.PublishSettings, WordPressPublishSettings)),
            It.IsAny<IPlatformPublishCheckpoint>(),
            It.IsAny<CancellationToken>()));
    }

    private void SetPublisherException(Exception exception) =>
        _scenario.Publisher
            .Setup(candidate => candidate.PublishAsync(
                It.IsAny<PlatformPublishRequest>(),
                It.IsAny<IPlatformPublishCheckpoint>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

    private void VerifyMarkedFailed(string? externalResourceId) =>
        _scenario.PublicationAttempts.Verify(candidate => candidate.MarkFailedAsync(
            CalendarEventId,
            PlatformId,
            externalResourceId,
            It.IsAny<PublicationFailure>(),
            It.IsAny<CancellationToken>()));

    private void VerifyNoRelease() =>
        _scenario.PublicationAttempts.Verify(candidate => candidate.ReleasePublishingAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());

    private void VerifyNoReleaseOrPublished()
    {
        VerifyNoRelease();
        VerifyNoMarkPublished();
    }

    private void VerifyNoMarkPublished() =>
        _scenario.PublicationAttempts.Verify(candidate => candidate.MarkPublishedAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());

    private void VerifyNoIndexAdd() =>
        _scenario.PublicationIndex.Verify(candidate => candidate.AddPublishedPlatformAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());

}
