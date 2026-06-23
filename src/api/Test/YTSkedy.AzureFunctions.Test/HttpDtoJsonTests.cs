using System.Text.Json;
using YTSkedy.AzureFunctions.CalendarEvents;
using YTSkedy.AzureFunctions.Platforms;
using YTSkedy.AzureFunctions.Templates;

namespace YTSkedy.AzureFunctions.Test;

public sealed class HttpDtoJsonTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void CreateCalendarEventRequest_InternalDto_DeserializesWithWebDefaults()
    {
        const string json = """
            {
              "start": {
                "localDateTime": "2026-06-15T10:00:00",
                "timeZoneId": "America/Vancouver"
              },
              "descriptions": [
                {
                  "language": "en",
                  "title": "English stream",
                  "description": "Live stream"
                }
              ]
            }
            """;

        var request = JsonSerializer.Deserialize<CreateCalendarEventRequest>(json, JsonOptions);

        Assert.NotNull(request);
        Assert.Equal(new DateTime(2026, 6, 15, 10, 0, 0), request.Start.LocalDateTime);
        Assert.Equal("America/Vancouver", request.Start.TimeZoneId);

        var description = Assert.Single(request.Descriptions);
        Assert.Equal("en", description.Language);
        Assert.Equal("English stream", description.Title);
        Assert.Equal("Live stream", description.Description);
    }

    [Fact]
    public void CreatePlatformRequest_InternalNestedDto_DeserializesWithWebDefaults()
    {
        const string json = """
            {
              "name": "Main YouTube channel",
              "type": "YouTube",
              "publishSettings": {
                "credentials": "main-channel",
                "privacyStatus": "private",
                "selfDeclaredMadeForKids": false
              }
            }
            """;

        var request = JsonSerializer.Deserialize<CreatePlatformRequest>(json, JsonOptions);

        Assert.NotNull(request);
        Assert.Equal("Main YouTube channel", request.Name);
        Assert.Equal("YouTube", request.Type);
        Assert.NotNull(request.PublishSettings);
        Assert.Equal("main-channel", request.PublishSettings.Credentials);
        Assert.Equal("private", request.PublishSettings.PrivacyStatus);
        Assert.False(request.PublishSettings.SelfDeclaredMadeForKids);
    }

    [Fact]
    public void ResponseDto_InternalRuntimeType_SerializesWithWebDefaults()
    {
        object response = new TemplateListResponse(
            [
                new TemplateResponse(
                    "9f8b1c2d3e4f",
                    "Weeknight stream",
                    "YouTube",
                    "Live at {{ localizedTime }}")
            ]);

        var json = JsonSerializer.Serialize(response, JsonOptions);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var template = root.GetProperty("templates")[0];

        Assert.Equal("9f8b1c2d3e4f", template.GetProperty("id").GetString());
        Assert.Equal("Weeknight stream", template.GetProperty("name").GetString());
        Assert.Equal("YouTube", template.GetProperty("type").GetString());
        Assert.Equal("Live at {{ localizedTime }}", template.GetProperty("content").GetString());
    }
}
