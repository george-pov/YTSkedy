namespace YTSkedy.Scheduling.Domain.CalendarEvents;

/// <summary>
/// Single place that parses the wire spelling of <see cref="EventTextType"/>.
/// The forward direction is the enum name (for example <c>ShortText</c>);
/// parsing is case-insensitive and trims surrounding whitespace. Callers decide
/// how to react to an unknown value, so this exposes only the boolean primitive.
/// </summary>
public static class EventTextTypeParser
{
    public static bool TryParse(string? value, out EventTextType type)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case "shorttext":
                type = EventTextType.ShortText;
                return true;
            case "longtext":
                type = EventTextType.LongText;
                return true;
            default:
                type = default;
                return false;
        }
    }
}
