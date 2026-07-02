using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Settings;

public sealed class UpdateEventTextFieldsHandler(IEventTextFieldsModifier eventTextFields)
{
    public async Task<EventTextFields> HandleAsync(
        UpdateEventTextFieldsCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var normalized = new EventTextFields(command.Fields);

        await eventTextFields.SaveAsync(normalized, cancellationToken);

        return normalized;
    }
}
