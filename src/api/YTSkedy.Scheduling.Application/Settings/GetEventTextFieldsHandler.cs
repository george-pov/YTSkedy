using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Settings;

public sealed class GetEventTextFieldsHandler(IEventTextFieldsReader eventTextFields)
{
    public async Task<EventTextFields> HandleAsync(CancellationToken cancellationToken) =>
        await eventTextFields.GetAsync(cancellationToken);
}
