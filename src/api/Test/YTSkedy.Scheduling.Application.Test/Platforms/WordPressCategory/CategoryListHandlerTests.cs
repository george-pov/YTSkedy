using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Platforms.WordPressCategory;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Test;

public class CategoryListHandlerTests
{
    private const string PlatformId = "4fb4a32f3f344de1a7c3a9f4a2f94918";
    private readonly Mock<IPlatformReader> _platforms = new();
    private readonly Mock<ICategoryReader> _categories = new();
    private readonly CategoryListHandler _handler;

    public CategoryListHandlerTests()
    {
        _handler = new CategoryListHandler(_platforms.Object, _categories.Object);
    }

    [Fact]
    public async Task HandleAsync_WordPressPlatform_ReturnsListedPageAndForwardsQuery()
    {
        var settings = ApplicationTestData.WordPressSettings("draft");
        var page = new CategoryPage(
            [new CategoryView(12, "Events", "events")],
            2,
            25,
            26,
            2);
        _categories
            .Setup(candidate => candidate.ListAsync(
                settings,
                It.Is<CategoryQuery>(query =>
                    query.Search == "events" &&
                    query.IncludeIds.SequenceEqual(new long[] { 12, 34 }) &&
                    query.Page == 2 &&
                    query.PageSize == 25),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);
        var platformReader = PlatformReader(Platform(PlatformType.WordPress, settings));
        var cancellationToken = new CancellationTokenSource().Token;

        var result = await _handler.HandleAsync(
            new CategoryListQuery(PlatformId, "events", [12, 34], 2, 25),
            cancellationToken);

        Assert.Equal(CategoryListStatus.Listed, result.Status);
        Assert.Same(page, result.Page);
        platformReader.Verify(candidate => candidate.GetAsync(PlatformId, cancellationToken));
        _categories.Verify(candidate => candidate.ListAsync(
            settings,
            It.Is<CategoryQuery>(query =>
                query.Search == "events" &&
                query.IncludeIds.SequenceEqual(new long[] { 12, 34 }) &&
                query.Page == 2 &&
                query.PageSize == 25),
            cancellationToken));
    }

    [Fact]
    public async Task HandleAsync_MissingPlatform_ReturnsPlatformNotFound()
    {
        PlatformReader(null);

        var result = await HandleAsync(_handler);

        Assert.Equal(CategoryListStatus.PlatformNotFound, result.Status);
        Assert.Null(result.Page);
        VerifyNoCategoryRead();
    }

    [Fact]
    public async Task HandleAsync_YouTubePlatform_ReturnsInvalidPlatformType()
    {
        PlatformReader(Platform(PlatformType.YouTube));

        var result = await HandleAsync(_handler);

        Assert.Equal(CategoryListStatus.InvalidPlatformType, result.Status);
        Assert.Null(result.Page);
        VerifyNoCategoryRead();
    }

    [Fact]
    public async Task HandleAsync_WordPressTypeWithYouTubeSettings_ReturnsInvalidPlatformType()
    {
        PlatformReader(Platform(
            PlatformType.WordPress,
            ApplicationTestData.YouTubeSettings()));

        var result = await HandleAsync(_handler);

        Assert.Equal(CategoryListStatus.InvalidPlatformType, result.Status);
        VerifyNoCategoryRead();
    }

    [Fact]
    public async Task HandleAsync_ProviderFailure_ReturnsProviderFailed()
    {
        var settings = ApplicationTestData.WordPressSettings();
        _categories
            .Setup(candidate => candidate.ListAsync(
                settings,
                It.IsAny<CategoryQuery>(),
                CancellationToken.None))
            .ThrowsAsync(new CategoryReadException("Provider failed."));
        PlatformReader(Platform(PlatformType.WordPress, settings));

        var result = await HandleAsync(_handler);

        Assert.Equal(CategoryListStatus.ProviderFailed, result.Status);
        Assert.Null(result.Page);
    }

    [Fact]
    public async Task HandleAsync_ReaderCancellation_PropagatesCancellation()
    {
        var cancellation = new OperationCanceledException();
        var settings = ApplicationTestData.WordPressSettings();
        _categories
            .Setup(candidate => candidate.ListAsync(
                settings,
                It.IsAny<CategoryQuery>(),
                CancellationToken.None))
            .ThrowsAsync(cancellation);
        PlatformReader(Platform(PlatformType.WordPress, settings));

        var exception = await Assert.ThrowsAsync<OperationCanceledException>(
            () => HandleAsync(_handler));

        Assert.Same(cancellation, exception);
    }

    [Fact]
    public async Task HandleAsync_NullQuery_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _handler.HandleAsync(null!, CancellationToken.None));
    }

    private static Task<CategoryListResult> HandleAsync(CategoryListHandler handler) =>
        handler.HandleAsync(
            new CategoryListQuery(PlatformId, null, [], 1, 25),
            CancellationToken.None);

    private static PlatformView Platform(
        PlatformType type,
        PublishSettings? settings = null) =>
        ApplicationTestData.Platform(
            platformId: PlatformId,
            type: type,
            publishSettings: settings);

    private Mock<IPlatformReader> PlatformReader(PlatformView? platform)
    {
        _platforms
            .Setup(candidate => candidate.GetAsync(
                PlatformId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(platform);
        return _platforms;
    }

    private void VerifyNoCategoryRead() =>
        _categories.Verify(candidate => candidate.ListAsync(
            It.IsAny<WordPressSettings>(),
            It.IsAny<CategoryQuery>(),
            It.IsAny<CancellationToken>()), Times.Never());
}
