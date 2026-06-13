using System.Globalization;
using System.Text.Json;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Infrastructure.CalendarEvents;

internal static class CalendarEventReadMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IReadOnlyList<CalendarEventListItem> ToListItemsForMonth(
        IEnumerable<CalendarEventEntity> entities,
        CalendarEventMonthCriteria criteria)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(criteria);

        return entities
            .Where(entity => IsInLocalMonth(entity, criteria))
            .OrderBy(entity => entity.ScheduledStartUtc)
            .ThenBy(entity => entity.CalendarEventId, StringComparer.Ordinal)
            .Select(ToListItem)
            .ToArray();
    }

    public static IReadOnlyList<string> GetPartitionKeysForLocalMonth(
        CalendarEventMonthCriteria criteria)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        var requestedMonth = new DateTime(
            criteria.Year,
            criteria.Month,
            1,
            0,
            0,
            0,
            DateTimeKind.Utc);

        var partitionMonths = new List<DateTime>(3);

        if (requestedMonth.Year > DateTime.MinValue.Year || requestedMonth.Month > 1)
        {
            partitionMonths.Add(requestedMonth.AddMonths(-1));
        }

        partitionMonths.Add(requestedMonth);

        if (requestedMonth.Year < DateTime.MaxValue.Year || requestedMonth.Month < 12)
        {
            partitionMonths.Add(requestedMonth.AddMonths(1));
        }

        return partitionMonths
            .Select(ToPartitionKey)
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

    private static CalendarEventListItem ToListItem(CalendarEventEntity entity)
    {
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

        return new CalendarEventListItem(
            entity.CalendarEventId,
            new ScheduledStart(
                localDateTime,
                entity.TimeZoneId),
            DeserializeDescriptions(entity),
            ParseStatus(entity.Status));
    }

    public static CalendarEventDetail ToDetail(CalendarEventEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new CalendarEventDetail(
            entity.CalendarEventId,
            entity.ScheduledStartUtc,
            DeserializeDescriptions(entity),
            ParseStatus(entity.Status));
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

    private static CalendarEventStatus ParseStatus(string? status) =>
        Enum.TryParse<CalendarEventStatus>(status, ignoreCase: true, out var parsed)
            ? parsed
            : CalendarEventStatus.Draft;

    private static string ToPartitionKey(DateTime utcMonth) =>
        AzureCalendarEventRepository.GetPartitionKey(
            new DateTimeOffset(utcMonth, TimeSpan.Zero));
}
