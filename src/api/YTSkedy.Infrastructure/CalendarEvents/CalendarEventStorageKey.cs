using System.Globalization;

namespace YTSkedy.Infrastructure.CalendarEvents;

internal static class CalendarEventStorageKey
{
    private const string ScheduledStartRowKeyPrefix = "start-";
    private const string ScheduledStartRowKeyFormat = "yyyyMMdd'T'HHmmss'Z'";

    internal static string NewCalendarEventId(DateTimeOffset scheduledStartUtc) =>
        $"{RowKeyForScheduledStart(scheduledStartUtc)}-{Guid.NewGuid():N}";

    internal static string RowKeyForScheduledStart(DateTimeOffset scheduledStartUtc) =>
        ScheduledStartRowKeyPrefix + scheduledStartUtc.UtcDateTime.ToString(
            ScheduledStartRowKeyFormat,
            CultureInfo.InvariantCulture);

    internal static bool TryGetAddress(
        string calendarEventId,
        out DateTimeOffset scheduledStartUtc,
        out string rowKey)
    {
        scheduledStartUtc = default;
        rowKey = string.Empty;

        var separatorIndex = calendarEventId.LastIndexOf('-');
        if (separatorIndex < 0)
        {
            return false;
        }

        var nonce = calendarEventId[(separatorIndex + 1)..];
        if (!IsLowercaseHexGuid(nonce))
        {
            return false;
        }

        var candidateRowKey = calendarEventId[..separatorIndex];
        if (!TryParseRowKey(candidateRowKey, out scheduledStartUtc))
        {
            return false;
        }

        rowKey = candidateRowKey;
        return true;
    }

    internal static string PartitionFilter(string partitionKey) =>
        $"PartitionKey eq '{EscapeLiteral(partitionKey)}'";

    internal static string EscapeLiteral(string value) =>
        value.Replace("'", "''", StringComparison.Ordinal);

    private static bool TryParseRowKey(
        string rowKey,
        out DateTimeOffset scheduledStartUtc)
    {
        scheduledStartUtc = default;

        if (!rowKey.StartsWith(ScheduledStartRowKeyPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var value = rowKey[ScheduledStartRowKeyPrefix.Length..];
        if (!DateTime.TryParseExact(
                value,
                ScheduledStartRowKeyFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
        {
            return false;
        }

        scheduledStartUtc = new DateTimeOffset(parsed, TimeSpan.Zero);
        return true;
    }

    private static bool IsLowercaseHexGuid(string value)
    {
        if (value.Length != 32)
        {
            return false;
        }

        foreach (var character in value)
        {
            if (character is not (>= '0' and <= '9' or >= 'a' and <= 'f'))
            {
                return false;
            }
        }

        return true;
    }
}
