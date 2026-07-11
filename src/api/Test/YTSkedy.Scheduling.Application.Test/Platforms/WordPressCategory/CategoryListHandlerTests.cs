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
        var categoryReader = new FakeCategoryReader { Result = page };
        var platformReader = new FakePlatformReader(
            getResult: Platform(PlatformType.WordPress, settings));
        var handler = new CategoryListHandler(platformReader, categoryReader);
        var cancellationToken = new CancellationTokenSource().Token;

        var result = await handler.HandleAsync(
            new CategoryListQuery(PlatformId, "events", [12, 34], 2, 25),
            cancellationToken);

        Assert.Equal(CategoryListStatus.Listed, result.Status);
        Assert.Same(page, result.Page);
        Assert.Equal(PlatformId, platformReader.PlatformId);
        Assert.Equal(cancellationToken, platformReader.CancellationToken);
        Assert.Same(settings, categoryReader.Settings);
        Assert.Equal("events", categoryReader.Query!.Search);
        Assert.Equal([12, 34], categoryReader.Query.IncludeIds);
        Assert.Equal(2, categoryReader.Query.Page);
        Assert.Equal(25, categoryReader.Query.PageSize);
        Assert.Equal(cancellationToken, categoryReader.CancellationToken);
    }

    [Fact]
    public async Task HandleAsync_MissingPlatform_ReturnsPlatformNotFound()
    {
        var categoryReader = new FakeCategoryReader();
        var handler = new CategoryListHandler(
            new FakePlatformReader(),
            categoryReader);

        var result = await HandleAsync(handler);

        Assert.Equal(CategoryListStatus.PlatformNotFound, result.Status);
        Assert.Null(result.Page);
        Assert.Equal(0, categoryReader.CallCount);
    }

    [Fact]
    public async Task HandleAsync_YouTubePlatform_ReturnsInvalidPlatformType()
    {
        var categoryReader = new FakeCategoryReader();
        var handler = new CategoryListHandler(
            new FakePlatformReader(getResult: Platform(PlatformType.YouTube)),
            categoryReader);

        var result = await HandleAsync(handler);

        Assert.Equal(CategoryListStatus.InvalidPlatformType, result.Status);
        Assert.Null(result.Page);
        Assert.Equal(0, categoryReader.CallCount);
    }

    [Fact]
    public async Task HandleAsync_WordPressTypeWithYouTubeSettings_ReturnsInvalidPlatformType()
    {
        var categoryReader = new FakeCategoryReader();
        var handler = new CategoryListHandler(
            new FakePlatformReader(
                getResult: Platform(
                    PlatformType.WordPress,
                    ApplicationTestData.YouTubeSettings())),
            categoryReader);

        var result = await HandleAsync(handler);

        Assert.Equal(CategoryListStatus.InvalidPlatformType, result.Status);
        Assert.Equal(0, categoryReader.CallCount);
    }

    [Fact]
    public async Task HandleAsync_ProviderFailure_ReturnsProviderFailed()
    {
        var handler = new CategoryListHandler(
            new FakePlatformReader(
                getResult: Platform(
                    PlatformType.WordPress,
                    ApplicationTestData.WordPressSettings())),
            new FakeCategoryReader
            {
                Exception = new CategoryReadException("Provider failed.")
            });

        var result = await HandleAsync(handler);

        Assert.Equal(CategoryListStatus.ProviderFailed, result.Status);
        Assert.Null(result.Page);
    }

    [Fact]
    public async Task HandleAsync_ReaderCancellation_PropagatesCancellation()
    {
        var cancellation = new OperationCanceledException();
        var handler = new CategoryListHandler(
            new FakePlatformReader(
                getResult: Platform(
                    PlatformType.WordPress,
                    ApplicationTestData.WordPressSettings())),
            new FakeCategoryReader { Exception = cancellation });

        var exception = await Assert.ThrowsAsync<OperationCanceledException>(
            () => HandleAsync(handler));

        Assert.Same(cancellation, exception);
    }

    [Fact]
    public async Task HandleAsync_NullQuery_Throws()
    {
        var handler = new CategoryListHandler(
            new FakePlatformReader(),
            new FakeCategoryReader());

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
}
