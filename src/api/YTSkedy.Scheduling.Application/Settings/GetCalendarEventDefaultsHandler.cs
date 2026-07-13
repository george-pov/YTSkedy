namespace YTSkedy.Scheduling.Application.Settings;

public sealed class GetCalendarEventDefaultsHandler(
    IEventTextFieldsReader eventTextFields,
    IStartDefaultsReader startDefaults)
{
    public async Task<CalendarEventDefaults> HandleAsync(
        CancellationToken cancellationToken)
    {
        var fields = await eventTextFields.GetAsync(cancellationToken);
        var defaults = await startDefaults.GetAsync(cancellationToken);

        return new CalendarEventDefaults(fields, defaults);
    }
}
