using System.Globalization;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Templates;

/// <summary>
/// Builds the token values available when rendering template content from a
/// persisted calendar event.
/// </summary>
public sealed class CalendarEventTokenValues
{
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

        var localDate = calendarEvent.Start.LocalDateTime;

        return new CalendarEventTokenValues(
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["title"] = calendarEvent.Text.ValueFor("text1") ?? string.Empty,
                ["description"] = calendarEvent.Text.ValueFor("text2") ?? string.Empty,
                ["titleRu"] = calendarEvent.Text.ValueFor("text3") ?? string.Empty,
                ["descriptionRu"] = calendarEvent.Text.ValueFor("text4") ?? string.Empty,
                ["longDate"] = localDate.ToString("MMMM d, yyyy", EnglishCulture),
                ["longDateRu"] = localDate.ToString("d MMMM yyyy", RussianCulture),
                ["shortDate"] = localDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["shortDateRu"] = localDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)
            });
    }
}
