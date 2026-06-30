using System.Globalization;
using System.Text.Json;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Infrastructure.CalendarEvents;

internal static class CalendarEventViewMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    internal static IReadOnlyList<CalendarEventView> ToViewsForMonth(
        IEnumerable<CalendarEventEntity> entities,
        CalendarEventMonthCriteria criteria)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(criteria);

        return entities
            .Where(entity => IsInLocalMonth(entity, criteria))
            .OrderBy(entity => entity.ScheduledStartUtc)
            .ThenBy(entity => entity.CalendarEventId, StringComparer.Ordinal)
            .Select(ToView)
            .ToArray();
    }

    internal static IReadOnlyList<CalendarEventView> ToViews(
        IEnumerable<CalendarEventEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        return entities
            .Select(ToView)
            .ToArray();
    }

    private static bool IsInLocalMonth(
        CalendarEventEntity entity,
        CalendarEventMonthCriteria criteria)
    {
        var localMonthPrefix = string.Create(
            CultureInfo.InvariantCulture,
            $"{criteria.Year:0000}-{criteria.Month:00}");

        return entity.LocalDateTime.StartsWith(
            localMonthPrefix,
            StringComparison.Ordinal);
    }

    internal static CalendarEventView ToView(CalendarEventEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (!DateTime.TryParseExact(
                entity.LocalDateTime,
                "yyyy-MM-dd'T'HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var localDateTime))
        {
            throw new InvalidOperationException(
                $"Calendar event '{entity.CalendarEventId}' has invalid local date-time.");
        }

        return new CalendarEventView(
            entity.CalendarEventId,
            new ScheduledStart(
                localDateTime,
                entity.TimeZoneId),
            entity.ScheduledStartUtc,
            DeserializeText(entity));
    }

    internal static string SerializeText(EventTextSnapshot text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var document = new EventTextSnapshotDocument(
            text.Fields
                .Select(field => new EventTextFieldDocument(
                    field.FieldKey,
                    field.Label,
                    field.Type.ToString(),
                    field.MaxLength))
                .ToArray(),
            text.Values
                .Select(value => new EventTextValueDocument(
                    value.FieldKey,
                    value.Value))
                .ToArray());

        return JsonSerializer.Serialize(document, JsonOptions);
    }

    private static EventTextSnapshot DeserializeText(CalendarEventEntity entity)
    {
        try
        {
            var document = JsonSerializer.Deserialize<EventTextSnapshotDocument>(
                entity.TextJson,
                JsonOptions) ?? throw new InvalidOperationException(
                $"Calendar event '{entity.CalendarEventId}' has missing text JSON.");

            if (document.Fields is null || document.Values is null)
            {
                throw new InvalidOperationException(
                    $"Calendar event '{entity.CalendarEventId}' has incomplete text JSON.");
            }

            return new EventTextSnapshot(
                document.Fields.Select(ToDomainField).ToArray(),
                document.Values.Select(ToDomainValue).ToArray());
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                $"Calendar event '{entity.CalendarEventId}' has malformed text JSON.",
                exception);
        }
    }

    private static EventTextField ToDomainField(EventTextFieldDocument field)
    {
        if (field is null)
        {
            throw new InvalidOperationException(
                "Stored calendar event text JSON cannot contain null fields.");
        }

        return new EventTextField(
            field.FieldKey ?? string.Empty,
            field.Label ?? string.Empty,
            ParseType(field.Type),
            field.MaxLength);
    }

    private static EventTextValue ToDomainValue(EventTextValueDocument value)
    {
        if (value is null)
        {
            throw new InvalidOperationException(
                "Stored calendar event text JSON cannot contain null values.");
        }

        return new EventTextValue(
            value.FieldKey ?? string.Empty,
            value.Value ?? string.Empty);
    }

    private static EventTextType ParseType(string? type) =>
        type?.ToLowerInvariant() switch
        {
            "shorttext" => EventTextType.ShortText,
            "longtext" => EventTextType.LongText,
            _ => throw new InvalidOperationException(
                $"Stored event text type value '{type ?? "<null>"}' is invalid.")
        };

    private sealed record EventTextSnapshotDocument(
        IReadOnlyList<EventTextFieldDocument> Fields,
        IReadOnlyList<EventTextValueDocument> Values);

    private sealed record EventTextFieldDocument(
        string? FieldKey,
        string? Label,
        string? Type,
        int MaxLength);

    private sealed record EventTextValueDocument(
        string? FieldKey,
        string? Value);
}
