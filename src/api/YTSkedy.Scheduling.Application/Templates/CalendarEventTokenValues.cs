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
    private static readonly CultureInfo FrenchCulture = CultureInfo.GetCultureInfo("fr-FR");

    private CalendarEventTokenValues(IReadOnlyDictionary<string, string> values)
    {
        Values = values;
    }

    public IReadOnlyDictionary<string, string> Values { get; }

    public static CalendarEventTokenValues From(CalendarEventView calendarEvent)
    {
        ArgumentNullException.ThrowIfNull(calendarEvent);

        var localDate = calendarEvent.Start.LocalDateTime;
        var values = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var field in calendarEvent.Text.Fields)
        {
            values[field.FieldKey] = calendarEvent.Text.ValueFor(field.FieldKey) ?? string.Empty;
        }

        values["longDateEn"] = localDate.ToString("MMMM d, yyyy", EnglishCulture);
        values["shortDateEn"] = localDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        values["longDateRu"] = localDate.ToString("d MMMM yyyy", RussianCulture);
        values["shortDateRu"] = localDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        values["longDateFr"] = localDate.ToString("d MMMM yyyy", FrenchCulture);
        values["shortDateFr"] = localDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

        return new CalendarEventTokenValues(values);
    }
}
