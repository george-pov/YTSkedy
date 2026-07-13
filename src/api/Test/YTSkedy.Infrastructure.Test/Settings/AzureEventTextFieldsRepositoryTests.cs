using YTSkedy.Infrastructure.Settings;
using YTSkedy.Infrastructure.Test.TestSupport;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Infrastructure.Test.Settings;

public sealed class AzureEventTextFieldsRepositoryTests
{
    [Fact]
    public async Task GetAsync_MissingRow_ReturnsDefaultFields()
    {
        var repository = new AzureEventTextFieldsRepository(new ApplicationSettingsTableClient());

        var eventTextFields = await repository.GetAsync(CancellationToken.None);

        Assert.Equal(["text1", "text2"], eventTextFields.Fields.Select(field => field.FieldKey));
        Assert.Equal(
            [EventTextType.ShortText, EventTextType.LongText],
            eventTextFields.Fields.Select(field => field.Type));
    }

    [Fact]
    public async Task GetAsync_StoredFields_ReturnsNormalizedFields()
    {
        var tableClient = new ApplicationSettingsTableClient();
        var repository = new AzureEventTextFieldsRepository(tableClient);
        var settings = new EventTextFields(
            [
                new EventTextField("Title", EventTextType.ShortText, 80),
                new EventTextField("Details", EventTextType.LongText, 3000)
            ]);

        tableClient.Seed(new ApplicationSettingsEntity
        {
            PartitionKey = ApplicationSettingsKey.PartitionKey,
            RowKey = ApplicationSettingsKey.EventTextFieldsRowKey,
            ValueJson = EventTextFieldsSerializer.Serialize(settings)
        });

        var read = await repository.GetAsync(CancellationToken.None);

        Assert.Equal(["text1", "text2"], read.Fields.Select(field => field.FieldKey));
        Assert.Equal(["Title", "Details"], read.Fields.Select(field => field.Label));
    }
}
