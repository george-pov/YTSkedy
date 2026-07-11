using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using YTSkedy.AzureFunctions.Platforms;
using YTSkedy.Scheduling.Application.Platforms.WordPressCategory;
using YTSkedy.TestSupport;

namespace YTSkedy.AzureFunctions.Test.Platforms;

public sealed class WordPressCategoryApiResultTests
{
    private const string PlatformId = "wp-platform";

    [Fact]
    public void ToResult_Listed_ReturnsOrderedCamelCaseResponse()
    {
        var result = CategoryListResult.Listed(
            new CategoryPage(
                [
                    new CategoryView(34, "News", "news"),
                    new CategoryView(12, "Events", "events")
                ],
                2,
                25,
                26,
                2));

        var actionResult = WordPressCategoryApi.ToResult(result, PlatformId);

        var ok = Assert.IsType<OkObjectResult>(actionResult);
        var response = Assert.IsType<WordPressCategoryListResponse>(ok.Value);
        Assert.Equal([34, 12], response.Items.Select(item => item.Id));
        Assert.Equal(2, response.Page);
        Assert.Equal(25, response.PageSize);
        Assert.Equal(26, response.Total);
        Assert.Equal(2, response.TotalPages);
        var json = JsonSerializer.Serialize(
            response,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Contains("\"items\"", json);
        Assert.Contains("\"pageSize\"", json);
        Assert.Contains("\"totalPages\"", json);
        Assert.DoesNotContain("\"Items\"", json);
    }

    [Theory]
    [InlineData(CategoryListStatus.PlatformNotFound, StatusCodes.Status404NotFound)]
    [InlineData(CategoryListStatus.InvalidPlatformType, StatusCodes.Status409Conflict)]
    [InlineData(CategoryListStatus.ProviderFailed, StatusCodes.Status502BadGateway)]
    public void ToResult_Failure_MapsExpectedStatus(
        CategoryListStatus status,
        int expectedStatus)
    {
        var actionResult = WordPressCategoryApi.ToResult(
            CategoryListResult.ForStatus(status),
            PlatformId);

        Assert.Equal(expectedStatus, ActionResultAssertions.StatusCode(actionResult));
    }

    [Fact]
    public void ToResult_ProviderFailed_ReturnsFixedSecretSafeMessage()
    {
        var actionResult = WordPressCategoryApi.ToResult(
            CategoryListResult.ForStatus(CategoryListStatus.ProviderFailed),
            PlatformId);

        var result = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal("WordPress categories could not be loaded.", result.Value);
        Assert.DoesNotContain("password", result.Value!.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Basic", result.Value!.ToString());
    }
}
