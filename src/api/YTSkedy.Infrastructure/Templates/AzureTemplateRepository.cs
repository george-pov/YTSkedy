using Azure;
using Azure.Data.Tables;
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

        await tableClient.CreateIfNotExistsAsync(cancellationToken);

        // Check-then-write name uniqueness within the type partition. The rare
        // This store does not use an atomic name-index row.
        var partitionEntities = await QueryEntitiesAsync(
            PartitionFilter(partitionKey),
            cancellationToken);

        if (partitionEntities.Any(entity => NameEquals(entity.Name, template.Name)))
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

        // One partition read serves both the locate and the rename uniqueness
        // check; the target row and its siblings come from the same snapshot.
        var partitionEntities = await QueryEntitiesAsync(
            PartitionFilter(partitionKey),
            cancellationToken);

        var entity = partitionEntities.FirstOrDefault(
            candidate => string.Equals(candidate.RowKey, id, StringComparison.Ordinal));

        if (entity is null)
        {
            return UpdateTemplateResult.NotFound;
        }

        if (partitionEntities.Any(other =>
                !string.Equals(other.RowKey, id, StringComparison.Ordinal) &&
                NameEquals(other.Name, name)))
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

        var entities = await QueryEntitiesAsync(filter, cancellationToken);

        return TemplateViewMapper.ToViews(entities);
    }

    public async Task<TemplateView?> GetAsync(
        TemplateType type,
        string templateId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateId);

        try
        {
            var response = await tableClient.GetEntityIfExistsAsync<TemplateEntity>(
                TemplatePartitionKey.ForType(type),
                templateId,
                cancellationToken: cancellationToken);

            return response.HasValue
                ? TemplateViewMapper.ToView(response.Value!)
                : null;
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            // The table does not exist yet, so there is no template to return.
            return null;
        }
    }

    private async Task<List<TemplateEntity>> QueryEntitiesAsync(
        string? filter,
        CancellationToken cancellationToken)
    {
        var entities = new List<TemplateEntity>();

        try
        {
            await foreach (var entity in tableClient.QueryAsync<TemplateEntity>(
                filter,
                cancellationToken: cancellationToken))
            {
                entities.Add(entity);
            }
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            // The table does not exist yet, so there are no templates to return.
        }

        return entities;
    }

    // The partition key is a controlled value (templates-youtube or
    // templates-wordpress), so it is safe to embed directly in the filter.
    private static string PartitionFilter(string partitionKey) =>
        $"PartitionKey eq '{partitionKey}'";

    private static bool NameEquals(string left, string right) =>
        string.Equals(left, right, StringComparison.Ordinal);
}
