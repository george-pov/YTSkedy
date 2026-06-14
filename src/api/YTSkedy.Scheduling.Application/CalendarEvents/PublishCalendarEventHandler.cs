using YTSkedy.Scheduling.Application.YouTube;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.CalendarEvents;

/// <summary>
/// Publishes a single draft calendar event as a scheduled YouTube live
/// broadcast. Guards run before any external call: a missing event, an
/// already-published event, a past start instant, or a missing English
/// description short-circuit without contacting YouTube. The event is reserved
/// (Draft to Publishing) before the broadcast call so concurrent publishes
/// cannot create duplicate broadcasts, and it is marked Published only after
/// the broadcast is created. A failed broadcast releases the reservation back
/// to Draft.
/// </summary>
public sealed class PublishCalendarEventHandler(
    ICalendarEventReader calendarEventReader,
    ICalendarEventRepository calendarEventRepository,
    IYouTubeBroadcastPublisher broadcastPublisher,
    TimeProvider timeProvider)
{
    private const string EnglishLanguage = "en";

    public async Task<PublishCalendarEventResult> HandleAsync(
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);

        var calendarEvent = await calendarEventReader.GetByIdAsync(
            calendarEventId,
            cancellationToken);

        if (calendarEvent is null)
        {
            return PublishCalendarEventResult.NotFound();
        }

        if (calendarEvent.Status == CalendarEventStatus.Published)
        {
            return PublishCalendarEventResult.AlreadyPublished();
        }

        if (calendarEvent.ScheduledStartUtc <= timeProvider.GetUtcNow())
        {
            return PublishCalendarEventResult.StartInPast();
        }

        var englishDescription = calendarEvent.Descriptions.FirstOrDefault(description =>
            string.Equals(description.Language, EnglishLanguage, StringComparison.OrdinalIgnoreCase));

        if (englishDescription is null)
        {
            return PublishCalendarEventResult.MissingEnglishDescription();
        }

        // Reserve the publish transition before any external call. A concurrent
        // request that already moved the event out of Draft loses the race here,
        // so YouTube is never asked to create a duplicate broadcast. A failed
        // reservation is reported as already published: another request owns the
        // publish and retrying would not help.
        if (!await calendarEventRepository.TryReserveForPublishingAsync(
                calendarEventId,
                cancellationToken))
        {
            return PublishCalendarEventResult.AlreadyPublished();
        }

        string broadcastId;

        try
        {
            broadcastId = await broadcastPublisher.PublishAsync(
                new YouTubeBroadcastRequest(
                    englishDescription.Title,
                    englishDescription.Description,
                    calendarEvent.ScheduledStartUtc),
                cancellationToken);
        }
        catch
        {
            // The broadcast was not created. Release the reservation so the
            // event returns to Draft and stays retryable. Use an uncancelled
            // token so the compensation still runs when the request itself was
            // canceled.
            await calendarEventRepository.ReleaseReservationAsync(
                calendarEventId,
                CancellationToken.None);

            throw;
        }

        await calendarEventRepository.MarkPublishedAsync(
            calendarEventId,
            broadcastId,
            cancellationToken);

        return PublishCalendarEventResult.Published(broadcastId);
    }
}
