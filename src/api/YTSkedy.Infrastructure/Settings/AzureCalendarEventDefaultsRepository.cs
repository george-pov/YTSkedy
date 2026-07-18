using Azure.Data.Tables;
using YTSkedy.Infrastructure.Storage;
using YTSkedy.Scheduling.Application.Settings;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Infrastructure.Settings;

public sealed class AzureCalendarEventDefaultsRepository(TableClient tableClient) :
    ICalendarEventDefaultsModifier,
    IEventTextFieldsReader,
    IStartDefaultsReader
{
    Task<EventTextFields> IEventTextFieldsReader.GetAsync(
        CancellationToken cancellationToken) =>
        GetEventTextFieldsAsync(cancellationToken);

    Task<StartDefaults> IStartDefaultsReader.GetAsync(
        CancellationToken cancellationToken) =>
        GetStartDefaultsAsync(cancellationToken);

    public async Task SaveAsync(
        CalendarEventDefaults defaults,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(defaults);

        TableTransactionAction[] actions =
        [
            new(
                TableTransactionActionType.UpsertReplace,
                new ApplicationSettingsEntity
                {
                    PartitionKey = ApplicationSettingsKey.PartitionKey,
                    RowKey = ApplicationSettingsKey.EventTextFieldsRowKey,
                    ValueJson = EventTextFieldsSerializer.Serialize(defaults.EventTextFields)
                }),
            new(
                TableTransactionActionType.UpsertReplace,
                new ApplicationSettingsEntity
                {
                    PartitionKey = ApplicationSettingsKey.PartitionKey,
                    RowKey = ApplicationSettingsKey.StartDefaultsRowKey,
                    ValueJson = StartDefaultsSerializer.Serialize(defaults.StartDefaults)
                })
        ];

        await tableClient.SubmitTransactionAsync(actions, cancellationToken);
    }

    private async Task<EventTextFields> GetEventTextFieldsAsync(
        CancellationToken cancellationToken)
    {
        var entity = await tableClient.GetEntityOrNullAsync<ApplicationSettingsEntity>(
            ApplicationSettingsKey.PartitionKey,
            ApplicationSettingsKey.EventTextFieldsRowKey,
            cancellationToken);

        return entity is null
            ? EventTextFields.Default
            : EventTextFieldsSerializer.Deserialize(entity.ValueJson);
    }

    private async Task<StartDefaults> GetStartDefaultsAsync(
        CancellationToken cancellationToken)
    {
        var entity = await tableClient.GetEntityOrNullAsync<ApplicationSettingsEntity>(
            ApplicationSettingsKey.PartitionKey,
            ApplicationSettingsKey.StartDefaultsRowKey,
            cancellationToken);

        return entity is null
            ? StartDefaults.Empty
            : StartDefaultsSerializer.Deserialize(entity.ValueJson);
    }
}
