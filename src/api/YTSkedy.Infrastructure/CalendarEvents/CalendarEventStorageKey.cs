namespace YTSkedy.Infrastructure.CalendarEvents;

internal static class CalendarEventStorageKey
{
    internal const string PartitionKey = "calendar-events";
    private const string RowKeyPrefix = "event-";

    internal static string NewCalendarEventId() => $"{Guid.NewGuid():N}";

    internal static string RowKeyFor(string calendarEventId) =>
        RowKeyPrefix + calendarEventId;

    internal static bool TryGetAddress(
        string calendarEventId,
        out string partitionKey,
        out string rowKey)
    {
        partitionKey = string.Empty;
        rowKey = string.Empty;

        if (!IsLowercaseHexGuid(calendarEventId))
        {
            return false;
        }

        partitionKey = PartitionKey;
        rowKey = RowKeyFor(calendarEventId);
        return true;
    }

    internal static string PartitionFilter() =>
        $"PartitionKey eq '{EscapeLiteral(PartitionKey)}'";

    internal static string EscapeLiteral(string value) =>
        value.Replace("'", "''", StringComparison.Ordinal);

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
