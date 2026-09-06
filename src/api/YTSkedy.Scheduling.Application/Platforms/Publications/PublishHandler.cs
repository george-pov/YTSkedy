using Microsoft.Extensions.Logging;
using System.Diagnostics;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.Platforms.Content;
using YTSkedy.Scheduling.Application.Platforms.EventPlatforms;
using YTSkedy.Scheduling.Application.Platforms.PublicationThumbnails;
using YTSkedy.Scheduling.Application.Platforms.Providers;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms.Publications;

/// <summary>
/// Publishes one calendar event to one platform. Reads and preflight work honor
/// request cancellation. Immediately before reserving a publication row, the
/// handler switches to a bounded server-owned execution scope so a client
/// disconnect cannot strand an attempt that this process can still finalize.
/// </summary>
public sealed class PublishHandler(
    ICalendarEventReader calendarEvents,
    IPlatformReader platforms,
    IPlatformPublicationReader publications,
    IPublicationAttemptWriter publicationAttempts,
    PublicationIndexUpdater publicationIndexUpdater,
    IPlatformTypeAdapterSelector<IPlatformPublisher> publishers,
    PublicationThumbnailApplier thumbnailApplier,
    PublishingContentRenderer contentRenderer,
    IPublishExecutionScopeFactory publicationExecutionScopes,
    TimeProvider timeProvider,
    ILogger<PublishHandler> logger)
{
    public async Task<PublishResult> HandleAsync(
        PublishCommand command,
        CancellationToken requestToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var calendarEvent = await calendarEvents.GetByIdAsync(
            command.CalendarEventId,
            requestToken);
        if (calendarEvent is null)
        {
            return PublishResult.ForStatus(PublishResultStatus.EventNotFound);
        }

        var platform = await platforms.GetAsync(command.PlatformId, requestToken);
        if (platform is null)
        {
            return PublishResult.ForStatus(PublishResultStatus.PlatformNotFound);
        }

        var publisher = publishers.Find(platform.Type);
        if (publisher is null)
        {
            return PublishResult.ForStatus(PublishResultStatus.ProviderNotSupported);
        }

        var existing = await publications.GetAsync(
            command.CalendarEventId,
            command.PlatformId,
            requestToken);
        var existingPublicationStatus = ValidateExistingPublication(existing);
        if (existingPublicationStatus is not null)
        {
            return PublishResult.ForStatus(existingPublicationStatus.Value);
        }

        if (calendarEvent.ScheduledStartUtc <= timeProvider.GetUtcNow())
        {
            return PublishResult.ForStatus(PublishResultStatus.PastStart);
        }

        var runtimeTokenValues = await PlatformReferenceTokenValues.BuildAsync(
            platforms,
            publications,
            command.CalendarEventId,
            requestToken);
        var renderResult = await contentRenderer.RenderAsync(
            platform,
            calendarEvent,
            runtimeTokenValues,
            requestToken);
        if (renderResult.Status != RenderContentStatus.Rendered ||
            renderResult.HasUnresolvedPlaceholders)
        {
            return PublishResult.ForStatus(PublishResultStatus.InvalidPublishingContent);
        }

        var renderedContent = renderResult.Content!;
        var attemptId = Guid.NewGuid().ToString("N");
        Activity.Current?.SetTag("ytskedy.publish.attempt_id", attemptId);
        Activity.Current?.SetTag("ytskedy.publish.provider", platform.Type.ToString());
        var attempt = new PlatformPublicationAttempt(
            command.CalendarEventId,
            command.PlatformId,
            platform.Name,
            platform.Type,
            platform.PublishSettings,
            new ContentSnapshot(renderedContent.Title, renderedContent.Description),
            attemptId);

        // Point of no request cancellation. All started-attempt work below is
        // owned by the bounded execution scope, never by the HTTP request.
        requestToken.ThrowIfCancellationRequested();
        using var execution = publicationExecutionScopes.Create();
        var startResult = await publicationAttempts.StartPublishingAsync(
            attempt,
            execution.OperationToken);
        if (startResult == StartPublicationResult.Conflict)
        {
            return PublishResult.ForStatus(PublishResultStatus.PublishInProgress);
        }

        PublicationThumbnail thumbnail;
        try
        {
            thumbnail = await thumbnailApplier.LoadAsync(
                command.CalendarEventId,
                execution.OperationToken);
        }
        catch (OperationCanceledException exception)
        {
            LogExecutionCancellation(exception, execution, command, "loading thumbnail content");
            return await RecordFailedAsync(
                execution,
                command,
                attemptId,
                externalResourceId: null,
                new PlatformPublishFailure(
                    PlatformPublishFailureCodes.ProviderCanceled,
                    "Publication was canceled while loading thumbnail content.",
                    "load_thumbnail",
                    VerificationRequired: false));
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Failed to load thumbnail content for calendar event {CalendarEventId} before " +
                "publishing platform {PlatformId}.",
                command.CalendarEventId,
                command.PlatformId);
            return await RecordFailedAsync(
                execution,
                command,
                attemptId,
                externalResourceId: null,
                new PlatformPublishFailure(
                    PlatformPublishFailureCodes.ThumbnailLoadFailed,
                    "YTSkedy could not load the thumbnail before publishing.",
                    "load_thumbnail",
                    VerificationRequired: false));
        }

        PlatformPublishResult publishResult;
        var checkpoint = new PublishCheckpoint(
            publicationAttempts,
            command.CalendarEventId,
            command.PlatformId);
        try
        {
            publishResult = await publisher.PublishAsync(
                new PlatformPublishRequest(
                    command.CalendarEventId,
                    command.PlatformId,
                    platform.PublishSettings,
                    renderedContent.Title,
                    renderedContent.Description,
                    calendarEvent.ScheduledStartUtc,
                    attemptId),
                checkpoint,
                execution.OperationToken);
        }
        catch (PlatformPublishValidationException exception)
        {
            logger.LogWarning(
                exception,
                "Publishing calendar event {CalendarEventId} to platform {PlatformId} " +
                "failed provider-specific validation.",
                command.CalendarEventId,
                command.PlatformId);
            return await RecordFailedAsync(
                execution,
                command,
                attemptId,
                externalResourceId: null,
                new PlatformPublishFailure(
                    PlatformPublishFailureCodes.ProviderValidationFailed,
                    exception.Message,
                    "validate_request",
                    VerificationRequired: false));
        }
        catch (PlatformPublishException exception)
        {
            LogProviderFailure(exception, command, attemptId);
            return await RecordFailedAsync(
                execution,
                command,
                attemptId,
                exception.ExternalResourceId ?? checkpoint.ExternalResourceId,
                exception.Failure);
        }
        catch (OperationCanceledException exception)
        {
            LogExecutionCancellation(exception, execution, command, "calling the provider");
            var source = execution.ClassifyCancellation();
            return await RecordFailedAsync(
                execution,
                command,
                attemptId,
                checkpoint.ExternalResourceId,
                new PlatformPublishFailure(
                    source == PublishCancellationSource.OperationTimeout
                        ? PlatformPublishFailureCodes.ProviderTimeout
                        : PlatformPublishFailureCodes.ProviderCanceled,
                    source == PublishCancellationSource.OperationTimeout
                        ? "The publishing provider timed out."
                        : "The publishing provider call was canceled.",
                    "provider",
                    VerificationRequired: true));
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Publishing calendar event {CalendarEventId} to platform {PlatformId} failed " +
                "with unexpected provider exception type {ExceptionType}.",
                command.CalendarEventId,
                command.PlatformId,
                exception.GetType().FullName);
            return await RecordFailedAsync(
                execution,
                command,
                attemptId,
                checkpoint.ExternalResourceId,
                new PlatformPublishFailure(
                    PlatformPublishFailureCodes.ProviderFailure,
                    "The publishing provider failed unexpectedly.",
                    "provider",
                    VerificationRequired: true));
        }

        DateTimeOffset? publishedUtc;
        try
        {
            publishedUtc = await execution.RunFinalizationAsync(
                token => publicationAttempts.MarkPublishedAsync(
                    command.CalendarEventId,
                    command.PlatformId,
                    publishResult.ExternalResourceId,
                    token));
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Published calendar event {CalendarEventId} to platform {PlatformId} as " +
                "external resource {ExternalResourceId}, but finalizing Published failed.",
                command.CalendarEventId,
                command.PlatformId,
                publishResult.ExternalResourceId);
            return await RecordFailedAsync(
                execution,
                command,
                attemptId,
                publishResult.ExternalResourceId,
                new PlatformPublishFailure(
                    PlatformPublishFailureCodes.ProviderFailure,
                    "The provider resource was created, but YTSkedy could not finalize publication state.",
                    "finalize_publication",
                    VerificationRequired: true));
        }

        if (publishedUtc is null)
        {
            logger.LogError(
                "Published calendar event {CalendarEventId} to platform {PlatformId} as external " +
                "resource {ExternalResourceId}, but the publication row rejected finalization.",
                command.CalendarEventId,
                command.PlatformId,
                publishResult.ExternalResourceId);
            return await RecordFailedAsync(
                execution,
                command,
                attemptId,
                publishResult.ExternalResourceId,
                new PlatformPublishFailure(
                    PlatformPublishFailureCodes.ProviderFailure,
                    "The provider resource was created, but YTSkedy could not finalize publication state.",
                    "finalize_publication",
                    VerificationRequired: true));
        }

        await RunPublishedFollowUpAsync(
            () => publicationIndexUpdater.AddPublishedPlatformAsync(
                command.CalendarEventId,
                command.PlatformId,
                execution.OperationToken),
            execution,
            command,
            "updating the publication index");

        var thumbnailStatus = ThumbnailPublicationPolicy.InitialStatusFor(platform.Type);
        try
        {
            thumbnailStatus = await thumbnailApplier.ApplyAsync(
                new PublicationThumbnailCommand(
                    command.CalendarEventId,
                    command.PlatformId,
                    platform,
                    publishResult.ExternalResourceId,
                    thumbnail),
                execution.OperationToken);
        }
        catch (Exception exception)
        {
            LogPublishedFollowUpFailure(
                exception,
                execution,
                command,
                "applying the publication thumbnail");
        }

        return PublishResult.Published(
            EventPlatformMapper.MapPublished(
                calendarEvent,
                platform,
                publishResult.ExternalResourceId,
                publishedUtc.Value,
                timeProvider.GetUtcNow(),
                thumbnailStatus));
    }

    private async Task RunPublishedFollowUpAsync(
        Func<Task> action,
        IPublishExecutionScope execution,
        PublishCommand command,
        string operation)
    {
        try
        {
            await action();
        }
        catch (Exception exception)
        {
            LogPublishedFollowUpFailure(exception, execution, command, operation);
        }
    }

    private void LogPublishedFollowUpFailure(
        Exception exception,
        IPublishExecutionScope execution,
        PublishCommand command,
        string operation)
    {
        if (exception is OperationCanceledException)
        {
            logger.LogWarning(
                exception,
                "Publication follow-up was canceled while {Operation} for calendar event " +
                "{CalendarEventId} and platform {PlatformId}. Cancellation source: " +
                "{CancellationSource}. The publication remains Published.",
                operation,
                command.CalendarEventId,
                command.PlatformId,
                execution.ClassifyCancellation());
            return;
        }

        logger.LogError(
            exception,
            "Publication follow-up failed while {Operation} for calendar event " +
            "{CalendarEventId} and platform {PlatformId}. The publication remains Published.",
            operation,
            command.CalendarEventId,
            command.PlatformId);
    }

    private void LogProviderFailure(
        PlatformPublishException exception,
        PublishCommand command,
        string attemptId)
    {
        if (exception.FailureKind == PlatformPublishFailureKind.Timeout)
        {
            logger.LogWarning(
                exception,
                "Publishing calendar event {CalendarEventId} to platform {PlatformId} failed. " +
                "Provider failure kind: {FailureKind}. Failure code: {FailureCode}. " +
                "Failure stage: {FailureStage}. Provider status: {ProviderStatus}. " +
                "Publish attempt: {PublishAttemptId}.",
                command.CalendarEventId,
                command.PlatformId,
                exception.FailureKind,
                exception.Failure.Code,
                exception.Failure.Stage,
                exception.Failure.ProviderStatus,
                attemptId);
            return;
        }

        logger.LogError(
            exception,
            "Publishing calendar event {CalendarEventId} to platform {PlatformId} failed. " +
            "Provider failure kind: {FailureKind}. Failure code: {FailureCode}. " +
            "Failure stage: {FailureStage}. Provider status: {ProviderStatus}. " +
            "Publish attempt: {PublishAttemptId}.",
            command.CalendarEventId,
            command.PlatformId,
            exception.FailureKind,
            exception.Failure.Code,
            exception.Failure.Stage,
            exception.Failure.ProviderStatus,
            attemptId);
    }

    private void LogExecutionCancellation(
        OperationCanceledException exception,
        IPublishExecutionScope execution,
        PublishCommand command,
        string operation)
    {
        var source = execution.ClassifyCancellation();
        if (source == PublishCancellationSource.HostShutdown)
        {
            logger.LogWarning(
                exception,
                "Host shutdown canceled publication while {Operation} for calendar event " +
                "{CalendarEventId} and platform {PlatformId}.",
                operation,
                command.CalendarEventId,
                command.PlatformId);
            return;
        }

        logger.LogError(
            exception,
            "Publication was canceled while {Operation} for calendar event {CalendarEventId} " +
            "and platform {PlatformId}. Cancellation source: {CancellationSource}.",
            operation,
            command.CalendarEventId,
            command.PlatformId,
            source);
    }

    private static PublishResultStatus? ValidateExistingPublication(
        PlatformPublication? existing)
    {
        if (existing is null)
        {
            return null;
        }

        if (existing.IsOrphaned)
        {
            return PublishResultStatus.PlatformDeleted;
        }

        return existing.Status switch
        {
            PublishStatus.Published => PublishResultStatus.AlreadyPublished,
            PublishStatus.Publishing => PublishResultStatus.PublishInProgress,
            _ => null
        };
    }

    private async Task<PublishResult> RecordFailedAsync(
        IPublishExecutionScope execution,
        PublishCommand command,
        string attemptId,
        string? externalResourceId,
        PlatformPublishFailure failure)
    {
        var persistedFailure = new PublicationFailure(
            failure.Code,
            failure.Message,
            failure.Stage,
            failure.ProviderStatus,
            failure.ProviderErrorCode,
            failure.RetryAfterUtc,
            timeProvider.GetUtcNow(),
            attemptId,
            failure.VerificationRequired);

        Activity.Current?.SetTag("ytskedy.publish.failure_code", failure.Code);
        Activity.Current?.SetTag("ytskedy.publish.failure_stage", failure.Stage);
        Activity.Current?.SetTag("ytskedy.publish.provider_status", failure.ProviderStatus);

        MarkFailedResult result;
        try
        {
            result = await execution.RunFinalizationAsync(
                token => publicationAttempts.MarkFailedAsync(
                    command.CalendarEventId,
                    command.PlatformId,
                    externalResourceId,
                    persistedFailure,
                    token));
        }
        catch (Exception exception)
        {
            logger.LogCritical(
                exception,
                "Publishing calendar event {CalendarEventId} to platform {PlatformId} could not " +
                "record Failed. External resource id: {ExternalResourceId}.",
                command.CalendarEventId,
                command.PlatformId,
                externalResourceId);
            return PublishResult.ForStatus(PublishResultStatus.FinalizeFailed);
        }

        if (result == MarkFailedResult.Marked)
        {
            return PublishResult.Failed(persistedFailure);
        }

        logger.LogCritical(
            "Publishing calendar event {CalendarEventId} to platform {PlatformId} could not " +
            "record a final publication state. Failed-state result: {MarkFailedResult}. " +
            "External resource id: {ExternalResourceId}.",
            command.CalendarEventId,
            command.PlatformId,
            result,
            externalResourceId);
        return PublishResult.ForStatus(PublishResultStatus.FinalizeFailed);
    }
}
