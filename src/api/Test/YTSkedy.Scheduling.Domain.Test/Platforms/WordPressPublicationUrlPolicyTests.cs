using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Domain.Test.Platforms;

public class WordPressPublicationUrlPolicyTests
{
    [Theory]
    [InlineData(
        "https://example.com/blog?old=1#section",
        "74",
        "https://example.com/blog/wp-admin/post.php?post=74&action=edit")]
    [InlineData(
        "https://example.com/blog/",
        "74",
        "https://example.com/blog/wp-admin/post.php?post=74&action=edit")]
    [InlineData(
        "https://example.com",
        "126",
        "https://example.com/wp-admin/post.php?post=126&action=edit")]
    [InlineData(
        "http://localhost:8080/blog",
        "9",
        "http://localhost:8080/blog/wp-admin/post.php?post=9&action=edit")]
    public void BuildAdminEditUrl_SafeTargetAndPositiveId_ReturnsEditorUrl(
        string siteUrl,
        string externalResourceId,
        string expected)
    {
        var result = WordPressPublicationUrlPolicy.BuildAdminEditUrl(
            siteUrl,
            externalResourceId);

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
    public void BuildAdminEditUrl_InvalidId_ReturnsNull(string? externalResourceId)
    {
        var result = WordPressPublicationUrlPolicy.BuildAdminEditUrl(
            "https://example.com/blog",
            externalResourceId);

        Assert.Null(result);
    }

    [Fact]
    public void BuildAdminEditUrl_UnsafeOrMissingTarget_ReturnsNull()
    {
        Assert.Null(WordPressPublicationUrlPolicy.BuildAdminEditUrl(null, "74"));
        Assert.Null(WordPressPublicationUrlPolicy.BuildAdminEditUrl("http://example.com", "74"));
        Assert.Null(WordPressPublicationUrlPolicy.BuildAdminEditUrl(
            "https://user:password@example.com",
            "74"));
    }

    [Theory]
    [InlineData("https://example.com/posts/74")]
    [InlineData("https://other.example.com/wp-admin/post.php?post=74&action=edit")]
    [InlineData("https://example.com/wp-admin/post.php?post=75&action=edit")]
    [InlineData("https://example.com/wp-admin/post.php?action=edit&post=74")]
    [InlineData("https://example.com/wp-admin/post.php?post=74&action=edit&extra=1")]
    [InlineData("https://example.com/wp-admin/post.php?post=74&action=edit#section")]
    public void NormalizeAdminEditUrl_UnexpectedDestination_ReturnsNull(string storedUrl)
    {
        Assert.Null(WordPressPublicationUrlPolicy.NormalizeAdminEditUrl(
            storedUrl,
            "https://example.com",
            "74"));
    }

    [Fact]
    public void Resolve_StoredEditorUrlMatchesTarget_ReturnsNormalizedUrl()
    {
        Assert.Equal(
            "https://example.com/blog/wp-admin/post.php?post=74&action=edit",
            WordPressPublicationUrlPolicy.Resolve(
                " https://EXAMPLE.com:443/blog/wp-admin/post.php?post=74&action=edit ",
                "https://example.com/blog",
                "74"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("https://example.com/posts/74")]
    [InlineData("https://other.example.com/wp-admin/post.php?post=74&action=edit")]
    public void Resolve_MissingOrOldStoredUrl_DerivesEditorUrl(string? storedUrl)
    {
        Assert.Equal(
            "https://example.com/blog/wp-admin/post.php?post=74&action=edit",
            WordPressPublicationUrlPolicy.Resolve(
                storedUrl,
                "https://example.com/blog",
                "74"));
    }

    [Fact]
    public void BuildAdminEditUrl_ResultExceedsLimit_ReturnsNull()
    {
        var siteUrl = $"https://example.com/{new string('a', 2020)}";

        Assert.Null(WordPressPublicationUrlPolicy.BuildAdminEditUrl(siteUrl, "74"));
    }
}
