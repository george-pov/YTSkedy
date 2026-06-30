using System.Text.Json;
using YTSkedy.Infrastructure.Settings;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Infrastructure.Test.Settings;

public sealed class EventTextFieldsSerializerTests
{
    [Fact]
    public void Serialize_EventTextFields_WritesFieldDefinitions()
    {
        var eventTextFields = new EventTextFields(
            [
                new EventTextField("old1", "Title", EventTextType.ShortText, 80),
                new EventTextField("old2", "Body", EventTextType.LongText, 3000)
            ]);

        var json = EventTextFieldsSerializer.Serialize(eventTextFields);

        using var document = JsonDocument.Parse(json);
        var fields = document.RootElement.GetProperty("fields");
        Assert.Equal("text1", fields[0].GetProperty("fieldKey").GetString());
        Assert.Equal("Title", fields[0].GetProperty("label").GetString());
        Assert.Equal("ShortText", fields[0].GetProperty("type").GetString());
        Assert.Equal(80, fields[0].GetProperty("maxLength").GetInt32());
        Assert.Equal("text2", fields[1].GetProperty("fieldKey").GetString());
        Assert.Equal("LongText", fields[1].GetProperty("type").GetString());
    }

    [Fact]
    public void Deserialize_StoredFields_NormalizesKeysFromOrder()
    {
        const string json = """
            {
              "fields": [
                {
                  "fieldKey": "text1",
                  "label": "Title",
                  "type": "ShortText",
                  "maxLength": 50
                },
                {
                  "fieldKey": "text3",
                  "label": "Details",
                  "type": "LongText",
                  "maxLength": 2500
                }
              ]
            }
            """;

        var eventTextFields = EventTextFieldsSerializer.Deserialize(json);

        Assert.Equal(["text1", "text2"], eventTextFields.Fields.Select(field => field.FieldKey));
        Assert.Equal(["Title", "Details"], eventTextFields.Fields.Select(field => field.Label));
    }

    [Fact]
    public void Deserialize_InvalidType_ThrowsInvalidOperationException()
    {
        const string json = """
            {
              "fields": [
                {
                  "fieldKey": "text1",
                  "label": "Title",
                  "type": "Unknown",
                  "maxLength": 50
                }
              ]
            }
            """;

        Assert.Throws<InvalidOperationException>(
            () => EventTextFieldsSerializer.Deserialize(json));
    }
}
