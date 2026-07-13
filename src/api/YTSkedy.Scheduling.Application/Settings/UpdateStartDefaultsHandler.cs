using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Settings;

public sealed class UpdateStartDefaultsHandler(IStartDefaultsModifier startDefaults)
{
    public async Task<StartDefaults> HandleAsync(
        UpdateStartDefaultsCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var replacement = new StartDefaults(
            command.DayOfWeek,
            command.LocalTime,
            command.TimeZoneId);

        await startDefaults.SaveAsync(replacement, cancellationToken);
        return replacement;
    }
}
