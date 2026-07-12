using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Domain.Test.Platforms;

public class WordPressSettingsTests
{
    [Fact]
    public void Constructor_ValidInput_SetsProperties()
    {
        var settings = CreateSettings(
            siteUrl: "  https://example.com/blog  ",
            applicationPassword: "application-password",
            categoryIds: [12, 34],
            sticky: true);

        Assert.Equal("https://example.com/blog", settings.SiteUrl);
        Assert.Equal("editor", settings.Username);
        Assert.Equal("application-password", settings.ApplicationPassword);
        Assert.Equal("draft", settings.PostStatus);
        Assert.Equal([12, 34], settings.CategoryIds);
        Assert.True(settings.Sticky);
        Assert.Null(settings.ScheduleOffsetHours);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("/wp-json/wp/v2/posts")]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com")]
    [InlineData("http://example.com")]
    [InlineData("http://user:password@example.com")]
    public void Constructor_InvalidSiteUrl_Throws(string? siteUrl)
    {
        Assert.Throws<ArgumentException>(
            () => CreateSettings(siteUrl: siteUrl!, postStatus: "publish"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyUsername_Throws(string? username)
    {
        Assert.Throws<ArgumentException>(
            () => CreateSettings(username: username!, postStatus: "publish"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyApplicationPassword_Throws(string? applicationPassword)
    {
        Assert.Throws<ArgumentException>(
            () => CreateSettings(
                applicationPassword: applicationPassword!,
                postStatus: "publish"));
    }

    [Theory]
    [InlineData("Publish")]
    [InlineData("scheduled")]
    [InlineData("")]
    public void Constructor_InvalidPostStatus_Throws(string postStatus)
    {
        Assert.Throws<ArgumentException>(
            () => CreateSettings(postStatus: postStatus));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(25)]
    [InlineData(WordPressSettings.MaxScheduleOffsetHours)]
    public void Constructor_ScheduledPostWithValidOffset_SetsScheduleOffsetHours(
        int scheduleOffsetHours)
    {
        var settings = CreateSettings(
            postStatus: WordPressSettings.ScheduledPostStatus,
            scheduleOffsetHours: scheduleOffsetHours);

        Assert.Equal(WordPressSettings.ScheduledPostStatus, settings.PostStatus);
        Assert.Equal(scheduleOffsetHours, settings.ScheduleOffsetHours);
    }

    [Fact]
    public void Constructor_ScheduledPostWithoutOffset_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => CreateSettings(postStatus: WordPressSettings.ScheduledPostStatus));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(WordPressSettings.MaxScheduleOffsetHours + 1)]
    public void Constructor_ScheduledPostWithInvalidOffset_Throws(int scheduleOffsetHours)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateSettings(
                postStatus: WordPressSettings.ScheduledPostStatus,
                scheduleOffsetHours: scheduleOffsetHours));
    }

    [Fact]
    public void TryComputeScheduledPostUtc_ValidOffset_ReturnsComputedInstant()
    {
        var computed = WordPressSettings.TryComputeScheduledPostUtc(
            new DateTimeOffset(2026, 6, 25, 17, 0, 0, TimeSpan.Zero),
            WordPressSettings.MaxScheduleOffsetHours,
            out var scheduledPostUtc);

        Assert.True(computed);
        Assert.Equal(
            new DateTimeOffset(2026, 6, 18, 17, 0, 0, TimeSpan.Zero),
            scheduledPostUtc);
    }

    [Fact]
    public void TryComputeScheduledPostUtc_InvalidOffset_ReturnsFalse()
    {
        var computed = WordPressSettings.TryComputeScheduledPostUtc(
            new DateTimeOffset(2026, 6, 25, 17, 0, 0, TimeSpan.Zero),
            WordPressSettings.MaxScheduleOffsetHours + 1,
            out _);

        Assert.False(computed);
    }

    [Fact]
    public void Constructor_NonScheduledPostWithOffset_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => CreateSettings(scheduleOffsetHours: 2));
    }

    [Fact]
    public void Constructor_EmptyCategoryIds_SetsEmptyCollection()
    {
        var settings = CreateSettings();

        Assert.Empty(settings.CategoryIds);
    }

    [Fact]
    public void Constructor_CategoryIds_PreservesOrderAndCopiesInput()
    {
        long[] categoryIds = [34, 12];

        var settings = CreateSettings(categoryIds: categoryIds);
        categoryIds[0] = 99;

        Assert.Equal([34, 12], settings.CategoryIds);
    }

    [Fact]
    public void Constructor_NullCategoryIds_Throws()
    {
        Assert.Throws<ArgumentException>(() => CreateSettings(
            categoryIds: null,
            useDefaultCategoryIds: false));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_NonPositiveCategoryId_Throws(long categoryId)
    {
        Assert.Throws<ArgumentException>(() => CreateSettings(categoryIds: [categoryId]));
    }

    [Fact]
    public void Constructor_DuplicateCategoryIds_Throws()
    {
        Assert.Throws<ArgumentException>(() => CreateSettings(categoryIds: [12, 12]));
    }

    [Theory]
    [InlineData(true, new long[] { })]
    [InlineData(true, new long[] { 12, 34 })]
    [InlineData(false, new long[] { 0 })]
    [InlineData(false, new long[] { -1 })]
    [InlineData(false, new long[] { 12, 12 })]
    public void AreValidCategoryIds_Values_ReturnsExpected(
        bool expected,
        long[] categoryIds)
    {
        Assert.Equal(expected, WordPressSettings.AreValidCategoryIds(categoryIds));
    }

    [Fact]
    public void AreValidCategoryIds_Null_ReturnsFalse()
    {
        Assert.False(WordPressSettings.AreValidCategoryIds(null));
    }

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("https://example.com/blog")]
    [InlineData("http://localhost")]
    [InlineData("http://localhost:8000/")]
    [InlineData("http://127.0.0.1:8000/")]
    public void IsValidSiteUrl_AllowedValues_ReturnsTrue(string siteUrl)
    {
        Assert.True(WordPressSettings.IsValidSiteUrl(siteUrl));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("example.com")]
    [InlineData("http://example.com")]
    [InlineData("ftp://example.com")]
    [InlineData("https://user:password@example.com")]
    [InlineData("/relative")]
    public void IsValidSiteUrl_DisallowedValues_ReturnsFalse(string? siteUrl)
    {
        Assert.False(WordPressSettings.IsValidSiteUrl(siteUrl));
    }

    [Theory]
    [InlineData("editor")]
    public void IsValidUsername_NonEmpty_ReturnsTrue(string username)
    {
        Assert.True(WordPressSettings.IsValidUsername(username));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValidUsername_NullOrWhiteSpace_ReturnsFalse(string? username)
    {
        Assert.False(WordPressSettings.IsValidUsername(username));
    }

    [Theory]
    [InlineData("application-password")]
    public void IsValidApplicationPassword_NonEmpty_ReturnsTrue(string applicationPassword)
    {
        Assert.True(WordPressSettings.IsValidApplicationPassword(applicationPassword));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValidApplicationPassword_NullOrWhiteSpace_ReturnsFalse(
        string? applicationPassword)
    {
        Assert.False(WordPressSettings.IsValidApplicationPassword(applicationPassword));
    }

    [Theory]
    [InlineData("draft")]
    [InlineData("pending")]
    [InlineData("private")]
    [InlineData("future")]
    [InlineData("publish")]
    public void IsValidPostStatus_AllowedLowercaseValues_ReturnsTrue(string postStatus)
    {
        Assert.True(WordPressSettings.IsValidPostStatus(postStatus));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Publish")]
    [InlineData("scheduled")]
    public void IsValidPostStatus_OtherValues_ReturnsFalse(string? postStatus)
    {
        Assert.False(WordPressSettings.IsValidPostStatus(postStatus));
    }

    [Theory]
    [InlineData("future", true)]
    [InlineData("draft", false)]
    [InlineData("pending", false)]
    [InlineData("private", false)]
    [InlineData("publish", false)]
    [InlineData("scheduled", false)]
    [InlineData(null, false)]
    public void RequiresScheduleOffsetHours_Status_ReturnsExpected(
        string? postStatus,
        bool expected)
    {
        Assert.Equal(expected, WordPressSettings.RequiresScheduleOffsetHours(postStatus));
    }

    private static WordPressSettings CreateSettings(
        string siteUrl = "https://example.com",
        string username = "editor",
        string applicationPassword = "password",
        string postStatus = "draft",
        IReadOnlyCollection<long>? categoryIds = null,
        bool sticky = false,
        bool useDefaultCategoryIds = true,
        int? scheduleOffsetHours = null)
    {
        return new WordPressSettings(
            siteUrl,
            username,
            applicationPassword,
            postStatus,
            useDefaultCategoryIds ? categoryIds ?? [] : categoryIds!,
            sticky,
            scheduleOffsetHours);
    }
}
