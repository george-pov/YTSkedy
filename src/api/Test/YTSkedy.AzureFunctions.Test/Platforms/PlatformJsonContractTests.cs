using System.Text.Json;
using YTSkedy.AzureFunctions.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;
using static YTSkedy.AzureFunctions.Test.Platforms.PlatformTestData;

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
            WordPressSettings(postStatus: "draft"),
            RequiredPublishingContent());

        var json = JsonSerializer.Serialize(response, JsonOptions);

        using var document = JsonDocument.Parse(json);
        Assert.Equal("company-blog", document.RootElement.GetProperty("referenceKey").GetString());
        Assert.Equal(
            "https://example.com",
            document.RootElement.GetProperty("publishSettings").GetProperty("siteUrl").GetString());
        Assert.DoesNotContain("applicationPassword\":\"", json);
        Assert.DoesNotContain("application-password", json);
    }
}
