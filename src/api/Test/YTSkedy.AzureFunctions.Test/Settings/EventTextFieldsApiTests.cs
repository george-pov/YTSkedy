using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using YTSkedy.AzureFunctions.Settings;
using YTSkedy.Scheduling.Application.Settings;
using YTSkedy.Scheduling.Domain.CalendarEvents;
using YTSkedy.TestSupport;

namespace YTSkedy.AzureFunctions.Test.Settings;

public sealed class EventTextFieldsApiTests
{
    [Fact]
    public async Task Get_DefaultSettings_ReturnsDefaultFields()
    {
        var api = CreateApi(new FakeEventTextFieldsStore(EventTextFields.Default));

        var actionResult = await api.Get(
            new DefaultHttpContext().Request,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(actionResult);
        var response = Assert.IsType<EventTextFieldsResponse>(ok.Value);
        Assert.Equal(["text1", "text2"], response.Fields.Select(field => field.FieldKey));
        Assert.Equal(["ShortText", "LongText"], response.Fields.Select(field => field.Type));
    }

    [Fact]
    public async Task Update_ValidRequest_SavesAndReturnsNormalizedFields()
    {
        var store = new FakeEventTextFieldsStore(EventTextFields.Default);
        var api = CreateApi(store);
        var request = HttpRequestFactory.WithBody("""
            {
              "fields": [
                {
                  "fieldKey": "text7",
                  "label": "Episode",
                  "type": "ShortText",
                  "maxLength": 80
                },
                {
                  "fieldKey": "text9",
                  "label": "Notes",
                  "type": "LongText",
                  "maxLength": 1000
                }
              ]
            }
            """);

        var actionResult = await api.Update(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(actionResult);
        var response = Assert.IsType<EventTextFieldsResponse>(ok.Value);
        Assert.NotNull(store.Saved);
        Assert.Equal(["text1", "text2"], store.Saved!.Fields.Select(field => field.FieldKey));
        Assert.Equal(["text1", "text2"], response.Fields.Select(field => field.FieldKey));
        Assert.Equal(["Episode", "Notes"], response.Fields.Select(field => field.Label));
    }

    [Theory]
    [InlineData("""{ "fields": [] }""")]
    [InlineData("""{ "fields": [{ "fieldKey": "text1", "label": "", "type": "ShortText", "maxLength": 50 }] }""")]
    [InlineData("""{ "fields": [{ "fieldKey": "text1", "label": "Title", "type": "Unknown", "maxLength": 50 }] }""")]
    [InlineData("""{ "fields": [{ "fieldKey": "text1", "label": "Title", "type": "ShortText", "maxLength": 0 }] }""")]
    public async Task Update_InvalidFieldList_ReturnsBadRequest(string json)
    {
        var api = CreateApi(new FakeEventTextFieldsStore(EventTextFields.Default));

        var actionResult = await api.Update(
            HttpRequestFactory.WithBody(json),
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(actionResult);
    }

    [Fact]
    public void TryBuildUpdateCommand_ValidRequest_BuildsCommand()
    {
        var request = new UpdateEventTextFieldsRequest(
            [
                new UpdateEventTextFieldRequest("text1", "Title", "ShortText", 50),
                new UpdateEventTextFieldRequest("text2", "Description", "LongText", 2500)
            ]);

        var built = EventTextFieldsApi.TryBuildUpdateCommand(request, out var command, out _);

        Assert.True(built);
        Assert.Equal(["Title", "Description"], command.Fields.Select(field => field.Label));
    }

    [Fact]
    public void ToResponse_EventTextFields_MapsEveryField()
    {
        var eventTextFields = new EventTextFields(
            [
                new EventTextField("Episode", EventTextType.ShortText, 80),
                new EventTextField("Body", EventTextType.LongText, 1000)
            ]);

        var response = EventTextFieldsApi.ToResponse(eventTextFields);

        Assert.Collection(
            response.Fields,
            first =>
            {
                Assert.Equal("text1", first.FieldKey);
                Assert.Equal("Episode", first.Label);
                Assert.Equal("ShortText", first.Type);
                Assert.Equal(80, first.MaxLength);
            },
            second =>
            {
                Assert.Equal("text2", second.FieldKey);
                Assert.Equal("Body", second.Label);
                Assert.Equal("LongText", second.Type);
                Assert.Equal(1000, second.MaxLength);
            });
    }

    private static EventTextFieldsApi CreateApi(FakeEventTextFieldsStore store) =>
        new(
            new GetEventTextFieldsHandler(store),
            new UpdateEventTextFieldsHandler(store));

    private sealed class FakeEventTextFieldsStore(EventTextFields current) :
        IEventTextFieldsReader,
        IEventTextFieldsModifier
    {
        public EventTextFields? Saved { get; private set; }

        public Task<EventTextFields> GetAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Saved ?? current);

        public Task SaveAsync(
            EventTextFields eventTextFields,
            CancellationToken cancellationToken)
        {
            Saved = eventTextFields;

            return Task.CompletedTask;
        }
    }
}
