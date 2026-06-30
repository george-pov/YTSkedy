using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Settings;

public interface IEventTextFieldsReader
{
    Task<EventTextFields> GetAsync(CancellationToken cancellationToken);
}
