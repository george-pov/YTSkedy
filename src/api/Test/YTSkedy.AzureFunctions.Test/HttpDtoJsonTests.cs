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
              "referenceKey": "youTube1",
              "publishingContent": {
                "titleTemplateId": "title-template",
                "descriptionTemplateId": null
              },
              "publishSettings": {
                "credentials": {
                  "clientId": "client-id",
                  "clientSecret": "client-secret",
                  "refreshToken": "refresh-token"
                },
                "privacyStatus": "private",
                "selfDeclaredMadeForKids": false
              }
            }
            """;

        var request = JsonSerializer.Deserialize<CreatePlatformRequest>(json, JsonOptions);

        Assert.NotNull(request);
        Assert.Equal("Main YouTube channel", request.Name);
        Assert.Equal("YouTube", request.Type);
        Assert.Equal("youTube1", request.ReferenceKey);
        Assert.NotNull(request.PublishingContent);
        Assert.Equal("title-template", request.PublishingContent.TitleTemplateId);
        Assert.Null(request.PublishingContent.DescriptionTemplateId);
        Assert.NotNull(request.PublishSettings);
        Assert.NotNull(request.PublishSettings.Credentials);
        Assert.Equal("client-id", request.PublishSettings.Credentials.ClientId);
        Assert.Equal("client-secret", request.PublishSettings.Credentials.ClientSecret);
        Assert.Equal("refresh-token", request.PublishSettings.Credentials.RefreshToken);
        Assert.Equal("private", request.PublishSettings.PrivacyStatus);
        Assert.False(request.PublishSettings.SelfDeclaredMadeForKids.GetValueOrDefault());
    }

    [Fact]
    public void CreatePlatformRequest_WordPressNestedDto_DeserializesWithWebDefaults()
    {
        const string json = """
            {
              "name": "Main WordPress site",
              "type": "WordPress",
              "publishSettings": {
                "siteUrl": "https://example.com",
                "username": "editor",
                "applicationPassword": "application-password",
                "postStatus": "publish"
              }
            }
            """;

        var request = JsonSerializer.Deserialize<CreatePlatformRequest>(json, JsonOptions);

        Assert.NotNull(request);
        Assert.Equal("Main WordPress site", request.Name);
        Assert.Equal("WordPress", request.Type);
        Assert.NotNull(request.PublishSettings);
        Assert.Equal("https://example.com", request.PublishSettings.SiteUrl);
        Assert.Equal("editor", request.PublishSettings.Username);
        Assert.Equal("application-password", request.PublishSettings.ApplicationPassword);
        Assert.Equal("publish", request.PublishSettings.PostStatus);
    }

    [Fact]
    public void PlatformResponse_WordPressSettings_SerializesRedactedSettings()
    {
        object response = new PlatformResponse(
            "wp-platform",
            "Main WordPress site",
            "company-blog",
            "WordPress",
            new PublishSettingsResponse(
                null,
                null,
                null,
                "https://example.com",
                "editor",
                "publish",
                true),
            new YTSkedy.AzureFunctions.Platforms.PublishingContentResponse(
                "title-template",
                null));

        var json = JsonSerializer.Serialize(response, JsonOptions);

        using var document = JsonDocument.Parse(json);
        var settings = document.RootElement.GetProperty("publishSettings");

        Assert.Equal("https://example.com", settings.GetProperty("siteUrl").GetString());
        Assert.Equal("company-blog", document.RootElement.GetProperty("referenceKey").GetString());
        var publishingContent = document.RootElement.GetProperty("publishingContent");
        Assert.Equal("title-template", publishingContent.GetProperty("titleTemplateId").GetString());
        Assert.True(publishingContent.GetProperty("descriptionTemplateId").ValueKind == JsonValueKind.Null);
        Assert.Equal("editor", settings.GetProperty("username").GetString());
        Assert.Equal("publish", settings.GetProperty("postStatus").GetString());
        Assert.True(settings.GetProperty("applicationPasswordConfigured").GetBoolean());
        Assert.DoesNotContain("applicationPassword\":\"", json);
        Assert.DoesNotContain("application-password", json);
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
                    "Live on {{ longDate }}")
            ]);

        var json = JsonSerializer.Serialize(response, JsonOptions);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var template = root.GetProperty("templates")[0];

        Assert.Equal("9f8b1c2d3e4f", template.GetProperty("id").GetString());
        Assert.Equal("Weeknight stream", template.GetProperty("name").GetString());
        Assert.Equal("YouTube", template.GetProperty("type").GetString());
        Assert.Equal("Live on {{ longDate }}", template.GetProperty("content").GetString());
    }
}
