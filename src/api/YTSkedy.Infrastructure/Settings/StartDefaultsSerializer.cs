using System.Globalization;
using System.Text.Json;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Infrastructure.Settings;

internal static class StartDefaultsSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    internal static string Serialize(StartDefaults startDefaults)
    {
        ArgumentNullException.ThrowIfNull(startDefaults);

        return JsonSerializer.Serialize(
            new StartDefaultsJson(
                startDefaults.DayOfWeek?.ToString(),
                startDefaults.LocalTime?.ToString("HH:mm", CultureInfo.InvariantCulture),
                startDefaults.TimeZoneId),
            JsonOptions);
    }

    internal static StartDefaults Deserialize(string valueJson)
    {
        if (string.IsNullOrWhiteSpace(valueJson))
        {
            throw new InvalidOperationException("Stored start defaults JSON is empty.");
        }

        try
        {
            var document = JsonSerializer.Deserialize<StartDefaultsJson>(valueJson, JsonOptions)
                ?? throw new InvalidOperationException("Stored start defaults JSON is empty.");

            return new StartDefaults(
                ParseDayOfWeek(document.DayOfWeek),
                ParseLocalTime(document.LocalTime),
                document.TimeZoneId);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("Stored start defaults JSON is invalid.", exception);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidOperationException("Stored start defaults value is invalid.", exception);
        }
    }

    private static DayOfWeek? ParseDayOfWeek(string? value)
    {
        if (value is null)
        {
            return null;
        }

        if (!Enum.TryParse<DayOfWeek>(value, ignoreCase: false, out var result) ||
            !Enum.IsDefined(result))
        {
            throw new ArgumentException("Stored weekday is invalid.", nameof(value));
        }

        return result;
    }

    private static TimeOnly? ParseLocalTime(string? value)
    {
        if (value is null)
        {
            return null;
        }

        if (!TimeOnly.TryParseExact(
                value,
                "HH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var result))
        {
            throw new ArgumentException("Stored local time is invalid.", nameof(value));
        }

        return result;
    }

    private sealed record StartDefaultsJson(
        string? DayOfWeek,
        string? LocalTime,
        string? TimeZoneId);
}
