using Azure;
using Azure.Data.Tables;
using System.Globalization;
using System.Text.Json;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Infrastructure.CalendarEvents;

public sealed class AzureCalendarEventRepository(TableClient tableClient) :
    ICalendarEventRepository,
    ICalendarEventReader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<string> CreateAsync(
        CalendarEvent calendarEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(calendarEvent);

        var scheduledStartUtc = ToUtc(calendarEvent.Start);
        var calendarEventId = FormatCalendarEventId(scheduledStartUtc);
        var entity = new CalendarEventEntity
        {
            PartitionKey = GetPartitionKey(scheduledStartUtc),
            RowKey = calendarEventId,
            CalendarEventId = calendarEventId,
            ScheduledStartUtc = scheduledStartUtc,
            LocalDateTime = FormatLocalDateTime(calendarEvent.Start.LocalDateTime),
            TimeZoneId = calendarEvent.Start.TimeZoneId,
            DescriptionsJson = JsonSerializer.Serialize(calendarEvent.Descriptions, JsonOptions),
            Status = calendarEvent.Status.ToString(),
            CreatedUtc = DateTimeOffset.UtcNow
        };

        await tableClient.CreateIfNotExistsAsync(cancellationToken);

        try
        {
            await tableClient.AddEntityAsync(entity, cancellationToken);
        }
        catch (RequestFailedException exception) when (exception.Status == 409)
        {
            throw new InvalidOperationException(
                $"Calendar event '{calendarEventId}' already exists.",
                exception);
        }

        return calendarEventId;
    }

    public async Task<IReadOnlyList<CalendarEventListItem>> ListByMonthAsync(
        CalendarEventMonthCriteria criteria,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        var entities = new List<CalendarEventEntity>();

        foreach (var partitionKey in CalendarEventReadMapper.GetPartitionKeysForLocalMonth(criteria))
        {
            var filter = $"PartitionKey eq '{partitionKey}'";

            try
            {
                await foreach (var entity in tableClient.QueryAsync<CalendarEventEntity>(
                    filter,
                    cancellationToken: cancellationToken))
                {
                    entities.Add(entity);
                }
            }
            catch (RequestFailedException exception) when (exception.Status == 404)
            {
                return [];
            }
        }

        return CalendarEventReadMapper.ToListItemsForMonth(
            entities,
            criteria);
    }

    public async Task<CalendarEventDetail?> GetByIdAsync(
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);

        if (!TryParseScheduledStartUtc(calendarEventId, out var scheduledStartUtc))
        {
            return null;
        }

        try
        {
            var response = await tableClient.GetEntityAsync<CalendarEventEntity>(
                GetPartitionKey(scheduledStartUtc),
                calendarEventId,
                cancellationToken: cancellationToken);

            return CalendarEventReadMapper.ToDetail(response.Value);
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return null;
        }
    }

    public async Task UpdateStatusAsync(
        string calendarEventId,
        CalendarEventStatus status,
        string youTubeBroadcastId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);
        ArgumentException.ThrowIfNullOrWhiteSpace(youTubeBroadcastId);

        if (!TryParseScheduledStartUtc(calendarEventId, out var scheduledStartUtc))
        {
            throw new InvalidOperationException(
                $"Calendar event id '{calendarEventId}' is not a valid identifier.");
        }

        var response = await tableClient.GetEntityAsync<CalendarEventEntity>(
            GetPartitionKey(scheduledStartUtc),
            calendarEventId,
            cancellationToken: cancellationToken);

        var entity = response.Value;
        entity.Status = status.ToString();
        entity.YouTubeLink = youTubeBroadcastId;

        await tableClient.UpdateEntityAsync(
            entity,
            entity.ETag,
            TableUpdateMode.Replace,
            cancellationToken);
    }

    private static DateTimeOffset ToUtc(ScheduledStart start)
    {
        var localDateTime = DateTime.SpecifyKind(
            start.LocalDateTime,
            DateTimeKind.Unspecified);
        var timeZone = FindTimeZone(start.TimeZoneId);

        if (timeZone.IsInvalidTime(localDateTime))
        {
            throw new InvalidOperationException(
                "Scheduled start time does not exist in the specified time zone.");
        }

        if (timeZone.IsAmbiguousTime(localDateTime))
        {
            throw new InvalidOperationException(
                "Scheduled start time is ambiguous in the specified time zone.");
        }

        var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(localDateTime, timeZone);

        return new DateTimeOffset(utcDateTime, TimeSpan.Zero);
    }

    private static TimeZoneInfo FindTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
            when (TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZoneId, out var windowsTimeZoneId))
        {
            return TimeZoneInfo.FindSystemTimeZoneById(windowsTimeZoneId);
        }
    }

    private static string FormatCalendarEventId(DateTimeOffset scheduledStartUtc) =>
        scheduledStartUtc.UtcDateTime.ToString(
            "yyyyMMdd'T'HHmmss'Z'",
            CultureInfo.InvariantCulture);

    private static bool TryParseScheduledStartUtc(
        string calendarEventId,
        out DateTimeOffset scheduledStartUtc)
    {
        if (DateTime.TryParseExact(
                calendarEventId,
                "yyyyMMdd'T'HHmmss'Z'",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
        {
            scheduledStartUtc = new DateTimeOffset(parsed, TimeSpan.Zero);
            return true;
        }

        scheduledStartUtc = default;
        return false;
    }

    internal static string GetPartitionKey(DateTimeOffset scheduledStartUtc) =>
        scheduledStartUtc.UtcDateTime.ToString(
            "'calendar-events-'yyyyMM",
            CultureInfo.InvariantCulture);

    private static string FormatLocalDateTime(DateTime localDateTime) =>
        localDateTime.ToString(
            "yyyy-MM-dd'T'HH:mm:ss",
            CultureInfo.InvariantCulture);
}
