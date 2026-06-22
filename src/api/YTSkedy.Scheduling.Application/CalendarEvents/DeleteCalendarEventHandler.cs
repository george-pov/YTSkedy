namespace YTSkedy.Scheduling.Application.CalendarEvents;

public sealed class DeleteCalendarEventHandler(
    ICalendarEventReader calendarEventReader,
    ICalendarEventRepository calendarEventRepository)
{
    public async Task<DeleteCalendarEventResult> HandleAsync(
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);

        // One application read decides eligibility and supplies the broadcast id.
        // No fresh snapshot is taken before the YouTube call, and the local row
        // is deleted by id afterwards without re-reading status.
        var calendarEvent = await calendarEventReader.GetByIdAsync(
            calendarEventId,
            cancellationToken);

        if (calendarEvent is null)
        {
            return DeleteCalendarEventResult.NotFound;
        }

        await calendarEventRepository.DeleteAsync(calendarEventId, cancellationToken);

        return DeleteCalendarEventResult.Deleted;
    }
}
