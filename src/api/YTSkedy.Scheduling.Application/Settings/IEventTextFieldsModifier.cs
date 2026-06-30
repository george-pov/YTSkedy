using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Settings;

public interface IEventTextFieldsModifier
{
    Task SaveAsync(EventTextFields eventTextFields, CancellationToken cancellationToken);
}
