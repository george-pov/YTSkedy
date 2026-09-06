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
    public void PublicationActionErrorResponse_SerializesStableCodeAndMessage()
    {
        var response = new PublicationActionErrorResponse(
            "publication_target_mismatch",
            "Restore the original target.");

        var json = JsonSerializer.Serialize(response, JsonOptions);
        using var document = JsonDocument.Parse(json);

        Assert.Equal(
            "publication_target_mismatch",
            document.RootElement.GetProperty("code").GetString());
        Assert.Equal(
            "Restore the original target.",
            document.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public void PublicationActionErrorResponse_SerializesPublishDiagnostics()
    {
        var retryAfterUtc = new DateTimeOffset(2026, 9, 6, 8, 1, 0, TimeSpan.Zero);
        var response = new PublicationActionErrorResponse(
            "wordpress_rate_limited",
            "WordPress limited publishing requests.",
            "create_post",
            429,
            "imunify_rate_limited",
            retryAfterUtc,
            "attempt-id",
            VerificationRequired: true);

        var json = JsonSerializer.Serialize(response, JsonOptions);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal("wordpress_rate_limited", root.GetProperty("code").GetString());
        Assert.Equal("create_post", root.GetProperty("stage").GetString());
        Assert.Equal(429, root.GetProperty("providerStatus").GetInt32());
        Assert.Equal("imunify_rate_limited", root.GetProperty("providerErrorCode").GetString());
        Assert.Equal(retryAfterUtc, root.GetProperty("retryAfterUtc").GetDateTimeOffset());
        Assert.Equal("attempt-id", root.GetProperty("attemptId").GetString());
        Assert.True(root.GetProperty("verificationRequired").GetBoolean());
    }

    [Fact]
    public void EventPlatformResponse_SerializesRecoveryFieldsAdditively()
    {
        var updatedUtc = new DateTimeOffset(2026, 7, 15, 20, 0, 0, TimeSpan.Zero);
        var response = new EventPlatformResponse(
            "platform-id",
            "Main channel",
            "YouTube",
            "Publishing",
            null,
            "NotConfigured",
            null,
            updatedUtc,
            null,
            CanPublish: false,
            CanDeletePublication: false,
            CanPreviewPublishingContent: true,
            CanRecoverPublication: true);

        var json = JsonSerializer.Serialize(response, JsonOptions);
        using var document = JsonDocument.Parse(json);

        Assert.Equal(
            updatedUtc,
            document.RootElement.GetProperty("publicationUpdatedUtc").GetDateTimeOffset());
        Assert.True(document.RootElement.GetProperty("canRecoverPublication").GetBoolean());
        Assert.Equal("Publishing", document.RootElement.GetProperty("status").GetString());
    }

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
                "selfDeclaredMadeForKids": false,
                "categoryId": "27",
                "containsSyntheticMedia": true,
                "defaultAudioLanguage": "en-US",
                "defaultLanguage": "ru"
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
        Assert.Equal("27", request.PublishSettings.CategoryId);
        Assert.True(request.PublishSettings.ContainsSyntheticMedia);
        Assert.Equal("en-US", request.PublishSettings.DefaultAudioLanguage);
        Assert.Equal("ru", request.PublishSettings.DefaultLanguage);
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
                "scheduleOffsetHours": 25,
                "categoryIds": [12, 34]
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
        Assert.Equal([12, 34], request.PublishSettings.CategoryIds);
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
        Assert.Null(request.PublishSettings.CategoryId);
        Assert.False(request.PublishSettings.ContainsSyntheticMedia.GetValueOrDefault());
    }

    [Fact]
    public void UpdateCalendarEventDefaultsRequest_InternalDto_DeserializesWithWebDefaults()
    {
        const string json = """
            {
              "eventTextFields": {
                "fields": [
                  {
                    "fieldKey": "text1",
                    "label": "Episode title",
                    "type": "ShortText",
                    "maxLength": 80
                  }
                ]
              },
              "startDefaults": {
                "dayOfWeek": "Sunday",
                "localTime": "10:00",
                "timeZoneId": "America/Vancouver"
              }
            }
            """;

        var request = JsonSerializer.Deserialize<UpdateCalendarEventDefaultsRequest>(
            json,
            JsonOptions);

        Assert.NotNull(request);
        Assert.NotNull(request.EventTextFields);
        Assert.NotNull(request.EventTextFields.Fields);
        var field = Assert.Single(request.EventTextFields.Fields);
        Assert.Equal("text1", field.FieldKey);
        Assert.Equal("Episode title", field.Label);
        Assert.Equal("ShortText", field.Type);
        Assert.Equal(80, field.MaxLength);
        Assert.NotNull(request.StartDefaults);
        Assert.Equal("Sunday", request.StartDefaults.DayOfWeek);
        Assert.Equal("10:00", request.StartDefaults.LocalTime);
        Assert.Equal("America/Vancouver", request.StartDefaults.TimeZoneId);
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
                false,
                categoryId: null,
                containsSyntheticMedia: false,
                defaultAudioLanguage: "en-US",
                defaultLanguage: "ru"),
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
        Assert.Equal(JsonValueKind.Null, settings.GetProperty("categoryId").ValueKind);
        Assert.False(settings.GetProperty("containsSyntheticMedia").GetBoolean());
        Assert.Equal("en-US", settings.GetProperty("defaultAudioLanguage").GetString());
        Assert.Equal("ru", settings.GetProperty("defaultLanguage").GetString());
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
                [12, 34],
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
        Assert.Equal(
            [12, 34],
            settings.GetProperty("categoryIds").EnumerateArray().Select(item => item.GetInt64()));
        Assert.True(settings.GetProperty("applicationPasswordConfigured").GetBoolean());
        Assert.Equal("*******", settings.GetProperty("passwordDisplayValue").GetString());
        Assert.DoesNotContain("applicationPassword\":\"", json);
        Assert.DoesNotContain("local-test-password", json);
        Assert.False(settings.TryGetProperty("categoryId", out _));
        Assert.False(settings.TryGetProperty("containsSyntheticMedia", out _));
        Assert.False(settings.TryGetProperty("defaultAudioLanguage", out _));
        Assert.False(settings.TryGetProperty("defaultLanguage", out _));
    }

    [Fact]
    public void WriteRequestDtos_DoNotDeclareDisplayValueFields()
    {
        Assert.Null(typeof(PublishSettingsRequest).GetProperty("PasswordDisplayValue"));
        Assert.Null(typeof(PublishSettingsRequest).GetProperty("ClientSecretDisplayValue"));
        Assert.Null(typeof(PublishSettingsRequest).GetProperty("RefreshTokenDisplayValue"));
        Assert.Null(typeof(YouTubeCredentialsRequest).GetProperty("ClientSecretDisplayValue"));
        Assert.Null(typeof(YouTubeCredentialsRequest).GetProperty("RefreshTokenDisplayValue"));
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
    public void CalendarEventDefaultsResponse_SerializesWithWebDefaults()
    {
        object response = new CalendarEventDefaultsResponse(
            new EventTextFieldsResponse(
                [
                    new EventTextFieldResponse(
                        "text1",
                        "Episode title",
                        "ShortText",
                        80)
                ]),
            new StartDefaultsResponse(
                "Sunday",
                "10:00",
                "America/Vancouver"));

        var json = JsonSerializer.Serialize(response, JsonOptions);

        using var document = JsonDocument.Parse(json);
        var field = document.RootElement.GetProperty("eventTextFields").GetProperty("fields")[0];
        Assert.Equal("text1", field.GetProperty("fieldKey").GetString());
        Assert.Equal("Episode title", field.GetProperty("label").GetString());
        Assert.Equal("ShortText", field.GetProperty("type").GetString());
        Assert.Equal(80, field.GetProperty("maxLength").GetInt32());
        var startDefaults = document.RootElement.GetProperty("startDefaults");
        Assert.Equal("Sunday", startDefaults.GetProperty("dayOfWeek").GetString());
        Assert.Equal("10:00", startDefaults.GetProperty("localTime").GetString());
        Assert.Equal(
            "America/Vancouver",
            startDefaults.GetProperty("timeZoneId").GetString());
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
