using System.Globalization;
using YTSkedy.Scheduling.Application.CalendarEvents;

namespace YTSkedy.Infrastructure.CalendarEvents;

/// <summary>
/// Owns the Azure Table partition-key scheme for calendar events. An event is
/// partitioned by the UTC month of its scheduled start, so <see cref="ForInstant"/>
/// formats the single key used on the write, read, and delete paths, and
/// <see cref="ForLocalMonth"/> returns the set of keys to scan for a requested
/// local month. That set includes the adjacent UTC months because a local-time
/// month boundary can fall into the previous or next UTC month, and it omits any
/// month outside the representable <see cref="DateTime"/> range.
/// </summary>
internal static class CalendarEventPartitionKey
{
    private const string PartitionKeyFormat = "'calendar-events-'yyyyMM";

    internal static string ForInstant(DateTimeOffset scheduledStartUtc) =>
        scheduledStartUtc.UtcDateTime.ToString(
            PartitionKeyFormat,
            CultureInfo.InvariantCulture);

    internal static IReadOnlyList<string> ForLocalMonth(CalendarEventMonthCriteria criteria)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        var requestedMonth = new DateTime(
            criteria.Year,
            criteria.Month,
            1,
            0,
            0,
            0,
            DateTimeKind.Utc);

        var partitionMonths = new List<DateTime>(3);

        if (requestedMonth.Year > DateTime.MinValue.Year || requestedMonth.Month > 1)
        {
            partitionMonths.Add(requestedMonth.AddMonths(-1));
        }

        partitionMonths.Add(requestedMonth);

        if (requestedMonth.Year < DateTime.MaxValue.Year || requestedMonth.Month < 12)
        {
            partitionMonths.Add(requestedMonth.AddMonths(1));
        }

        return partitionMonths
            .Select(month => ForInstant(new DateTimeOffset(month, TimeSpan.Zero)))
            .ToArray();
    }
}
