using YTSkedy.Infrastructure.Platforms;
using YTSkedy.Infrastructure.Test.TestSupport;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.Scheduling.TestSupport;
using YTSkedy.TestSupport;

namespace YTSkedy.Infrastructure.Test.Platforms;

public sealed class AzurePlatformRepositoryTests
{
    [Fact]
    public async Task CreateAsync_NullReferenceKey_CreatesAndReadsNull()
    {
        var tableClient = new PlatformTableClient();
        var repository = CreateRepository(tableClient);

        var result = await repository.CreateAsync(
            YouTubePlatform("Main YouTube channel", referenceKey: null),
            CancellationToken.None);

        Assert.Equal(CreatePlatformStatus.Created, result.Status);
        Assert.Equal(
            [
                nameof(PlatformEntity.RowKey),
                nameof(PlatformEntity.Name),
                nameof(PlatformEntity.ReferenceKey)
            ],
            tableClient.LastQuerySelect);
        Assert.DoesNotContain(
            nameof(PlatformEntity.PublishSettingsJson),
            tableClient.LastQuerySelect!);

        var view = await repository.GetAsync(result.PlatformId!, CancellationToken.None);

        Assert.NotNull(view);
        Assert.Null(view.ReferenceKey);
    }

    [Fact]
    public async Task CreateAsync_WithReferenceKey_ReadAndListPreserveDisplayCasing()
    {
        var tableClient = new PlatformTableClient();
        var repository = CreateRepository(tableClient);

        var result = await repository.CreateAsync(
            YouTubePlatform("Main YouTube channel", "youTube1"),
            CancellationToken.None);

        var read = await repository.GetAsync(result.PlatformId!, CancellationToken.None);
        var listed = await repository.ListAsync(null, CancellationToken.None);

        Assert.NotNull(read);
        Assert.Equal("youTube1", read.ReferenceKey);
        Assert.Contains(listed, view => view.PlatformId == result.PlatformId &&
            view.ReferenceKey == "youTube1");
    }

    [Fact]
    public async Task CreateAsync_WithPublishingContent_ReadAndListPreserveTemplateIds()
    {
        var tableClient = new PlatformTableClient();
        var repository = CreateRepository(tableClient);

        var result = await repository.CreateAsync(
            YouTubePlatform(
                "Main YouTube channel",
                referenceKey: null,
                publishingContent: SchedulingSamples.PublishingContent()),
            CancellationToken.None);

        var read = await repository.GetAsync(result.PlatformId!, CancellationToken.None);
        var listed = await repository.ListAsync(null, CancellationToken.None);

        Assert.NotNull(read);
        Assert.Equal("title-template", read.PublishingContent.TitleTemplateId);
        Assert.Equal("description-template", read.PublishingContent.DescriptionTemplateId);
        Assert.Contains(listed, view => view.PlatformId == result.PlatformId &&
            view.PublishingContent.TitleTemplateId == "title-template" &&
            view.PublishingContent.DescriptionTemplateId == "description-template");
    }

    [Fact]
    public async Task CreateAsync_WordPressCategoryIds_ReadAndListPreserveOrder()
    {
        var tableClient = new PlatformTableClient();
        var repository = CreateRepository(tableClient);
        var platform = new Platform(
            "Main WordPress site",
            PlatformType.WordPress,
            SchedulingSamples.WordPressSettings(categoryIds: [34, 12]),
            SchedulingSamples.PublishingContent());

        var result = await repository.CreateAsync(platform, CancellationToken.None);
        var read = await repository.GetAsync(result.PlatformId!, CancellationToken.None);
        var listed = await repository.ListAsync(PlatformType.WordPress, CancellationToken.None);

        Assert.Equal(CreatePlatformStatus.Created, result.Status);
        Assert.Equal(
            [34, 12],
            Assert.IsType<WordPressSettings>(read!.PublishSettings).CategoryIds);
        Assert.Equal(
            [34, 12],
            Assert.IsType<WordPressSettings>(Assert.Single(listed).PublishSettings).CategoryIds);
    }

    [Fact]
    public async Task ListIdsAsync_PlatformRows_ReturnsOrdinalSetUsingIdOnlySelection()
    {
        var tableClient = new PlatformTableClient();
        var repository = CreateRepository(tableClient);
        var first = await repository.CreateAsync(
            YouTubePlatform("Main YouTube channel", referenceKey: null),
            CancellationToken.None);
        var second = await repository.CreateAsync(
            YouTubePlatform("Backup YouTube channel", referenceKey: null),
            CancellationToken.None);

        var result = await repository.ListIdsAsync(CancellationToken.None);

        Assert.Equal(
            new[] { first.PlatformId, second.PlatformId }.Order(StringComparer.Ordinal),
            result.Order(StringComparer.Ordinal));
        Assert.Equal(["PlatformId"], tableClient.LastQuerySelect);
    }

    [Fact]
    public async Task CreateAsync_DuplicateReferenceKeyDifferentCasing_ReturnsReferenceKeyAlreadyExists()
    {
        var tableClient = new PlatformTableClient();
        var repository = CreateRepository(tableClient);
        await repository.CreateAsync(
            YouTubePlatform("Main YouTube channel", "youTube1"),
            CancellationToken.None);

        var result = await repository.CreateAsync(
            YouTubePlatform("Backup YouTube channel", "youtube1"),
            CancellationToken.None);

        Assert.Equal(CreatePlatformStatus.ReferenceKeyAlreadyExists, result.Status);
        Assert.Null(result.PlatformId);
    }

    [Fact]
    public async Task UpdateAsync_NullToReferenceKey_PreservesDisplayCasing()
    {
        var tableClient = new PlatformTableClient();
        var repository = CreateRepository(tableClient);
        var create = await repository.CreateAsync(
            YouTubePlatform("Main YouTube channel", referenceKey: null),
            CancellationToken.None);

        var result = await repository.UpdateAsync(
            create.PlatformId!,
            "Main YouTube channel",
            "WP-1",
            SchedulingSamples.YouTubeSettings(),
            SchedulingSamples.PublishingContent(),
            CancellationToken.None);
        var read = await repository.GetAsync(create.PlatformId!, CancellationToken.None);

        Assert.Equal(UpdatePlatformResult.Updated, result);
        Assert.NotNull(read);
        Assert.Equal("WP-1", read.ReferenceKey);
    }

    [Fact]
    public async Task UpdateAsync_CasingOnlyReferenceKeyChange_PreservesDisplayCasing()
    {
        var tableClient = new PlatformTableClient();
        var repository = CreateRepository(tableClient);
        var create = await repository.CreateAsync(
            YouTubePlatform("Main YouTube channel", "WP-1"),
            CancellationToken.None);

        var result = await repository.UpdateAsync(
            create.PlatformId!,
            "Main YouTube channel",
            "wp-1",
            SchedulingSamples.YouTubeSettings(),
            SchedulingSamples.PublishingContent(),
            CancellationToken.None);
        var read = await repository.GetAsync(create.PlatformId!, CancellationToken.None);

        Assert.Equal(UpdatePlatformResult.Updated, result);
        Assert.NotNull(read);
        Assert.Equal("wp-1", read.ReferenceKey);
    }

    [Fact]
    public async Task UpdateAsync_OtherPlatformReferenceKeyDifferentCasing_ReturnsReferenceKeyAlreadyExists()
    {
        var tableClient = new PlatformTableClient();
        var repository = CreateRepository(tableClient);
        await repository.CreateAsync(
            YouTubePlatform("Main YouTube channel", "WP-1"),
            CancellationToken.None);
        var other = await repository.CreateAsync(
            YouTubePlatform("Backup YouTube channel", referenceKey: null),
            CancellationToken.None);

        var result = await repository.UpdateAsync(
            other.PlatformId!,
            "Backup YouTube channel",
            "wp-1",
            SchedulingSamples.YouTubeSettings(),
            SchedulingSamples.PublishingContent(),
            CancellationToken.None);

        Assert.Equal(UpdatePlatformResult.ReferenceKeyAlreadyExists, result);
    }

    [Fact]
    public async Task UpdateAsync_PublishingContent_ReplacesTemplateIds()
    {
        var tableClient = new PlatformTableClient();
        var repository = CreateRepository(tableClient);
        var create = await repository.CreateAsync(
            YouTubePlatform(
                "Main YouTube channel",
                referenceKey: null,
                publishingContent: new PublishingContent("old-title", "old-description")),
            CancellationToken.None);

        var result = await repository.UpdateAsync(
            create.PlatformId!,
            "Main YouTube channel",
            null,
            SchedulingSamples.YouTubeSettings(),
            new PublishingContent("new-title", "new-description"),
            CancellationToken.None);
        var read = await repository.GetAsync(create.PlatformId!, CancellationToken.None);

        Assert.Equal(UpdatePlatformResult.Updated, result);
        Assert.NotNull(read);
        Assert.Equal("new-title", read.PublishingContent.TitleTemplateId);
        Assert.Equal("new-description", read.PublishingContent.DescriptionTemplateId);
    }

    [Fact]
    public async Task DeleteAsync_ThenCreateWithSameReferenceKey_Creates()
    {
        var tableClient = new PlatformTableClient();
        var repository = CreateRepository(tableClient);
        var create = await repository.CreateAsync(
            YouTubePlatform("Main YouTube channel", "youTube1"),
            CancellationToken.None);

        var delete = await repository.DeleteAsync(create.PlatformId!, CancellationToken.None);
        var recreate = await repository.CreateAsync(
            YouTubePlatform("Replacement YouTube channel", "youtube1"),
            CancellationToken.None);

        Assert.Equal(DeletePlatformResult.Deleted, delete);
        Assert.Equal(CreatePlatformStatus.Created, recreate.Status);
    }

    private static AzurePlatformRepository CreateRepository(PlatformTableClient tableClient) =>
        new(tableClient, new FixedTimeProvider(
            new DateTimeOffset(2026, 06, 27, 12, 00, 00, TimeSpan.Zero)));

    private static Platform YouTubePlatform(
        string name,
        string? referenceKey,
        PublishingContent? publishingContent = null) =>
        new(
            name,
            PlatformType.YouTube,
            SchedulingSamples.YouTubeSettings(),
            publishingContent ?? SchedulingSamples.PublishingContent(),
            referenceKey);
}
