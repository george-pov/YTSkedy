using YTSkedy.Scheduling.Application.YouTube;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.CalendarEvents;

/// <summary>
/// Publishes a single draft calendar event as a scheduled YouTube live
/// broadcast. Guards run before any external call: a missing event, an
/// already-published event, a past start instant, or a missing English
/// description short-circuit without contacting YouTube. The event is marked
/// published only after the broadcast is created.
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

        var broadcastId = await broadcastPublisher.PublishAsync(
            new YouTubeBroadcastRequest(
                englishDescription.Title,
                englishDescription.Description,
                calendarEvent.ScheduledStartUtc),
            cancellationToken);

        await calendarEventRepository.UpdateStatusAsync(
            calendarEventId,
            CalendarEventStatus.Published,
            broadcastId,
            cancellationToken);

        return PublishCalendarEventResult.Published(broadcastId);
    }
}
