using Azure;
using Azure.Data.Tables;
using System.Globalization;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Infrastructure.CalendarEvents;

public sealed class AzureCalendarEventRepository(
    TableClient tableClient,
    TimeProvider timeProvider) :
    ICalendarEventModifier,
    ICalendarEventReader,
    ICalendarEventThumbnailReader,
    ICalendarEventThumbnailModifier
{
    public async Task<string> CreateAsync(
        CalendarEvent calendarEvent,
        DateTimeOffset scheduledStartUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(calendarEvent);

        await tableClient.CreateIfNotExistsAsync(cancellationToken);

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
            CreatedUtc = timeProvider.GetUtcNow()
        };

        await tableClient.AddEntityAsync(entity, cancellationToken);

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

        return CalendarEventViewMapper.ToViewsForMonth(
            await ListEntitiesAsync(cancellationToken),
            criteria);
    }

    private async Task<IReadOnlyList<CalendarEventView>> ListAllAsync(
        CancellationToken cancellationToken) =>
        CalendarEventViewMapper.ToViews(await ListEntitiesAsync(cancellationToken));

    public async Task<CalendarEventView?> GetByIdAsync(
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);

        var entity = await TryGetEntityAsync(calendarEventId, cancellationToken);

        return entity is null ? null : CalendarEventViewMapper.ToView(entity);
    }

    public async Task<bool> UpdateAsync(
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
            return false;
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
            return false;
        }

        return true;
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

    public async Task<bool> SaveThumbnailAsync(
        string calendarEventId,
        Thumbnail thumbnail,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);
        ArgumentNullException.ThrowIfNull(thumbnail);

        var entity = await TryGetEntityAsync(calendarEventId, cancellationToken);
        if (entity is null)
        {
            return false;
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
            return false;
        }

        return true;
    }

    public async Task<bool> DeleteThumbnailAsync(
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);

        var entity = await TryGetEntityAsync(calendarEventId, cancellationToken);
        if (entity is null)
        {
            return false;
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
            return false;
        }

        return true;
    }

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
        var entities = await ListEntitiesAsync(cancellationToken);

        return entities.Any(entity =>
            entity.ScheduledStartUtc == scheduledStartUtc &&
            !string.Equals(
                entity.CalendarEventId,
                excludedCalendarEventId,
                StringComparison.Ordinal));
    }

    private async Task<IReadOnlyList<CalendarEventEntity>> ListEntitiesAsync(
        CancellationToken cancellationToken)
    {
        var entities = new List<CalendarEventEntity>();

        try
        {
            await foreach (var entity in tableClient.QueryAsync<CalendarEventEntity>(
                CalendarEventStorageKey.PartitionFilter(),
                cancellationToken: cancellationToken))
            {
                entities.Add(entity);
            }
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return [];
        }

        return entities;
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

        try
        {
            var response = await tableClient.GetEntityIfExistsAsync<CalendarEventEntity>(
                partitionKey,
                rowKey,
                cancellationToken: cancellationToken);

            if (!response.HasValue || response.Value is not { } entity)
            {
                return null;
            }

            return string.Equals(
                entity.CalendarEventId,
                calendarEventId,
                StringComparison.Ordinal)
                    ? entity
                    : null;
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return null;
        }
    }

    private static string FormatLocalDateTime(DateTime localDateTime) =>
        localDateTime.ToString(
            "yyyy-MM-dd'T'HH:mm:ss",
            CultureInfo.InvariantCulture);
}
