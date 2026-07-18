using Azure;
using Azure.Core;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using YTSkedy.Infrastructure.CalendarEvents;
using YTSkedy.Infrastructure.Platforms;
using YTSkedy.Infrastructure.Settings;
using YTSkedy.Infrastructure.Templates;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Infrastructure.Test.TestSupport;

internal abstract class InMemoryTableClient<TEntity>(string tableName) : TableClient
    where TEntity : class, ITableEntity
{
    private readonly HashSet<(string PartitionKey, string RowKey)> deletePreconditionFailures = [];
    private readonly Dictionary<
        (string PartitionKey, string RowKey),
        Queue<Action<TEntity>?>> updatePreconditionFailures = [];

    public Dictionary<(string PartitionKey, string RowKey), TEntity> Entities { get; } = [];

    public bool CreateIfNotExistsCalled { get; private set; }

    public int UpdateCallCount { get; private set; }

    public TableUpdateMode? LastUpdateMode { get; private set; }

    public IReadOnlyList<string>? LastQuerySelect { get; private set; }

    public int? LastQueryMaxPerPage { get; private set; }

    public int QueryCallCount { get; private set; }

    public void Seed(TEntity entity)
    {
        Entities[(entity.PartitionKey, entity.RowKey)] = Clone(entity);
    }

    public void FailDeleteWithPreconditionFailed(TEntity entity)
    {
        deletePreconditionFailures.Add((entity.PartitionKey, entity.RowKey));
    }

    public void FailNextUpdateWithPreconditionFailed(
        TEntity entity,
        Action<TEntity>? onFailure = null)
    {
        var key = (entity.PartitionKey, entity.RowKey);
        if (!updatePreconditionFailures.TryGetValue(key, out var failures))
        {
            failures = [];
            updatePreconditionFailures[key] = failures;
        }

        failures.Enqueue(onFailure);
    }

    public override Task<Response<TableItem>> CreateIfNotExistsAsync(
        CancellationToken cancellationToken = default)
    {
        CreateIfNotExistsCalled = true;

        return Task.FromResult(Response.FromValue(
            TableModelFactory.TableItem(tableName),
            StubResponse.Instance));
    }

    public override Task<Response> AddEntityAsync<T>(
        T entity,
        CancellationToken cancellationToken = default)
    {
        var typedEntity = ToEntity(entity);
        var key = (typedEntity.PartitionKey, typedEntity.RowKey);
        if (Entities.ContainsKey(key))
        {
            throw new RequestFailedException(409, "Entity already exists.");
        }

        Entities[key] = Clone(typedEntity);

        return Task.FromResult<Response>(StubResponse.Instance);
    }

    public override Task<Response> UpsertEntityAsync<T>(
        T entity,
        TableUpdateMode mode = TableUpdateMode.Merge,
        CancellationToken cancellationToken = default)
    {
        var typedEntity = ToEntity(entity);
        Entities[(typedEntity.PartitionKey, typedEntity.RowKey)] = Clone(typedEntity);

        return Task.FromResult<Response>(StubResponse.Instance);
    }

    public override Task<Response> UpdateEntityAsync<T>(
        T entity,
        ETag ifMatch,
        TableUpdateMode mode,
        CancellationToken cancellationToken = default)
    {
        var typedEntity = ToEntity(entity);
        var key = (typedEntity.PartitionKey, typedEntity.RowKey);
        UpdateCallCount++;
        LastUpdateMode = mode;
        if (!Entities.ContainsKey(key))
        {
            throw new RequestFailedException(404, "Entity not found.");
        }

        if (updatePreconditionFailures.TryGetValue(key, out var failures) &&
            failures.TryDequeue(out var onFailure))
        {
            onFailure?.Invoke(Entities[key]);
            throw new RequestFailedException(412, "Precondition failed.");
        }

        Entities[key] = Clone(typedEntity);

        return Task.FromResult<Response>(StubResponse.Instance);
    }

    public override Task<Response> DeleteEntityAsync(
        string partitionKey,
        string rowKey,
        ETag ifMatch = default,
        CancellationToken cancellationToken = default)
    {
        var key = (partitionKey, rowKey);
        if (deletePreconditionFailures.Contains(key))
        {
            throw new RequestFailedException(412, "Precondition failed.");
        }

        if (!Entities.Remove(key))
        {
            throw new RequestFailedException(404, "Entity not found.");
        }

        return Task.FromResult<Response>(StubResponse.Instance);
    }

    public override Task<NullableResponse<T>> GetEntityIfExistsAsync<T>(
        string partitionKey,
        string rowKey,
        IEnumerable<string>? select = null,
        CancellationToken cancellationToken = default)
    {
        if (Entities.TryGetValue((partitionKey, rowKey), out var entity))
        {
            return Task.FromResult<NullableResponse<T>>(
                Response.FromValue((T)(object)Clone(entity), StubResponse.Instance));
        }

        return Task.FromResult<NullableResponse<T>>(new EmptyNullableResponse<T>());
    }

    public override AsyncPageable<T> QueryAsync<T>(
        string? filter = null,
        int? maxPerPage = null,
        IEnumerable<string>? select = null,
        CancellationToken cancellationToken = default)
    {
        QueryCallCount++;
        LastQuerySelect = select?.ToArray();
        LastQueryMaxPerPage = maxPerPage;

        var values = Entities.Values
            .Where(entity => MatchesFilter(entity, filter))
            .Select(entity => (T)(object)Clone(entity))
            .ToArray();

        return AsyncPageable<T>.FromPages(
            [Azure.Page<T>.FromValues(values, continuationToken: null, StubResponse.Instance)]);
    }

    protected abstract TEntity Clone(TEntity entity);

    protected virtual bool MatchesFilter(TEntity entity, string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return true;
        }

        throw new NotSupportedException($"Unsupported filter '{filter}'.");
    }

    private TEntity ToEntity<T>(T entity)
    {
        if (entity is not TEntity typedEntity)
        {
            throw new InvalidOperationException($"Unsupported entity type '{typeof(T).Name}'.");
        }

        return typedEntity;
    }
}

internal sealed class CalendarEventTableClient : InMemoryTableClient<CalendarEventEntity>
{
    public CalendarEventTableClient()
        : base("CalendarEvents")
    {
    }

    protected override bool MatchesFilter(CalendarEventEntity entity, string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return true;
        }

        foreach (var clause in filter.Split(" and ", StringSplitOptions.None))
        {
            if (TryStringClause(clause, "PartitionKey", "eq", out var partitionKey))
            {
                if (!string.Equals(
                        entity.PartitionKey,
                        partitionKey,
                        StringComparison.Ordinal))
                {
                    return false;
                }

                continue;
            }

            if (TryStringClause(clause, "RowKey", "ne", out var excludedRowKey))
            {
                if (string.Equals(entity.RowKey, excludedRowKey, StringComparison.Ordinal))
                {
                    return false;
                }

                continue;
            }

            if (TryStringClause(clause, "LocalDateTime", "ge", out var monthStart))
            {
                if (string.CompareOrdinal(entity.LocalDateTime, monthStart) < 0)
                {
                    return false;
                }

                continue;
            }

            if (TryStringClause(clause, "LocalDateTime", "lt", out var nextMonthStart))
            {
                if (string.CompareOrdinal(entity.LocalDateTime, nextMonthStart) >= 0)
                {
                    return false;
                }

                continue;
            }

            const string scheduledStartPrefix = "ScheduledStartUtc eq datetime'";
            if (clause.StartsWith(scheduledStartPrefix, StringComparison.Ordinal) &&
                clause.EndsWith('\'') &&
                DateTimeOffset.TryParse(
                    clause[scheduledStartPrefix.Length..^1],
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AssumeUniversal,
                    out var scheduledStartUtc))
            {
                if (entity.ScheduledStartUtc != scheduledStartUtc)
                {
                    return false;
                }

                continue;
            }

            throw new NotSupportedException($"Unsupported filter '{filter}'.");
        }

        return true;
    }

    private static bool TryStringClause(
        string clause,
        string propertyName,
        string operation,
        out string value)
    {
        var prefix = $"{propertyName} {operation} '";
        if (clause.StartsWith(prefix, StringComparison.Ordinal) &&
            clause.EndsWith('\''))
        {
            value = clause[prefix.Length..^1].Replace(
                "''",
                "'",
                StringComparison.Ordinal);
            return true;
        }

        value = string.Empty;
        return false;
    }

    protected override CalendarEventEntity Clone(CalendarEventEntity entity) =>
        new()
        {
            PartitionKey = entity.PartitionKey,
            RowKey = entity.RowKey,
            Timestamp = entity.Timestamp,
            ETag = entity.ETag,
            CalendarEventId = entity.CalendarEventId,
            ScheduledStartUtc = entity.ScheduledStartUtc,
            LocalDateTime = entity.LocalDateTime,
            TimeZoneId = entity.TimeZoneId,
            TextJson = entity.TextJson,
            PublishedPlatformIdsJson = entity.PublishedPlatformIdsJson,
            ThumbnailJson = entity.ThumbnailJson,
            CreatedUtc = entity.CreatedUtc
        };
}

internal sealed class PlatformTableClient : InMemoryTableClient<PlatformEntity>
{
    public PlatformTableClient()
        : base("Platforms")
    {
    }

    protected override bool MatchesFilter(PlatformEntity entity, string? filter) =>
        string.IsNullOrWhiteSpace(filter) ||
        string.Equals(filter, "PartitionKey eq 'platforms'", StringComparison.Ordinal)
            ? true
            : throw new NotSupportedException($"Unsupported filter '{filter}'.");

    protected override PlatformEntity Clone(PlatformEntity entity) =>
        new()
        {
            PartitionKey = entity.PartitionKey,
            RowKey = entity.RowKey,
            Timestamp = entity.Timestamp,
            ETag = entity.ETag,
            PlatformId = entity.PlatformId,
            Name = entity.Name,
            ReferenceKey = entity.ReferenceKey,
            Type = entity.Type,
            TitleTemplateId = entity.TitleTemplateId,
            DescriptionTemplateId = entity.DescriptionTemplateId,
            PublishSettingsJson = entity.PublishSettingsJson,
            CreatedUtc = entity.CreatedUtc,
            UpdatedUtc = entity.UpdatedUtc
        };
}

internal sealed class TemplateTableClient : InMemoryTableClient<TemplateEntity>
{
    public TemplateTableClient()
        : base("Templates")
    {
    }

    protected override bool MatchesFilter(TemplateEntity entity, string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return true;
        }

        var prefix = "PartitionKey eq '";
        if (!filter.StartsWith(prefix, StringComparison.Ordinal) ||
            !filter.EndsWith('\''))
        {
            throw new NotSupportedException($"Unsupported filter '{filter}'.");
        }

        return string.Equals(
            entity.PartitionKey,
            filter[prefix.Length..^1],
            StringComparison.Ordinal);
    }

    protected override TemplateEntity Clone(TemplateEntity entity) =>
        new()
        {
            PartitionKey = entity.PartitionKey,
            RowKey = entity.RowKey,
            Timestamp = entity.Timestamp,
            ETag = entity.ETag,
            TemplateId = entity.TemplateId,
            Name = entity.Name,
            Type = entity.Type,
            Content = entity.Content,
            CreatedUtc = entity.CreatedUtc
        };
}

internal sealed class PlatformPublicationTableClient
    : InMemoryTableClient<PlatformPublicationEntity>
{
    public PlatformPublicationTableClient()
        : base("PlatformPublications")
    {
    }

    protected override bool MatchesFilter(PlatformPublicationEntity entity, string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return true;
        }

        if (TryReadSingleClause(filter, "PartitionKey", out var partitionKey))
        {
            return string.Equals(entity.PartitionKey, partitionKey, StringComparison.Ordinal);
        }

        if (TryReadTwoClauses(
                filter,
                "PlatformId",
                "Status",
                out var platformId,
                out var status))
        {
            if (!string.Equals(status, PublishStatus.Publishing.ToString(), StringComparison.Ordinal) &&
                !string.Equals(status, PublishStatus.Published.ToString(), StringComparison.Ordinal))
            {
                throw new NotSupportedException($"Unsupported filter '{filter}'.");
            }

            return string.Equals(entity.PlatformId, platformId, StringComparison.Ordinal) &&
                string.Equals(entity.Status, status, StringComparison.Ordinal);
        }

        throw new NotSupportedException($"Unsupported filter '{filter}'.");
    }

    protected override PlatformPublicationEntity Clone(PlatformPublicationEntity entity) =>
        new()
        {
            PartitionKey = entity.PartitionKey,
            RowKey = entity.RowKey,
            Timestamp = entity.Timestamp,
            ETag = entity.ETag,
            CalendarEventId = entity.CalendarEventId,
            PlatformId = entity.PlatformId,
            PlatformName = entity.PlatformName,
            PlatformType = entity.PlatformType,
            Status = entity.Status,
            ExternalResourceId = entity.ExternalResourceId,
            ThumbnailStatus = entity.ThumbnailStatus,
            ContentSnapshotTitle = entity.ContentSnapshotTitle,
            ContentSnapshotDescription = entity.ContentSnapshotDescription,
            PublishSettingsJson = entity.PublishSettingsJson,
            PublishedUtc = entity.PublishedUtc,
            PlatformDeletedUtc = entity.PlatformDeletedUtc,
            CreatedUtc = entity.CreatedUtc,
            UpdatedUtc = entity.UpdatedUtc
        };

    private static bool TryReadSingleClause(
        string filter,
        string propertyName,
        out string value)
    {
        var prefix = $"{propertyName} eq '";
        if (!filter.StartsWith(prefix, StringComparison.Ordinal) ||
            filter[^1] != '\'')
        {
            value = string.Empty;
            return false;
        }

        value = filter[prefix.Length..^1];
        return true;
    }

    private static bool TryReadTwoClauses(
        string filter,
        string firstPropertyName,
        string secondPropertyName,
        out string firstValue,
        out string secondValue)
    {
        firstValue = string.Empty;
        secondValue = string.Empty;

        var separator = "' and ";
        var separatorIndex = filter.IndexOf(separator, StringComparison.Ordinal);
        if (separatorIndex < 0)
        {
            return false;
        }

        var firstClause = filter[..(separatorIndex + 1)];
        var secondClause = filter[(separatorIndex + separator.Length)..];

        return TryReadSingleClause(firstClause, firstPropertyName, out firstValue) &&
            TryReadSingleClause(secondClause, secondPropertyName, out secondValue);
    }
}

internal sealed class ApplicationSettingsTableClient
    : InMemoryTableClient<ApplicationSettingsEntity>
{
    public ApplicationSettingsTableClient()
        : base("ApplicationSettings")
    {
    }

    public bool SubmitTransactionCalled { get; private set; }

    public override Task<Response<IReadOnlyList<Response>>> SubmitTransactionAsync(
        IEnumerable<TableTransactionAction> transactionActions,
        CancellationToken cancellationToken = default)
    {
        var actions = transactionActions.ToArray();
        var entities = actions.Select(action =>
        {
            if (action.ActionType != TableTransactionActionType.UpsertReplace ||
                action.Entity is not ApplicationSettingsEntity entity)
            {
                throw new NotSupportedException(
                    $"Unsupported application settings transaction action '{action.ActionType}'.");
            }

            return entity;
        }).ToArray();

        SubmitTransactionCalled = true;
        foreach (var entity in entities)
        {
            Entities[(entity.PartitionKey, entity.RowKey)] = Clone(entity);
        }

        IReadOnlyList<Response> responses = actions
            .Select(_ => (Response)StubResponse.Instance)
            .ToArray();

        return Task.FromResult(Response.FromValue(responses, StubResponse.Instance));
    }

    protected override ApplicationSettingsEntity Clone(ApplicationSettingsEntity entity) =>
        new()
        {
            PartitionKey = entity.PartitionKey,
            RowKey = entity.RowKey,
            Timestamp = entity.Timestamp,
            ETag = entity.ETag,
            ValueJson = entity.ValueJson
        };
}

internal sealed class EmptyNullableResponse<T> : NullableResponse<T>
{
    public override bool HasValue => false;

    public override T Value => throw new InvalidOperationException("No value is available.");

    public override Response GetRawResponse() => StubResponse.Instance;
}

internal sealed class StubResponse : Response
{
    public static readonly StubResponse Instance = new();

    private StubResponse()
    {
    }

    public override int Status => 200;

    public override string ReasonPhrase => "OK";

    public override Stream? ContentStream { get; set; }

    public override string ClientRequestId { get; set; } = string.Empty;

    protected override bool ContainsHeader(string name) => false;

    protected override IEnumerable<HttpHeader> EnumerateHeaders() => [];

    protected override bool TryGetHeader(string name, out string value)
    {
        value = string.Empty;

        return false;
    }

    protected override bool TryGetHeaderValues(string name, out IEnumerable<string> values)
    {
        values = [];

        return false;
    }

    public override void Dispose()
    {
    }
}
