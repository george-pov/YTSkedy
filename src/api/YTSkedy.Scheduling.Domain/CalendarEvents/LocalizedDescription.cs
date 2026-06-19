namespace YTSkedy.Scheduling.Domain.CalendarEvents;

public sealed record LocalizedDescription(
    string Language,
    string Title,
    string? Description)
{
    /// <summary>
    /// True when this is the English description. English matching is
    /// case-insensitive, so "en", "EN", and "En" all qualify. See
    /// <see cref="CalendarEventLanguages"/> for the single source of the code
    /// and comparison.
    /// </summary>
    public bool IsEnglish => CalendarEventLanguages.IsEnglish(Language);
}
