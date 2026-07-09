using System.Text.Json;
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
    public async Task SaveAsync_Fields_UpsertsOneGenericApplicationSettingsRow()
    {
        var tableClient = new ApplicationSettingsTableClient();
        var repository = new AzureEventTextFieldsRepository(tableClient);
        var settings = new EventTextFields(
            [
                new EventTextField("Title", EventTextType.ShortText, 80),
                new EventTextField("Details", EventTextType.LongText, 3000)
            ]);

        await repository.SaveAsync(settings, CancellationToken.None);

        var entity = Assert.Single(tableClient.Entities.Values);
        Assert.True(tableClient.CreateIfNotExistsCalled);
        Assert.Equal(ApplicationSettingsKey.PartitionKey, entity.PartitionKey);
        Assert.Equal(ApplicationSettingsKey.EventTextFieldsRowKey, entity.RowKey);
        Assert.Equal(["text1", "text2"], FieldKeys(entity.ValueJson));
    }

    [Fact]
    public async Task SaveAsync_ThenGetAsync_ReturnsSavedNormalizedFields()
    {
        var tableClient = new ApplicationSettingsTableClient();
        var repository = new AzureEventTextFieldsRepository(tableClient);
        var settings = new EventTextFields(
            [new EventTextField("Episode", EventTextType.ShortText, 120)]);

        await repository.SaveAsync(settings, CancellationToken.None);
        var read = await repository.GetAsync(CancellationToken.None);

        var field = Assert.Single(read.Fields);
        Assert.Equal("text1", field.FieldKey);
        Assert.Equal("Episode", field.Label);
        Assert.Equal(EventTextType.ShortText, field.Type);
        Assert.Equal(120, field.MaxLength);
    }

    private static string[] FieldKeys(string valueJson)
    {
        using var document = JsonDocument.Parse(valueJson);

        return document.RootElement
            .GetProperty("fields")
            .EnumerateArray()
            .Select(field => field.GetProperty("fieldKey").GetString() ?? string.Empty)
            .ToArray();
    }
}
