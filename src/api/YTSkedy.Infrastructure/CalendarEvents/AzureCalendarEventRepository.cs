using Azure;
using Azure.Data.Tables;
using System.Globalization;
using System.Text.Json;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Infrastructure.CalendarEvents;

public sealed class AzureCalendarEventRepository(
    TableClient tableClient,
    TimeProvider timeProvider) :
    ICalendarEventRepository,
    ICalendarEventReader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private const string CalendarEventIdFormat = "yyyyMMdd'T'HHmmss'Z'";

    public async Task<string> CreateAsync(
        CalendarEvent calendarEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(calendarEvent);

        var scheduledStartUtc = ToUtc(calendarEvent.Start);
        var calendarEventId = FormatCalendarEventId(scheduledStartUtc);
        var entity = new CalendarEventEntity
        {
            PartitionKey = CalendarEventPartitionKey.ForInstant(scheduledStartUtc),
            RowKey = calendarEventId,
            CalendarEventId = calendarEventId,
            ScheduledStartUtc = scheduledStartUtc,
            LocalDateTime = FormatLocalDateTime(calendarEvent.Start.LocalDateTime),
            TimeZoneId = calendarEvent.Start.TimeZoneId,
            DescriptionsJson = JsonSerializer.Serialize(calendarEvent.Descriptions, JsonOptions),
            CreatedUtc = timeProvider.GetUtcNow()
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

    public async Task<IReadOnlyList<CalendarEventView>> ListAsync(
        CalendarEventMonthCriteria? criteria,
        CancellationToken cancellationToken) =>
        criteria is null
            ? await ListAllAsync(cancellationToken)
            : await ListByMonthAsync(criteria, cancellationToken);

    private async Task<IReadOnlyList<CalendarEventView>> ListByMonthAsync(
        CalendarEventMonthCriteria criteria,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        var entities = new List<CalendarEventEntity>();

        foreach (var partitionKey in CalendarEventPartitionKey.ForLocalMonth(criteria))
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

        return CalendarEventViewMapper.ToViewsForMonth(
            entities,
            criteria);
    }

    private async Task<IReadOnlyList<CalendarEventView>> ListAllAsync(
        CancellationToken cancellationToken)
    {
        var entities = new List<CalendarEventEntity>();

        try
        {
            await foreach (var entity in tableClient.QueryAsync<CalendarEventEntity>(
                filter: (string?)null,
                cancellationToken: cancellationToken))
            {
                entities.Add(entity);
            }
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return [];
        }

        return CalendarEventViewMapper.ToViews(entities);
    }

    public async Task<CalendarEventView?> GetByIdAsync(
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);

        var entity = await TryGetEntityAsync(calendarEventId, cancellationToken);

        return entity is null ? null : CalendarEventViewMapper.ToView(entity);
    }

    public async Task<bool> UpdateDescriptionsAsync(
        string calendarEventId,
        IReadOnlyList<LocalizedDescription> descriptions,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);
        ArgumentNullException.ThrowIfNull(descriptions);

        var entity = await TryGetEntityAsync(calendarEventId, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        entity.DescriptionsJson = JsonSerializer.Serialize(descriptions, JsonOptions);

        // Conditional on the read ETag so a concurrent change to the same row is
        // not silently overwritten. The start and identity are left untouched;
        // only the descriptions blob is replaced.
        await tableClient.UpdateEntityAsync(
            entity,
            entity.ETag,
            TableUpdateMode.Replace,
            cancellationToken);

        return true;
    }

    public async Task DeleteAsync(
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);

        // An unparseable id can address no row, so it is already gone.
        if (!TryParseScheduledStartUtc(calendarEventId, out var scheduledStartUtc))
        {
            return;
        }

        try
        {
            // Unconditional delete (wildcard ETag): a row changed after the delete
            // use case read it is still removed once YouTube cleanup succeeded.
            await tableClient.DeleteEntityAsync(
                CalendarEventPartitionKey.ForInstant(scheduledStartUtc),
                calendarEventId,
                ETag.All,
                cancellationToken);
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            // The row is already gone, which is the intended end state.
        }
    }

    private async Task<CalendarEventEntity?> TryGetEntityAsync(
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        if (!TryParseScheduledStartUtc(calendarEventId, out var scheduledStartUtc))
        {
            return null;
        }

        try
        {
            var response = await tableClient.GetEntityAsync<CalendarEventEntity>(
                CalendarEventPartitionKey.ForInstant(scheduledStartUtc),
                calendarEventId,
                cancellationToken: cancellationToken);

            return response.Value;
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return null;
        }
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
            CalendarEventIdFormat,
            CultureInfo.InvariantCulture);

    private static bool TryParseScheduledStartUtc(
        string calendarEventId,
        out DateTimeOffset scheduledStartUtc)
    {
        if (DateTime.TryParseExact(
                calendarEventId,
                CalendarEventIdFormat,
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

    private static string FormatLocalDateTime(DateTime localDateTime) =>
        localDateTime.ToString(
            "yyyy-MM-dd'T'HH:mm:ss",
            CultureInfo.InvariantCulture);
}
