using Azure;
using Azure.Data.Tables;
using YTSkedy.Scheduling.Application.Settings;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Infrastructure.Settings;

public sealed class AzureEventTextFieldsRepository(TableClient tableClient) :
    IEventTextFieldsReader,
    IEventTextFieldsModifier
{
    public async Task<EventTextFields> GetAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await tableClient.GetEntityIfExistsAsync<ApplicationSettingsEntity>(
                ApplicationSettingsKey.PartitionKey,
                ApplicationSettingsKey.EventTextFieldsRowKey,
                cancellationToken: cancellationToken);

            return response.HasValue
                ? EventTextFieldsSerializer.Deserialize(response.Value!.ValueJson)
                : EventTextFields.Default;
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            // The generic settings table has not been created yet.
            return EventTextFields.Default;
        }
    }

    public async Task SaveAsync(
        EventTextFields eventTextFields,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(eventTextFields);

        await tableClient.CreateIfNotExistsAsync(cancellationToken);

        var entity = new ApplicationSettingsEntity
        {
            PartitionKey = ApplicationSettingsKey.PartitionKey,
            RowKey = ApplicationSettingsKey.EventTextFieldsRowKey,
            ValueJson = EventTextFieldsSerializer.Serialize(eventTextFields)
        };

        await tableClient.UpsertEntityAsync(
            entity,
            TableUpdateMode.Replace,
            cancellationToken);
    }
}
