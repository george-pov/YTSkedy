using System.Globalization;
using System.Text.Json;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Infrastructure.CalendarEvents;

internal static class CalendarEventViewMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    internal static IReadOnlyList<CalendarEventListRecord> ToListRecordsForMonth(
        IEnumerable<CalendarEventEntity> entities,
        CalendarEventMonthCriteria criteria)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(criteria);

        return entities
            .Where(entity => IsInLocalMonth(entity, criteria))
            .OrderBy(entity => entity.ScheduledStartUtc)
            .ThenBy(entity => entity.CalendarEventId, StringComparer.Ordinal)
            .Select(ToListRecord)
            .ToArray();
    }

    internal static IReadOnlyList<CalendarEventListRecord> ToListRecords(
        IEnumerable<CalendarEventEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        return entities
            .Select(ToListRecord)
            .ToArray();
    }

    private static CalendarEventListRecord ToListRecord(CalendarEventEntity entity) =>
        new(
            ToView(entity),
            PublishedPlatformIdsJson.Deserialize(
                entity.PublishedPlatformIdsJson,
                entity.CalendarEventId));

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

        var document = new EventTextSnapshotJson(
            text.Fields
                .Select(EventTextFieldItem.From)
                .ToArray(),
            text.Values
                .Select(value => new EventTextValueItem(
                    value.FieldKey,
                    value.Value))
                .ToArray());

        return JsonSerializer.Serialize(document, JsonOptions);
    }

    private static EventTextSnapshot DeserializeText(CalendarEventEntity entity)
    {
        try
        {
            var document = JsonSerializer.Deserialize<EventTextSnapshotJson>(
                entity.TextJson,
                JsonOptions) ?? throw new InvalidOperationException(
                $"Calendar event '{entity.CalendarEventId}' has missing text JSON.");

            if (document.Fields is null || document.Values is null)
            {
                throw new InvalidOperationException(
                    $"Calendar event '{entity.CalendarEventId}' has incomplete text JSON.");
            }

            return new EventTextSnapshot(
                document.Fields.Select(EventTextFieldItem.ToDomain).ToArray(),
                document.Values.Select(ToDomainValue).ToArray());
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException)
        {
            throw new InvalidOperationException(
                $"Calendar event '{entity.CalendarEventId}' has malformed text JSON.",
                exception);
        }
    }

    private static EventTextValue ToDomainValue(EventTextValueItem value)
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

    private sealed record EventTextSnapshotJson(
        IReadOnlyList<EventTextFieldItem> Fields,
        IReadOnlyList<EventTextValueItem> Values);

    private sealed record EventTextValueItem(
        string? FieldKey,
        string? Value);
}
