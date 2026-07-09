using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms.Publications;

/// <summary>
/// Owns the row-based rule that current platform-publication rows block
/// calendar-event and thumbnail mutation.
/// </summary>
public sealed class CalendarEventPublicationLock(IPlatformPublicationReader publications)
{
    public async Task<bool> IsLockedAsync(
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);

        return await publications.HasAnyForEventAsync(calendarEventId, cancellationToken);
    }

    public static bool IsLocked(IReadOnlyCollection<PlatformPublication> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        return rows.Count > 0;
    }
}
