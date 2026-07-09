using System.Text.Json;
using YTSkedy.AzureFunctions.CalendarEvents;
using YTSkedy.AzureFunctions.Platforms;
using YTSkedy.AzureFunctions.Settings;
using YTSkedy.AzureFunctions.Templates;
using YTSkedy.Scheduling.Domain.Platforms;

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
              "texts": [
                {
                  "fieldKey": "text1",
                  "value": "English stream"
                },
                {
                  "fieldKey": "text2",
                  "value": "Live stream"
                }
              ]
            }
            """;

        var request = JsonSerializer.Deserialize<CreateCalendarEventRequest>(json, JsonOptions);

        Assert.NotNull(request);
        Assert.Equal(new DateTime(2026, 6, 15, 10, 0, 0), request.Start.LocalDateTime);
        Assert.Equal("America/Vancouver", request.Start.TimeZoneId);

        Assert.Collection(
            request.Texts,
            first =>
            {
                Assert.Equal("text1", first.FieldKey);
                Assert.Equal("English stream", first.Value);
            },
            second =>
            {
                Assert.Equal("text2", second.FieldKey);
                Assert.Equal("Live stream", second.Value);
            });
    }

    [Fact]
    public void UpdateCalendarEventRequest_InternalDto_DeserializesWithWebDefaults()
    {
        const string json = """
            {
              "start": {
                "localDateTime": "2026-07-20T09:30:00",
                "timeZoneId": "Europe/London"
              },
              "texts": [
                {
                  "fieldKey": "text1",
                  "value": "Updated English stream"
                },
                {
                  "fieldKey": "text2",
                  "value": "Updated live stream"
                }
              ]
            }
            """;

        var request = JsonSerializer.Deserialize<UpdateCalendarEventRequest>(json, JsonOptions);

        Assert.NotNull(request);
        Assert.Equal(new DateTime(2026, 7, 20, 9, 30, 0), request.Start.LocalDateTime);
        Assert.Equal("Europe/London", request.Start.TimeZoneId);

        Assert.Collection(
            request.Texts,
            first =>
            {
                Assert.Equal("text1", first.FieldKey);
                Assert.Equal("Updated English stream", first.Value);
            },
            second =>
            {
                Assert.Equal("text2", second.FieldKey);
                Assert.Equal("Updated live stream", second.Value);
            });
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
                "descriptionTemplateId": "description-template"
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
        Assert.Equal("description-template", request.PublishingContent.DescriptionTemplateId);
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
              "publishingContent": {
                "titleTemplateId": "title-template",
                "descriptionTemplateId": "description-template"
              },
              "publishSettings": {
                "siteUrl": "https://example.com",
                "username": "editor",
                "applicationPassword": "application-password",
                "postStatus": "future",
                "sticky": true,
                "scheduleOffsetHours": 25
              }
            }
            """;

        var request = JsonSerializer.Deserialize<CreatePlatformRequest>(json, JsonOptions);

        Assert.NotNull(request);
        Assert.Equal("Main WordPress site", request.Name);
        Assert.Equal("WordPress", request.Type);
        Assert.NotNull(request.PublishingContent);
        Assert.Equal("title-template", request.PublishingContent.TitleTemplateId);
        Assert.Equal("description-template", request.PublishingContent.DescriptionTemplateId);
        Assert.NotNull(request.PublishSettings);
        Assert.Equal("https://example.com", request.PublishSettings.SiteUrl);
        Assert.Equal("editor", request.PublishSettings.Username);
        Assert.Equal("application-password", request.PublishSettings.ApplicationPassword);
        Assert.Equal("future", request.PublishSettings.PostStatus);
        Assert.True(request.PublishSettings.Sticky);
        Assert.Equal(25, request.PublishSettings.ScheduleOffsetHours);
    }

    [Fact]
    public void UpdatePlatformRequest_InternalNestedDto_DeserializesWithWebDefaults()
    {
        const string json = """
            {
              "name": "Renamed YouTube channel",
              "referenceKey": "youTube1",
              "publishingContent": {
                "titleTemplateId": "title-template",
                "descriptionTemplateId": "description-template"
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

        var request = JsonSerializer.Deserialize<UpdatePlatformRequest>(json, JsonOptions);

        Assert.NotNull(request);
        Assert.Equal("Renamed YouTube channel", request.Name);
        Assert.Equal("youTube1", request.ReferenceKey);
        Assert.NotNull(request.PublishingContent);
        Assert.Equal("title-template", request.PublishingContent.TitleTemplateId);
        Assert.Equal("description-template", request.PublishingContent.DescriptionTemplateId);
        Assert.NotNull(request.PublishSettings);
        Assert.NotNull(request.PublishSettings.Credentials);
        Assert.Equal("client-id", request.PublishSettings.Credentials.ClientId);
        Assert.Equal("client-secret", request.PublishSettings.Credentials.ClientSecret);
        Assert.Equal("refresh-token", request.PublishSettings.Credentials.RefreshToken);
        Assert.Equal("private", request.PublishSettings.PrivacyStatus);
        Assert.False(request.PublishSettings.SelfDeclaredMadeForKids.GetValueOrDefault());
    }

    [Fact]
    public void UpdateEventTextFieldsRequest_InternalDto_DeserializesWithWebDefaults()
    {
        const string json = """
            {
              "fields": [
                {
                  "fieldKey": "text1",
                  "label": "Episode title",
                  "type": "ShortText",
                  "maxLength": 80
                }
              ]
            }
            """;

        var request = JsonSerializer.Deserialize<UpdateEventTextFieldsRequest>(json, JsonOptions);

        Assert.NotNull(request);
        Assert.NotNull(request.Fields);
        var field = Assert.Single(request.Fields);
        Assert.Equal("text1", field.FieldKey);
        Assert.Equal("Episode title", field.Label);
        Assert.Equal("ShortText", field.Type);
        Assert.Equal(80, field.MaxLength);
    }

    [Fact]
    public void PlatformResponse_YouTubeSettings_SerializesDisplayValuesAndExcludesRawSecrets()
    {
        object response = PlatformsApi.ToPlatformResponse(
            "yt-platform",
            "Main YouTube channel",
            PlatformType.YouTube,
            "main-channel",
            new YouTubeSettings(
                new YouTubeCredentials(
                    "client-id",
                    "stored-client-secret-A3B",
                    "stored-refresh-token-Z9Y"),
                "private",
                false),
            new PublishingContent(
                "title-template",
                "description-template"));

        var json = JsonSerializer.Serialize(response, JsonOptions);

        using var document = JsonDocument.Parse(json);
        var settings = document.RootElement.GetProperty("publishSettings");
        var credentials = settings.GetProperty("credentials");

        Assert.Equal("client-id", credentials.GetProperty("clientId").GetString());
        Assert.True(credentials.GetProperty("clientSecretConfigured").GetBoolean());
        Assert.True(credentials.GetProperty("refreshTokenConfigured").GetBoolean());
        Assert.Equal(
            "*********A3B",
            credentials.GetProperty("clientSecretDisplayValue").GetString());
        Assert.Equal(
            "*********Z9Y",
            credentials.GetProperty("refreshTokenDisplayValue").GetString());
        Assert.DoesNotContain("\"clientSecret\":\"", json);
        Assert.DoesNotContain("\"refreshToken\":\"", json);
        Assert.DoesNotContain("stored-client-secret-A3B", json);
        Assert.DoesNotContain("stored-refresh-token-Z9Y", json);
    }

    [Fact]
    public void PlatformResponse_WordPressSettings_SerializesDisplayValueAndExcludesRawSecret()
    {
        object response = PlatformsApi.ToPlatformResponse(
            "wp-platform",
            "Main WordPress site",
            PlatformType.WordPress,
            "company-blog",
            new WordPressSettings(
                "https://example.com",
                "editor",
                "local-test-password",
                WordPressSettings.ScheduledPostStatus,
                sticky: true,
                scheduleOffsetHours: 25),
            new PublishingContent(
                "title-template",
                "description-template"));

        var json = JsonSerializer.Serialize(response, JsonOptions);

        using var document = JsonDocument.Parse(json);
        var settings = document.RootElement.GetProperty("publishSettings");

        Assert.Equal("https://example.com", settings.GetProperty("siteUrl").GetString());
        Assert.Equal("company-blog", document.RootElement.GetProperty("referenceKey").GetString());
        var publishingContent = document.RootElement.GetProperty("publishingContent");
        Assert.Equal("title-template", publishingContent.GetProperty("titleTemplateId").GetString());
        Assert.Equal(
            "description-template",
            publishingContent.GetProperty("descriptionTemplateId").GetString());
        Assert.Equal("editor", settings.GetProperty("username").GetString());
        Assert.Equal(WordPressSettings.ScheduledPostStatus, settings.GetProperty("postStatus").GetString());
        Assert.True(settings.GetProperty("sticky").GetBoolean());
        Assert.Equal(25, settings.GetProperty("scheduleOffsetHours").GetInt32());
        Assert.True(settings.GetProperty("applicationPasswordConfigured").GetBoolean());
        Assert.Equal("*******", settings.GetProperty("passwordDisplayValue").GetString());
        Assert.DoesNotContain("applicationPassword\":\"", json);
        Assert.DoesNotContain("local-test-password", json);
    }

    [Fact]
    public void WriteRequestDtos_DoNotDeclareDisplayValueFields()
    {
        Assert.Null(typeof(PublishSettingsPayload).GetProperty("PasswordDisplayValue"));
        Assert.Null(typeof(PublishSettingsPayload).GetProperty("ClientSecretDisplayValue"));
        Assert.Null(typeof(PublishSettingsPayload).GetProperty("RefreshTokenDisplayValue"));
        Assert.Null(typeof(YouTubeCredentialsPayload).GetProperty("ClientSecretDisplayValue"));
        Assert.Null(typeof(YouTubeCredentialsPayload).GetProperty("RefreshTokenDisplayValue"));
    }

    [Fact]
    public void RenderedPublishingContentResponse_SerializesTypeWithWebDefaults()
    {
        object response = new RenderedPublishingContentResponse(
            "Preview",
            "Rendered title",
            "Rendered description");

        var json = JsonSerializer.Serialize(response, JsonOptions);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal("Preview", root.GetProperty("type").GetString());
        Assert.Equal("Rendered title", root.GetProperty("title").GetString());
        Assert.Equal("Rendered description", root.GetProperty("description").GetString());
        Assert.False(root.TryGetProperty("kind", out _));
    }

    [Fact]
    public void EventTextFieldsResponse_SerializesWithWebDefaults()
    {
        object response = new EventTextFieldsResponse(
            [
                new EventTextFieldResponse(
                    "text1",
                    "Episode title",
                    "ShortText",
                    80)
            ]);

        var json = JsonSerializer.Serialize(response, JsonOptions);

        using var document = JsonDocument.Parse(json);
        var field = document.RootElement.GetProperty("fields")[0];
        Assert.Equal("text1", field.GetProperty("fieldKey").GetString());
        Assert.Equal("Episode title", field.GetProperty("label").GetString());
        Assert.Equal("ShortText", field.GetProperty("type").GetString());
        Assert.Equal(80, field.GetProperty("maxLength").GetInt32());
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
                    "Live on {{ longDateEn }}")
            ]);

        var json = JsonSerializer.Serialize(response, JsonOptions);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var template = root.GetProperty("templates")[0];

        Assert.Equal("9f8b1c2d3e4f", template.GetProperty("id").GetString());
        Assert.Equal("Weeknight stream", template.GetProperty("name").GetString());
        Assert.Equal("YouTube", template.GetProperty("type").GetString());
        Assert.Equal("Live on {{ longDateEn }}", template.GetProperty("content").GetString());
    }
}
