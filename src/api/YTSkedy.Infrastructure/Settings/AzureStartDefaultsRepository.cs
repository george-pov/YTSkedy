using Azure;
using Azure.Data.Tables;
using YTSkedy.Scheduling.Application.Settings;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Infrastructure.Settings;

public sealed class AzureStartDefaultsRepository(TableClient tableClient) :
    IStartDefaultsReader,
    IStartDefaultsModifier
{
    public async Task<StartDefaults> GetAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await tableClient.GetEntityIfExistsAsync<ApplicationSettingsEntity>(
                ApplicationSettingsKey.PartitionKey,
                ApplicationSettingsKey.StartDefaultsRowKey,
                cancellationToken: cancellationToken);

            return response.HasValue
                ? StartDefaultsSerializer.Deserialize(response.Value!.ValueJson)
                : StartDefaults.Empty;
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return StartDefaults.Empty;
        }
    }

    public async Task SaveAsync(
        StartDefaults startDefaults,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(startDefaults);

        await tableClient.CreateIfNotExistsAsync(cancellationToken);
        await tableClient.UpsertEntityAsync(
            new ApplicationSettingsEntity
            {
                PartitionKey = ApplicationSettingsKey.PartitionKey,
                RowKey = ApplicationSettingsKey.StartDefaultsRowKey,
                ValueJson = StartDefaultsSerializer.Serialize(startDefaults)
            },
            TableUpdateMode.Replace,
            cancellationToken);
    }
}
