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
            DeserializeDescriptions(entity));
    }

    private static LocalizedDescription[] DeserializeDescriptions(CalendarEventEntity entity)
    {
        try
        {
            return JsonSerializer.Deserialize<LocalizedDescription[]>(
                entity.DescriptionsJson,
                JsonOptions) ?? throw new InvalidOperationException(
                $"Calendar event '{entity.CalendarEventId}' has missing descriptions JSON.");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                $"Calendar event '{entity.CalendarEventId}' has malformed descriptions JSON.",
                exception);
        }
    }

}
