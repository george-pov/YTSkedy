using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Settings;

public interface IStartDefaultsReader
{
    Task<StartDefaults> GetAsync(CancellationToken cancellationToken);
}
