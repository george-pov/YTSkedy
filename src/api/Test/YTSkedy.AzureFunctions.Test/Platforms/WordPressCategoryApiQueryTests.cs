using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using YTSkedy.AzureFunctions.Platforms;
using YTSkedy.Scheduling.Application.Platforms.WordPressCategory;

namespace YTSkedy.AzureFunctions.Test.Platforms;

public sealed class WordPressCategoryApiQueryTests
{
    private const string PlatformId = "wp platform/1";

    [Fact]
    public void TryBuildQuery_NoValues_UsesDefaults()
    {
        var built = TryBuildQuery("", out var query, out var error);

        Assert.True(built);
        Assert.IsType<EmptyResult>(error);
        Assert.Equal(PlatformId, query.PlatformId);
        Assert.Null(query.Search);
        Assert.Empty(query.IncludeIds);
        Assert.Equal(1, query.Page);
        Assert.Equal(25, query.PageSize);
    }

    [Fact]
    public void TryBuildQuery_Search_TrimsAndForwardsPaging()
    {
        var built = TryBuildQuery(
            "?search=%20live%20events%20&page=2&pageSize=100",
            out var query,
            out _);

        Assert.True(built);
        Assert.Equal("live events", query.Search);
        Assert.Empty(query.IncludeIds);
        Assert.Equal(2, query.Page);
        Assert.Equal(100, query.PageSize);
    }

    [Fact]
    public void TryBuildQuery_IncludeIds_PreservesOrder()
    {
        var built = TryBuildQuery(
            "?includeIds=34%2C12&pageSize=50",
            out var query,
            out _);

        Assert.True(built);
        Assert.Null(query.Search);
        Assert.Equal([34, 12], query.IncludeIds);
        Assert.Equal(50, query.PageSize);
    }

    [Theory]
    [InlineData("?search=events&includeIds=12")]
    [InlineData("?search=%20%20")]
    [InlineData("?search=one&search=two")]
    [InlineData("?includeIds=12&includeIds=34")]
    [InlineData("?includeIds=")]
    [InlineData("?includeIds=12%2C12")]
    [InlineData("?includeIds=0")]
    [InlineData("?includeIds=-1")]
    [InlineData("?includeIds=twelve")]
    [InlineData("?includeIds=12%2C")]
    [InlineData("?page=0")]
    [InlineData("?page=-1")]
    [InlineData("?page=one")]
    [InlineData("?page=1&page=2")]
    [InlineData("?pageSize=0")]
    [InlineData("?pageSize=101")]
    [InlineData("?pageSize=wide")]
    [InlineData("?pageSize=25&pageSize=50")]
    public void TryBuildQuery_InvalidValue_ReturnsBadRequest(string queryString)
    {
        var built = TryBuildQuery(queryString, out _, out var error);

        Assert.False(built);
        Assert.IsType<BadRequestObjectResult>(error);
    }

    [Fact]
    public void TryBuildQuery_SearchTooLong_ReturnsBadRequest()
    {
        var search = new string('x', WordPressCategoryApi.MaxSearchLength + 1);

        var built = TryBuildQuery(
            $"?search={search}",
            out _,
            out var error);

        Assert.False(built);
        Assert.IsType<BadRequestObjectResult>(error);
    }

    private static bool TryBuildQuery(
        string queryString,
        out CategoryListQuery query,
        out IActionResult error)
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString(queryString);
        return WordPressCategoryApi.TryBuildQuery(
            context.Request,
            PlatformId,
            out query,
            out error);
    }
}
