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

    public async Task<IReadOnlyList<CalendarEventListItem>> ListAsync(
        CalendarEventMonthCriteria? criteria,
        CancellationToken cancellationToken) =>
        criteria is null
            ? await ListAllAsync(cancellationToken)
            : await ListByMonthAsync(criteria, cancellationToken);

    private async Task<IReadOnlyList<CalendarEventListItem>> ListByMonthAsync(
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

    private async Task<IReadOnlyList<CalendarEventListItem>> ListAllAsync(
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

        return CalendarEventReadMapper.ToListItems(entities);
    }

    public async Task<CalendarEventDetail?> GetByIdAsync(
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);

        var entity = await TryGetEntityAsync(calendarEventId, cancellationToken);

        return entity is null ? null : CalendarEventReadMapper.ToDetail(entity);
    }

    public async Task<CalendarEventListItem?> GetListItemByIdAsync(
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);

        var entity = await TryGetEntityAsync(calendarEventId, cancellationToken);

        return entity is null ? null : CalendarEventReadMapper.ToListItem(entity);
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
        // not silently overwritten. The start, identity, and status are left
        // untouched; only the descriptions blob is replaced.
        await tableClient.UpdateEntityAsync(
            entity,
            entity.ETag,
            TableUpdateMode.Replace,
            cancellationToken);

        return true;
    }

    public async Task<bool> TryReserveForPublishingAsync(
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);

        var entity = await TryGetEntityAsync(calendarEventId, cancellationToken);

        if (entity is null ||
            CalendarEventReadMapper.ParseStatus(entity.Status) != CalendarEventStatus.Draft)
        {
            return false;
        }

        entity.Status = CalendarEventStatus.Publishing.ToString();

        try
        {
            await tableClient.UpdateEntityAsync(
                entity,
                entity.ETag,
                TableUpdateMode.Replace,
                cancellationToken);

            return true;
        }
        catch (RequestFailedException exception) when (exception.Status == 412)
        {
            // A concurrent publish changed the row between the read and this
            // write. That request owns the reservation, so this one must not
            // proceed to YouTube.
            return false;
        }
    }

    public async Task MarkPublishedAsync(
        string calendarEventId,
        string youTubeBroadcastId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);
        ArgumentException.ThrowIfNullOrWhiteSpace(youTubeBroadcastId);

        var entity = await GetEntityOrThrowAsync(calendarEventId, cancellationToken);
        entity.Status = CalendarEventStatus.Published.ToString();
        entity.YouTubeBroadcastId = youTubeBroadcastId;

        await tableClient.UpdateEntityAsync(
            entity,
            entity.ETag,
            TableUpdateMode.Replace,
            cancellationToken);
    }

    public async Task ReleaseReservationAsync(
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);

        var entity = await TryGetEntityAsync(calendarEventId, cancellationToken);

        // Releasing is best-effort compensation. If the row is gone or no longer
        // reserved by this publish (a concurrent request already advanced or
        // reset it), there is nothing for this caller to undo, so do not throw
        // and mask the original failure that triggered the release.
        if (entity is null ||
            CalendarEventReadMapper.ParseStatus(entity.Status) != CalendarEventStatus.Publishing)
        {
            return;
        }

        entity.Status = CalendarEventStatus.Draft.ToString();

        try
        {
            await tableClient.UpdateEntityAsync(
                entity,
                entity.ETag,
                TableUpdateMode.Replace,
                cancellationToken);
        }
        catch (RequestFailedException exception) when (exception.Status is 404 or 412)
        {
            // A concurrent write changed or removed the row after the read. The
            // reservation is no longer this caller's to release, so leave the
            // other writer's state intact.
        }
    }

    public async Task<DeleteDraftCalendarEventResult> DeleteDraftAsync(
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);

        // Reload here so the delete is conditioned on the latest ETag and storage
        // identity (partition key, row key) stays inside infrastructure rather
        // than crossing the application boundary.
        var entity = await TryGetEntityAsync(calendarEventId, cancellationToken);

        if (entity is null)
        {
            return DeleteDraftCalendarEventResult.NotFound;
        }

        if (CalendarEventReadMapper.ParseStatus(entity.Status) != CalendarEventStatus.Draft)
        {
            return DeleteDraftCalendarEventResult.NotDeletable;
        }

        try
        {
            await tableClient.DeleteEntityAsync(
                entity.PartitionKey,
                entity.RowKey,
                entity.ETag,
                cancellationToken);

            return DeleteDraftCalendarEventResult.Deleted;
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            // The row was removed between the Draft read and this delete.
            return DeleteDraftCalendarEventResult.NotFound;
        }
        catch (RequestFailedException exception) when (exception.Status == 412)
        {
            // A concurrent write changed the row (for example a publish
            // reservation) after the Draft read, so this delete must not proceed.
            return DeleteDraftCalendarEventResult.NotDeletable;
        }
    }

    public async Task<DeleteCalendarEventRowResult> DeleteAsync(
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);

        // Post-YouTube Published cleanup: delete by id without checking status.
        // An unparseable id can address no row, so it is already gone.
        if (!TryParseScheduledStartUtc(calendarEventId, out var scheduledStartUtc))
        {
            return DeleteCalendarEventRowResult.NotFound;
        }

        try
        {
            // Unconditional delete (wildcard ETag): a row changed after the delete
            // use case read it is still removed once YouTube cleanup succeeded.
            await tableClient.DeleteEntityAsync(
                GetPartitionKey(scheduledStartUtc),
                calendarEventId,
                ETag.All,
                cancellationToken);

            return DeleteCalendarEventRowResult.Deleted;
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return DeleteCalendarEventRowResult.NotFound;
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
                GetPartitionKey(scheduledStartUtc),
                calendarEventId,
                cancellationToken: cancellationToken);

            return response.Value;
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return null;
        }
    }

    private async Task<CalendarEventEntity> GetEntityOrThrowAsync(
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        var entity = await TryGetEntityAsync(calendarEventId, cancellationToken);

        return entity ?? throw new InvalidOperationException(
            $"Calendar event '{calendarEventId}' was not found while completing a publish.");
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

    internal static string GetPartitionKey(DateTimeOffset scheduledStartUtc) =>
        scheduledStartUtc.UtcDateTime.ToString(
            "'calendar-events-'yyyyMM",
            CultureInfo.InvariantCulture);

    private static string FormatLocalDateTime(DateTime localDateTime) =>
        localDateTime.ToString(
            "yyyy-MM-dd'T'HH:mm:ss",
            CultureInfo.InvariantCulture);
}
