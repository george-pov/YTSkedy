using Azure.Data.Tables;
using YTSkedy.Scheduling.Application.Settings;

namespace YTSkedy.Infrastructure.Settings;

public sealed class AzureCalendarEventDefaultsRepository(TableClient tableClient) :
    ICalendarEventDefaultsModifier
{
    public async Task SaveAsync(
        CalendarEventDefaults defaults,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(defaults);

        await tableClient.CreateIfNotExistsAsync(cancellationToken);

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
}
