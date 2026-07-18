using Azure;
using Azure.Data.Tables;
using YTSkedy.Infrastructure.Storage;
using YTSkedy.Scheduling.Application.Templates;
using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Infrastructure.Templates;

/// <summary>
/// Azure Table-backed template store implementing the write port
/// (<see cref="ITemplateModifier"/>) and the read port
/// (<see cref="ITemplateReader"/>). Templates are partitioned by
/// <see cref="TemplateType"/> through <see cref="TemplatePartitionKey"/>, so all
/// templates of one type share a partition and the row key is the
/// server-generated GUID id. Name uniqueness within a type is enforced
/// check-then-write against the type partition with an ordinal comparison.
/// Duplicate-name races are resolved by the last writer that reaches storage.
/// Storage
/// identity, id generation, and ETags stay inside this class.
/// </summary>
public sealed class AzureTemplateRepository(
    TableClient tableClient,
    TimeProvider timeProvider) :
    ITemplateModifier,
    ITemplateReader
{
    public async Task<CreateTemplateResult> CreateAsync(
        Template template,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(template);

        var partitionKey = TemplatePartitionKey.ForType(template.Type);

        // Check-then-write name uniqueness within the type partition. This
        // store does not use an atomic name-index row.
        if (await NameExistsAsync(
                partitionKey,
                template.Name,
                excludedRowKey: null,
                cancellationToken))
        {
            return CreateTemplateResult.NameAlreadyExists();
        }

        var id = Guid.NewGuid().ToString("N");
        var entity = new TemplateEntity
        {
            PartitionKey = partitionKey,
            RowKey = id,
            TemplateId = id,
            Name = template.Name,
            Type = template.Type.ToString(),
            Content = template.Content,
            CreatedUtc = timeProvider.GetUtcNow()
        };

        await tableClient.AddEntityAsync(entity, cancellationToken);

        return CreateTemplateResult.Created(id);
    }

    public async Task<UpdateTemplateResult> UpdateAsync(
        TemplateType type,
        string id,
        string name,
        string content,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        var partitionKey = TemplatePartitionKey.ForType(type);

        var entity = await GetEntityAsync(partitionKey, id, cancellationToken);

        if (entity is null)
        {
            return UpdateTemplateResult.NotFound;
        }

        if (await NameExistsAsync(
                partitionKey,
                name,
                id,
                cancellationToken))
        {
            return UpdateTemplateResult.NameAlreadyExists;
        }

        entity.Name = name;
        entity.Content = content;

        try
        {
            // Unconditional replace: templates carry no concurrency state machine,
            // so last-write-wins is acceptable and there is no conflict outcome to
            // surface. CreatedUtc, Type, and the keys are preserved by replacing
            // the read entity in place.
            await tableClient.UpdateEntityAsync(
                entity,
                ETag.All,
                TableUpdateMode.Replace,
                cancellationToken);

            return UpdateTemplateResult.Updated;
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            // The row was removed between the read and this write.
            return UpdateTemplateResult.NotFound;
        }
    }

    public async Task<DeleteTemplateResult> DeleteAsync(
        TemplateType type,
        string id,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var partitionKey = TemplatePartitionKey.ForType(type);

        try
        {
            await tableClient.DeleteEntityAsync(
                partitionKey,
                id,
                ETag.All,
                cancellationToken);

            return DeleteTemplateResult.Deleted;
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return DeleteTemplateResult.NotFound;
        }
    }

    public async Task<IReadOnlyList<TemplateView>> ListAsync(
        TemplateType? type,
        CancellationToken cancellationToken)
    {
        var filter = type is null
            ? null
            : PartitionFilter(TemplatePartitionKey.ForType(type.Value));

        var entities = await ListEntitiesAsync(filter, select: null, cancellationToken);

        return TemplateViewMapper.ToViews(entities);
    }

    public async Task<TemplateView?> GetAsync(
        TemplateType type,
        string templateId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateId);

        var entity = await GetEntityAsync(
            TemplatePartitionKey.ForType(type),
            templateId,
            cancellationToken);

        return entity is null ? null : TemplateViewMapper.ToView(entity);
    }

    private Task<TemplateEntity?> GetEntityAsync(
        string partitionKey,
        string rowKey,
        CancellationToken cancellationToken) =>
        tableClient.GetEntityOrNullAsync<TemplateEntity>(
            partitionKey,
            rowKey,
            cancellationToken);

    private Task<IReadOnlyList<TemplateEntity>> ListEntitiesAsync(
        string? filter,
        IEnumerable<string>? select,
        CancellationToken cancellationToken) =>
        tableClient.ListEntitiesAsync<TemplateEntity>(
            filter,
            select,
            cancellationToken);

    private async Task<bool> NameExistsAsync(
        string partitionKey,
        string name,
        string? excludedRowKey,
        CancellationToken cancellationToken)
    {
        var entities = await ListEntitiesAsync(
            PartitionFilter(partitionKey),
            [nameof(TemplateEntity.RowKey), nameof(TemplateEntity.Name)],
            cancellationToken);

        return entities.Any(entity =>
            !string.Equals(entity.RowKey, excludedRowKey, StringComparison.Ordinal) &&
            NameEquals(entity.Name, name));
    }

    private static string PartitionFilter(string partitionKey) =>
        TableClient.CreateQueryFilter($"PartitionKey eq {partitionKey}");

    private static bool NameEquals(string left, string right) =>
        string.Equals(left, right, StringComparison.Ordinal);
}
