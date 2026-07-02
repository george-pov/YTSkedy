using System.Text.Json;
using YTSkedy.Infrastructure.CalendarEvents;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Infrastructure.Settings;

internal static class EventTextFieldsSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    internal static string Serialize(EventTextFields eventTextFields)
    {
        ArgumentNullException.ThrowIfNull(eventTextFields);

        var document = new EventTextFieldsJson(
            eventTextFields.Fields
                .Select(EventTextFieldItem.From)
                .ToArray());

        return JsonSerializer.Serialize(document, JsonOptions);
    }

    internal static EventTextFields Deserialize(string valueJson)
    {
        if (string.IsNullOrWhiteSpace(valueJson))
        {
            throw new InvalidOperationException("Stored event text fields JSON is empty.");
        }

        try
        {
            var document = JsonSerializer.Deserialize<EventTextFieldsJson>(
                valueJson,
                JsonOptions);

            if (document is null)
            {
                throw new InvalidOperationException("Stored event text fields JSON is empty.");
            }

            if (document.Fields is null)
            {
                throw new InvalidOperationException(
                    "Stored event text fields JSON must contain fields.");
            }

            return new EventTextFields(
                document.Fields.Select(EventTextFieldItem.ToDomain));
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                "Stored event text fields JSON is invalid.",
                exception);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidOperationException(
                "Stored event text fields value is invalid.",
                exception);
        }
    }

    private sealed record EventTextFieldsJson(
        IReadOnlyList<EventTextFieldItem> Fields);
}
