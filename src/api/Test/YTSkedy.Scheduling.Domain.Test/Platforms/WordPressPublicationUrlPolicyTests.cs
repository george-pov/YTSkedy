using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Domain.Test.Platforms;

public class WordPressPublicationUrlPolicyTests
{
    [Theory]
    [InlineData(
        "https://example.com/posts/74",
        "https://example.com/blog",
        "https://example.com/posts/74")]
    [InlineData(
        " https://EXAMPLE.com:443/posts/74 ",
        "https://example.com/blog",
        "https://example.com/posts/74")]
    [InlineData(
        "http://localhost:8080/posts/74",
        "http://localhost:8080/blog",
        "http://localhost:8080/posts/74")]
    [InlineData(
        "http://127.0.0.1/posts/74",
        "http://127.0.0.1",
        "http://127.0.0.1/posts/74")]
    public void NormalizeCanonical_SafeSameOrigin_ReturnsNormalizedUrl(
        string canonical,
        string siteUrl,
        string expected)
    {
        var result = WordPressPublicationUrlPolicy.NormalizeCanonical(canonical, siteUrl);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null, "https://example.com")]
    [InlineData("", "https://example.com")]
    [InlineData("/posts/74", "https://example.com")]
    [InlineData("https://other.example.com/posts/74", "https://example.com")]
    [InlineData("http://example.com/posts/74", "https://example.com")]
    [InlineData("https://example.com:444/posts/74", "https://example.com")]
    [InlineData("https://user:password@example.com/posts/74", "https://example.com")]
    [InlineData("ftp://example.com/posts/74", "https://example.com")]
    [InlineData("http://example.com/posts/74", "http://example.com")]
    [InlineData("https://example.com/posts/74", null)]
    public void NormalizeCanonical_UnsafeOrIncomplete_ReturnsNull(
        string? canonical,
        string? siteUrl)
    {
        var result = WordPressPublicationUrlPolicy.NormalizeCanonical(canonical, siteUrl);

        Assert.Null(result);
    }

    [Fact]
    public void NormalizeCanonical_OversizedValue_ReturnsNull()
    {
        var value = $"https://example.com/{new string('a', WordPressPublicationUrlPolicy.MaxUrlLength)}";

        Assert.Null(WordPressPublicationUrlPolicy.NormalizeCanonical(value, "https://example.com"));
    }

    [Theory]
    [InlineData("https://example.com/blog?old=1#section", "74", "https://example.com/blog?p=74")]
    [InlineData("https://example.com/blog/", "74", "https://example.com/blog/?p=74")]
    [InlineData("http://localhost:8080/blog", "9", "http://localhost:8080/blog?p=9")]
    public void BuildFallback_SafeTargetAndPositiveId_ReturnsPlainPermalink(
        string siteUrl,
        string externalResourceId,
        string expected)
    {
        var result = WordPressPublicationUrlPolicy.BuildFallback(siteUrl, externalResourceId);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("+1")]
    [InlineData("1.0")]
    [InlineData(" 1")]
    [InlineData("1 ")]
    [InlineData("abc")]
    public void BuildFallback_InvalidId_ReturnsNull(string? externalResourceId)
    {
        var result = WordPressPublicationUrlPolicy.BuildFallback(
            "https://example.com/blog",
            externalResourceId);

        Assert.Null(result);
    }

    [Fact]
    public void BuildFallback_UnsafeOrMissingTarget_ReturnsNull()
    {
        Assert.Null(WordPressPublicationUrlPolicy.BuildFallback(null, "74"));
        Assert.Null(WordPressPublicationUrlPolicy.BuildFallback("http://example.com", "74"));
        Assert.Null(WordPressPublicationUrlPolicy.BuildFallback(
            "https://user:password@example.com",
            "74"));
    }

    [Fact]
    public void Resolve_PrefersCanonicalAndFallsBackWhenCanonicalIsUnsafe()
    {
        Assert.Equal(
            "https://example.com/custom/post",
            WordPressPublicationUrlPolicy.Resolve(
                "https://example.com/custom/post",
                "https://example.com/blog",
                "74"));
        Assert.Equal(
            "https://example.com/blog?p=74",
            WordPressPublicationUrlPolicy.Resolve(
                "https://other.example.com/post",
                "https://example.com/blog",
                "74"));
    }
}
