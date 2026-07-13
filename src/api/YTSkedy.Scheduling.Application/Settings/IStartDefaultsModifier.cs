using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Settings;

public interface IStartDefaultsModifier
{
    Task SaveAsync(StartDefaults startDefaults, CancellationToken cancellationToken);
}
