using YTSkedy.Scheduling.Application.Platforms;

namespace YTSkedy.Scheduling.Application.CalendarEvents;

public sealed class DeleteCalendarEventHandler(
    ICalendarEventReader calendarEventReader,
    IPlatformPublicationReader publicationReader,
    ICalendarEventModifier calendarEventModifier)
{
    public async Task<DeleteCalendarEventResult> HandleAsync(
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);

        var calendarEvent = await calendarEventReader.GetByIdAsync(
            calendarEventId,
            cancellationToken);

        if (calendarEvent is null)
        {
            return DeleteCalendarEventResult.NotFound;
        }

        var publicationRows = await publicationReader.ListByEventAsync(
            calendarEventId,
            cancellationToken);
        if (publicationRows.Count > 0)
        {
            return DeleteCalendarEventResult.HasPlatformPublications;
        }

        await calendarEventModifier.DeleteAsync(calendarEventId, cancellationToken);

        return DeleteCalendarEventResult.Deleted;
    }
}
