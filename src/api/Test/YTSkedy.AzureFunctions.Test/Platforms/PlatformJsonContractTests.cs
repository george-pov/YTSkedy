using System.Text.Json;
using YTSkedy.AzureFunctions.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;
using static YTSkedy.AzureFunctions.Test.Platforms.PlatformTestData;
using DomainWordPressSettings = YTSkedy.Scheduling.Domain.Platforms.WordPressSettings;

namespace YTSkedy.AzureFunctions.Test.Platforms;

public sealed class PlatformJsonContractTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void PlatformResponse_WithReferenceKey_SerializesCamelCaseAndRedactsSecrets()
    {
        var response = PlatformsApi.ToPlatformResponse(
            "wp-platform",
            "Main WordPress site",
            PlatformType.WordPress,
            "company-blog",
            WordPressSettings(
                postStatus: DomainWordPressSettings.ScheduledPostStatus,
                categoryIds: [12, 34],
                sticky: true,
                scheduleOffsetHours: 25),
            RequiredPublishingContent());

        var json = JsonSerializer.Serialize(response, JsonOptions);

        using var document = JsonDocument.Parse(json);
        Assert.Equal("company-blog", document.RootElement.GetProperty("referenceKey").GetString());
        Assert.Equal(
            "https://example.com",
            document.RootElement.GetProperty("publishSettings").GetProperty("siteUrl").GetString());
        Assert.Equal(
            DomainWordPressSettings.ScheduledPostStatus,
            document.RootElement.GetProperty("publishSettings").GetProperty("postStatus").GetString());
        Assert.True(document.RootElement.GetProperty("publishSettings").GetProperty("sticky").GetBoolean());
        Assert.Equal(
            25,
            document.RootElement.GetProperty("publishSettings").GetProperty("scheduleOffsetHours").GetInt32());
        Assert.Equal(
            [12, 34],
            document.RootElement.GetProperty("publishSettings")
                .GetProperty("categoryIds")
                .EnumerateArray()
                .Select(item => item.GetInt64()));
        Assert.DoesNotContain("applicationPassword\":\"", json);
        Assert.DoesNotContain("application-password", json);
    }
}
