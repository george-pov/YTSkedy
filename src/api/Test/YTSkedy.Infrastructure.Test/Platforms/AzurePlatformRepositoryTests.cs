using YTSkedy.Infrastructure.Platforms;
using YTSkedy.Infrastructure.Test.TestSupport;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;

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
                publishingContent: new PublishingContent("title-template", "description-template")),
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
            YouTubeSettings(),
            RequiredPublishingContent(),
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
            YouTubeSettings(),
            RequiredPublishingContent(),
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
            YouTubeSettings(),
            RequiredPublishingContent(),
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
            YouTubeSettings(),
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
            YouTubeSettings(),
            publishingContent ?? RequiredPublishingContent(),
            referenceKey);

    private static YouTubeSettings YouTubeSettings() =>
        new(
            new YouTubeCredentials("client-id", "client-secret", "refresh-token"),
            "private",
            false);

    private static PublishingContent RequiredPublishingContent() =>
        new("title-template", "description-template");

}
