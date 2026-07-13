using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Settings;

public sealed class GetStartDefaultsHandler(IStartDefaultsReader startDefaults)
{
    public Task<StartDefaults> HandleAsync(CancellationToken cancellationToken) =>
        startDefaults.GetAsync(cancellationToken);
}
