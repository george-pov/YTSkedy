using System.Text.Json;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Infrastructure.Settings;

internal static class EventTextFieldsSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    internal static string Serialize(EventTextFields eventTextFields)
    {
        ArgumentNullException.ThrowIfNull(eventTextFields);

        var normalized = EventTextFields.Normalize(eventTextFields.Fields);
        var document = new EventTextFieldsDocument(
            normalized.Fields
                .Select(field => new EventTextFieldDocument(
                    field.FieldKey,
                    field.Label,
                    field.Type.ToString(),
                    field.MaxLength))
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
            var document = JsonSerializer.Deserialize<EventTextFieldsDocument>(
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

            return EventTextFields.Normalize(
                document.Fields.Select(ToDomainField));
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

    private static EventTextType ParseType(string? type) =>
        type?.ToLowerInvariant() switch
        {
            "shorttext" => EventTextType.ShortText,
            "longtext" => EventTextType.LongText,
            _ => throw new ArgumentException(
                $"Event text type '{type ?? "<null>"}' is invalid.",
                nameof(type))
        };

    private static EventTextField ToDomainField(EventTextFieldDocument field)
    {
        if (field is null)
        {
            throw new InvalidOperationException(
                "Stored event text fields JSON cannot contain null fields.");
        }

        return new EventTextField(
            field.FieldKey ?? string.Empty,
            field.Label ?? string.Empty,
            ParseType(field.Type),
            field.MaxLength);
    }

    private sealed record EventTextFieldsDocument(
        IReadOnlyList<EventTextFieldDocument> Fields);

    private sealed record EventTextFieldDocument(
        string? FieldKey,
        string? Label,
        string? Type,
        int MaxLength);
}
