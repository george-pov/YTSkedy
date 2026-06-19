namespace YTSkedy.Scheduling.Domain.CalendarEvents;

/// <summary>
/// Language codes that carry behavior in the calendar event domain. English is
/// the required publish language and the Title-column sort key. Matching is
/// case-insensitive, so a description tagged "en", "EN", or "En" is treated
/// identically wherever the English description is selected.
/// </summary>
public static class CalendarEventLanguages
{
    public const string English = "en";

    public static bool IsEnglish(string? language) =>
        string.Equals(language, English, StringComparison.OrdinalIgnoreCase);
}
