namespace YTSkedy.Infrastructure.Platforms;

/// <summary>
/// Owns the Azure Table key scheme for platform publications. Every publication
/// for one calendar event shares the partition <c>event-{calendarEventId}</c>,
/// and the row key <c>platform-{platformId}</c> identifies the platform, so an
/// event/platform pair addresses exactly one row and all rows for an event read
/// from a single partition. <see cref="EscapeLiteral"/> hardens any value that
/// must be embedded in an OData filter against quote injection.
/// </summary>
internal static class PlatformPublicationKey
{
    internal static string PartitionKeyFor(string calendarEventId) =>
        $"event-{calendarEventId}";

    internal static string RowKeyFor(string platformId) =>
        $"platform-{platformId}";

    /// <summary>
    /// Escapes a string literal for use inside an OData filter by doubling single
    /// quotes. Calendar event ids reach this from the request route, so escaping
    /// is defense in depth even though the listing path validates the event
    /// first.
    /// </summary>
    internal static string EscapeLiteral(string value) =>
        value.Replace("'", "''", StringComparison.Ordinal);
}
