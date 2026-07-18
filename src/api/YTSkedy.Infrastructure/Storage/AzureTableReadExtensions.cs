using Azure;
using Azure.Data.Tables;

namespace YTSkedy.Infrastructure.Storage;

internal static class AzureTableReadExtensions
{
    internal static async Task<TEntity?> GetEntityOrNullAsync<TEntity>(
        this TableClient tableClient,
        string partitionKey,
        string rowKey,
        CancellationToken cancellationToken)
        where TEntity : class, ITableEntity, new()
    {
        try
        {
            var response = await tableClient.GetEntityIfExistsAsync<TEntity>(
                partitionKey,
                rowKey,
                cancellationToken: cancellationToken);

            return response.HasValue ? response.Value : null;
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return null;
        }
    }

    internal static async Task<IReadOnlyList<TEntity>> ListEntitiesAsync<TEntity>(
        this TableClient tableClient,
        string? filter,
        IEnumerable<string>? select,
        CancellationToken cancellationToken)
        where TEntity : class, ITableEntity, new()
    {
        var entities = new List<TEntity>();

        try
        {
            await foreach (var entity in tableClient.QueryAsync<TEntity>(
                filter,
                select: select,
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

    internal static async Task<bool> AnyEntityAsync<TEntity>(
        this TableClient tableClient,
        string filter,
        CancellationToken cancellationToken)
        where TEntity : class, ITableEntity, new()
    {
        try
        {
            await foreach (var _ in tableClient.QueryAsync<TEntity>(
                filter,
                maxPerPage: 1,
                select: [nameof(ITableEntity.PartitionKey)],
                cancellationToken: cancellationToken))
            {
                return true;
            }
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return false;
        }

        return false;
    }
}
