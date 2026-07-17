using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Platforms.WordPressCategory;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Test;

public class CategoryListHandlerTests
{
    private const string PlatformId = "4fb4a32f3f344de1a7c3a9f4a2f94918";

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
        var categoryReader = new Mock<ICategoryReader>();
        categoryReader
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
        var handler = new CategoryListHandler(platformReader.Object, categoryReader.Object);
        var cancellationToken = new CancellationTokenSource().Token;

        var result = await handler.HandleAsync(
            new CategoryListQuery(PlatformId, "events", [12, 34], 2, 25),
            cancellationToken);

        Assert.Equal(CategoryListStatus.Listed, result.Status);
        Assert.Same(page, result.Page);
        platformReader.Verify(candidate => candidate.GetAsync(PlatformId, cancellationToken));
        categoryReader.Verify(candidate => candidate.ListAsync(
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
        var categoryReader = new Mock<ICategoryReader>();
        var handler = new CategoryListHandler(
            PlatformReader(null).Object,
            categoryReader.Object);

        var result = await HandleAsync(handler);

        Assert.Equal(CategoryListStatus.PlatformNotFound, result.Status);
        Assert.Null(result.Page);
        VerifyNoCategoryRead(categoryReader);
    }

    [Fact]
    public async Task HandleAsync_YouTubePlatform_ReturnsInvalidPlatformType()
    {
        var categoryReader = new Mock<ICategoryReader>();
        var handler = new CategoryListHandler(
            PlatformReader(Platform(PlatformType.YouTube)).Object,
            categoryReader.Object);

        var result = await HandleAsync(handler);

        Assert.Equal(CategoryListStatus.InvalidPlatformType, result.Status);
        Assert.Null(result.Page);
        VerifyNoCategoryRead(categoryReader);
    }

    [Fact]
    public async Task HandleAsync_WordPressTypeWithYouTubeSettings_ReturnsInvalidPlatformType()
    {
        var categoryReader = new Mock<ICategoryReader>();
        var handler = new CategoryListHandler(
            PlatformReader(Platform(
                PlatformType.WordPress,
                ApplicationTestData.YouTubeSettings())).Object,
            categoryReader.Object);

        var result = await HandleAsync(handler);

        Assert.Equal(CategoryListStatus.InvalidPlatformType, result.Status);
        VerifyNoCategoryRead(categoryReader);
    }

    [Fact]
    public async Task HandleAsync_ProviderFailure_ReturnsProviderFailed()
    {
        var settings = ApplicationTestData.WordPressSettings();
        var categoryReader = new Mock<ICategoryReader>();
        categoryReader
            .Setup(candidate => candidate.ListAsync(
                settings,
                It.IsAny<CategoryQuery>(),
                CancellationToken.None))
            .ThrowsAsync(new CategoryReadException("Provider failed."));
        var handler = new CategoryListHandler(
            PlatformReader(Platform(PlatformType.WordPress, settings)).Object,
            categoryReader.Object);

        var result = await HandleAsync(handler);

        Assert.Equal(CategoryListStatus.ProviderFailed, result.Status);
        Assert.Null(result.Page);
    }

    [Fact]
    public async Task HandleAsync_ReaderCancellation_PropagatesCancellation()
    {
        var cancellation = new OperationCanceledException();
        var settings = ApplicationTestData.WordPressSettings();
        var categoryReader = new Mock<ICategoryReader>();
        categoryReader
            .Setup(candidate => candidate.ListAsync(
                settings,
                It.IsAny<CategoryQuery>(),
                CancellationToken.None))
            .ThrowsAsync(cancellation);
        var handler = new CategoryListHandler(
            PlatformReader(Platform(PlatformType.WordPress, settings)).Object,
            categoryReader.Object);

        var exception = await Assert.ThrowsAsync<OperationCanceledException>(
            () => HandleAsync(handler));

        Assert.Same(cancellation, exception);
    }

    [Fact]
    public async Task HandleAsync_NullQuery_Throws()
    {
        var handler = new CategoryListHandler(
            new Mock<IPlatformReader>().Object,
            new Mock<ICategoryReader>().Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!, CancellationToken.None));
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

    private static Mock<IPlatformReader> PlatformReader(PlatformView? platform)
    {
        var reader = new Mock<IPlatformReader>();
        reader
            .Setup(candidate => candidate.GetAsync(
                PlatformId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(platform);
        return reader;
    }

    private static void VerifyNoCategoryRead(Mock<ICategoryReader> reader) =>
        reader.Verify(candidate => candidate.ListAsync(
            It.IsAny<WordPressSettings>(),
            It.IsAny<CategoryQuery>(),
            It.IsAny<CancellationToken>()), Times.Never());
}
