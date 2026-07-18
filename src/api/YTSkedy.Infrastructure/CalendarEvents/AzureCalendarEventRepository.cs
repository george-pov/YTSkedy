using Azure;
using Azure.Data.Tables;
using System.Globalization;
using YTSkedy.Infrastructure.Storage;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.CalendarEvents.Thumbnails;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Infrastructure.CalendarEvents;

public sealed class AzureCalendarEventRepository(
    TableClient tableClient,
    TimeProvider timeProvider) :
    ICalendarEventModifier,
    ICalendarEventReader,
    IPublicationIndexWriter,
    ICalendarEventThumbnailReader,
    ICalendarEventThumbnailModifier
{
    private const int PublicationIndexConditionalWriteAttempts = 3;

    public async Task<string> CreateAsync(
        CalendarEvent calendarEvent,
        DateTimeOffset scheduledStartUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(calendarEvent);

        if (await HasScheduledStartAsync(
                scheduledStartUtc,
                excludedCalendarEventId: null,
                cancellationToken))
        {
            throw new DuplicateScheduledStartException(scheduledStartUtc);
        }

        var calendarEventId = CalendarEventStorageKey.NewCalendarEventId();
        var entity = new CalendarEventEntity
        {
            PartitionKey = CalendarEventStorageKey.PartitionKey,
            RowKey = CalendarEventStorageKey.RowKeyFor(calendarEventId),
            CalendarEventId = calendarEventId,
            ScheduledStartUtc = scheduledStartUtc,
            LocalDateTime = FormatLocalDateTime(calendarEvent.Start.LocalDateTime),
            TimeZoneId = calendarEvent.Start.TimeZoneId,
            TextJson = CalendarEventViewMapper.SerializeText(calendarEvent.Text),
            PublishedPlatformIdsJson = PublishedPlatformIdsJson.Serialize([]),
            CreatedUtc = timeProvider.GetUtcNow()
        };

        await tableClient.AddEntityAsync(entity, cancellationToken);

        return calendarEventId;
    }

    public async Task<IReadOnlyList<CalendarEventListRecord>> ListAsync(
        CalendarEventMonthCriteria? criteria,
        CancellationToken cancellationToken) =>
        criteria is null
            ? await ListAllAsync(cancellationToken)
            : await ListByMonthAsync(criteria, cancellationToken);

    private async Task<IReadOnlyList<CalendarEventListRecord>> ListByMonthAsync(
        CalendarEventMonthCriteria criteria,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        return CalendarEventViewMapper.ToListRecords(
            await tableClient.ListEntitiesAsync<CalendarEventEntity>(
                MonthFilter(criteria),
                select: null,
                cancellationToken));
    }

    private async Task<IReadOnlyList<CalendarEventListRecord>> ListAllAsync(
        CancellationToken cancellationToken) =>
        CalendarEventViewMapper.ToListRecords(
            await tableClient.ListEntitiesAsync<CalendarEventEntity>(
                PartitionFilter(),
                select: null,
                cancellationToken));

    public async Task<CalendarEventView?> GetByIdAsync(
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);

        var entity = await TryGetEntityAsync(calendarEventId, cancellationToken);

        return entity is null ? null : CalendarEventViewMapper.ToView(entity);
    }

    public async Task<CalendarEventChangeResult> UpdateAsync(
        string calendarEventId,
        CalendarEvent calendarEvent,
        DateTimeOffset scheduledStartUtc,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);
        ArgumentNullException.ThrowIfNull(calendarEvent);

        var entity = await TryGetEntityAsync(calendarEventId, cancellationToken);

        if (entity is null)
        {
            return CalendarEventChangeResult.NotFound;
        }

        if (await HasScheduledStartAsync(
                scheduledStartUtc,
                excludedCalendarEventId: calendarEventId,
                cancellationToken))
        {
            throw new DuplicateScheduledStartException(scheduledStartUtc);
        }

        entity.ScheduledStartUtc = scheduledStartUtc;
        entity.LocalDateTime = FormatLocalDateTime(calendarEvent.Start.LocalDateTime);
        entity.TimeZoneId = calendarEvent.Start.TimeZoneId;
        entity.TextJson = CalendarEventViewMapper.SerializeText(calendarEvent.Text);

        try
        {
            // Conditional on the read ETag so a concurrent change to the same row is
            // not silently overwritten.
            await tableClient.UpdateEntityAsync(
                entity,
                entity.ETag,
                TableUpdateMode.Replace,
                cancellationToken);
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return CalendarEventChangeResult.NotFound;
        }
        catch (RequestFailedException exception) when (exception.Status == 412)
        {
            return CalendarEventChangeResult.Conflict;
        }

        return CalendarEventChangeResult.Applied;
    }

    public async Task<Thumbnail?> GetThumbnailAsync(
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);

        var entity = await TryGetEntityAsync(calendarEventId, cancellationToken);

        return entity is null
            ? null
            : ThumbnailJson.Deserialize(entity.ThumbnailJson, calendarEventId);
    }

    public async Task<CalendarEventChangeResult> SaveThumbnailAsync(
        string calendarEventId,
        Thumbnail thumbnail,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);
        ArgumentNullException.ThrowIfNull(thumbnail);

        var entity = await TryGetEntityAsync(calendarEventId, cancellationToken);
        if (entity is null)
        {
            return CalendarEventChangeResult.NotFound;
        }

        entity.ThumbnailJson = ThumbnailJson.FromDomain(thumbnail).Serialize();

        try
        {
            await tableClient.UpdateEntityAsync(
                entity,
                entity.ETag,
                TableUpdateMode.Replace,
                cancellationToken);
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return CalendarEventChangeResult.NotFound;
        }
        catch (RequestFailedException exception) when (exception.Status == 412)
        {
            return CalendarEventChangeResult.Conflict;
        }

        return CalendarEventChangeResult.Applied;
    }

    public async Task<CalendarEventChangeResult> DeleteThumbnailAsync(
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);

        var entity = await TryGetEntityAsync(calendarEventId, cancellationToken);
        if (entity is null)
        {
            return CalendarEventChangeResult.NotFound;
        }

        entity.ThumbnailJson = null;

        try
        {
            await tableClient.UpdateEntityAsync(
                entity,
                entity.ETag,
                TableUpdateMode.Replace,
                cancellationToken);
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return CalendarEventChangeResult.NotFound;
        }
        catch (RequestFailedException exception) when (exception.Status == 412)
        {
            return CalendarEventChangeResult.Conflict;
        }

        return CalendarEventChangeResult.Applied;
    }

    public Task<bool> AddPublishedPlatformAsync(
        string calendarEventId,
        string platformId,
        CancellationToken cancellationToken) =>
        UpdatePublishedPlatformIdsAsync(
            calendarEventId,
            platformId,
            add: true,
            cancellationToken);

    public Task<bool> RemovePublishedPlatformAsync(
        string calendarEventId,
        string platformId,
        CancellationToken cancellationToken) =>
        UpdatePublishedPlatformIdsAsync(
            calendarEventId,
            platformId,
            add: false,
            cancellationToken);

    public async Task DeleteAsync(
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);

        var entity = await TryGetEntityAsync(calendarEventId, cancellationToken);

        if (entity is null)
        {
            return;
        }

        try
        {
            // Unconditional delete (wildcard ETag): a row changed after the delete
            // use case read it is still removed.
            await tableClient.DeleteEntityAsync(
                entity.PartitionKey,
                entity.RowKey,
                ETag.All,
                cancellationToken);
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            // The row is already gone, which is the intended end state.
        }
    }

    private async Task<bool> HasScheduledStartAsync(
        DateTimeOffset scheduledStartUtc,
        string? excludedCalendarEventId,
        CancellationToken cancellationToken)
    {
        return await tableClient.AnyEntityAsync<CalendarEventEntity>(
            ScheduledStartFilter(scheduledStartUtc, excludedCalendarEventId),
            cancellationToken);
    }

    private async Task<bool> UpdatePublishedPlatformIdsAsync(
        string calendarEventId,
        string platformId,
        bool add,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);
        ArgumentException.ThrowIfNullOrWhiteSpace(platformId);

        for (var attempt = 0; attempt < PublicationIndexConditionalWriteAttempts; attempt++)
        {
            var entity = await TryGetEntityAsync(calendarEventId, cancellationToken);
            if (entity is null)
            {
                return false;
            }

            var publishedPlatformIds = new HashSet<string>(
                PublishedPlatformIdsJson.Deserialize(
                    entity.PublishedPlatformIdsJson,
                    calendarEventId),
                StringComparer.Ordinal);
            var changed = add
                ? publishedPlatformIds.Add(platformId)
                : publishedPlatformIds.Remove(platformId);
            if (!changed)
            {
                return true;
            }

            entity.PublishedPlatformIdsJson = PublishedPlatformIdsJson.Serialize(
                publishedPlatformIds);

            try
            {
                await tableClient.UpdateEntityAsync(
                    entity,
                    entity.ETag,
                    TableUpdateMode.Merge,
                    cancellationToken);

                return true;
            }
            catch (RequestFailedException exception) when (exception.Status == 404)
            {
                return false;
            }
            catch (RequestFailedException exception) when (exception.Status == 412)
            {
                // A concurrent row update won. Re-read and reapply the set
                // operation while the bounded retry budget remains.
            }
        }

        return false;
    }

    private async Task<CalendarEventEntity?> TryGetEntityAsync(
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        if (!CalendarEventStorageKey.TryGetAddress(
                calendarEventId,
                out var partitionKey,
                out var rowKey))
        {
            return null;
        }

        var entity = await tableClient.GetEntityOrNullAsync<CalendarEventEntity>(
            partitionKey,
            rowKey,
            cancellationToken);

        return entity is not null &&
            string.Equals(
                entity.CalendarEventId,
                calendarEventId,
                StringComparison.Ordinal)
                ? entity
                : null;
    }

    private static string PartitionFilter() =>
        TableClient.CreateQueryFilter(
            $"PartitionKey eq {CalendarEventStorageKey.PartitionKey}");

    private static string MonthFilter(CalendarEventMonthCriteria criteria)
    {
        var monthStart = new DateTime(
            criteria.Year,
            criteria.Month,
            1).ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
        var nextMonthStart = new DateTime(
            criteria.Year,
            criteria.Month,
            1).AddMonths(1).ToString(
                "yyyy-MM-dd'T'HH:mm:ss",
                CultureInfo.InvariantCulture);

        FormattableString filter =
            $"PartitionKey eq {CalendarEventStorageKey.PartitionKey} and LocalDateTime ge {monthStart} and LocalDateTime lt {nextMonthStart}";

        return TableClient.CreateQueryFilter(filter);
    }

    private static string ScheduledStartFilter(
        DateTimeOffset scheduledStartUtc,
        string? excludedCalendarEventId) =>
        TableClient.CreateQueryFilter(
            ScheduledStartFilterFormattable(
                scheduledStartUtc,
                excludedCalendarEventId));

    private static FormattableString ScheduledStartFilterFormattable(
        DateTimeOffset scheduledStartUtc,
        string? excludedCalendarEventId)
    {
        if (excludedCalendarEventId is null)
        {
            return $"PartitionKey eq {CalendarEventStorageKey.PartitionKey} and ScheduledStartUtc eq {scheduledStartUtc}";
        }

        return $"PartitionKey eq {CalendarEventStorageKey.PartitionKey} and ScheduledStartUtc eq {scheduledStartUtc} and RowKey ne {CalendarEventStorageKey.RowKeyFor(excludedCalendarEventId)}";
    }

    private static string FormatLocalDateTime(DateTime localDateTime) =>
        localDateTime.ToString(
            "yyyy-MM-dd'T'HH:mm:ss",
            CultureInfo.InvariantCulture);
}
