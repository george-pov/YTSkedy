using System.Globalization;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Templates;

/// <summary>
/// Builds the token values available when rendering template content from a
/// persisted calendar event.
/// </summary>
public sealed class CalendarEventTokenValues
{
    private const string Russian = "ru";

    private static readonly CultureInfo EnglishCulture = CultureInfo.GetCultureInfo("en-US");
    private static readonly CultureInfo RussianCulture = CultureInfo.GetCultureInfo("ru-RU");

    private CalendarEventTokenValues(IReadOnlyDictionary<string, string> values)
    {
        Values = values;
    }

    public IReadOnlyDictionary<string, string> Values { get; }

    public static CalendarEventTokenValues From(CalendarEventView calendarEvent)
    {
        ArgumentNullException.ThrowIfNull(calendarEvent);

        var english = calendarEvent.Descriptions.FirstOrDefault(description => description.IsEnglish);
        var russian = calendarEvent.Descriptions.FirstOrDefault(IsRussian);
        var localDate = calendarEvent.Start.LocalDateTime;

        return new CalendarEventTokenValues(
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["title"] = english?.Title ?? string.Empty,
                ["description"] = english?.Description ?? string.Empty,
                ["titleRu"] = russian?.Title ?? string.Empty,
                ["descriptionRu"] = russian?.Description ?? string.Empty,
                ["longDate"] = localDate.ToString("MMMM d, yyyy", EnglishCulture),
                ["longDateRu"] = localDate.ToString("d MMMM yyyy", RussianCulture),
                ["shortDate"] = localDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["shortDateRu"] = localDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)
            });
    }

    private static bool IsRussian(LocalizedDescription description) =>
        string.Equals(description.Language, Russian, StringComparison.OrdinalIgnoreCase);
}
