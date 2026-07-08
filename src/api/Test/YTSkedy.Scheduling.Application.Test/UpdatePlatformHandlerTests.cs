using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Scheduling.Application.Test;

public class UpdatePlatformHandlerTests
{
    private static readonly YouTubeSettings Settings =
        ApplicationTestData.YouTubeSettings("unlisted");

    [Fact]
    public async Task HandleAsync_Updated_ForwardsCommandAndReturnsUpdated()
    {
        var modifier = new FakePlatformModifier
        {
            UpdateResult = UpdatePlatformResult.Updated
        };
        var templates = ApplicationTestAdapters.DefaultTemplateReader();
        var publishingContent = ApplicationTestData.PublishingContent();
        var handler = new UpdatePlatformHandler(
            new FakePlatformReader(getResult: ExistingPlatform()),
            modifier,
            templates);
        var command = new UpdatePlatformCommand(
            "p1",
            "Renamed channel",
            "main-youtube",
            Settings,
            publishingContent);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(UpdatePlatformResult.Updated, result);
        Assert.Equal("p1", modifier.PlatformId);
        Assert.Equal("Renamed channel", modifier.Name);
        Assert.Equal("main-youtube", modifier.ReferenceKey);
        Assert.Same(Settings, modifier.PublishSettings);
        Assert.Same(publishingContent, modifier.PublishingContent);
        Assert.Equal(
            [
                (TemplateType.YouTube, ApplicationTestData.TitleTemplateId),
                (TemplateType.YouTube, ApplicationTestData.DescriptionTemplateId)
            ],
            templates.GetCalls);
    }

    [Fact]
    public async Task HandleAsync_NotFound_ReturnsNotFound()
    {
        var modifier = new FakePlatformModifier();
        var handler = new UpdatePlatformHandler(
            new FakePlatformReader(getResult: null),
            modifier,
            new FakeTemplateReader());
        var command = new UpdatePlatformCommand(
            "missing",
            "Renamed channel",
            null,
            Settings,
            ApplicationTestData.PublishingContent());

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(UpdatePlatformResult.NotFound, result);
        Assert.Null(modifier.PlatformId);
    }

    [Fact]
    public async Task HandleAsync_DuplicateName_ReturnsNameAlreadyExists()
    {
        var modifier = new FakePlatformModifier
        {
            UpdateResult = UpdatePlatformResult.NameAlreadyExists
        };
        var handler = new UpdatePlatformHandler(
            new FakePlatformReader(getResult: ExistingPlatform()),
            modifier,
            ApplicationTestAdapters.DefaultTemplateReader());
        var command = new UpdatePlatformCommand(
            "p1",
            "Taken name",
            null,
            Settings,
            ApplicationTestData.PublishingContent());

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(UpdatePlatformResult.NameAlreadyExists, result);
    }

    [Fact]
    public async Task HandleAsync_DuplicateReferenceKey_ReturnsReferenceKeyAlreadyExists()
    {
        var modifier = new FakePlatformModifier
        {
            UpdateResult = UpdatePlatformResult.ReferenceKeyAlreadyExists
        };
        var handler = new UpdatePlatformHandler(
            new FakePlatformReader(getResult: ExistingPlatform()),
            modifier,
            ApplicationTestAdapters.DefaultTemplateReader());
        var command = new UpdatePlatformCommand(
            "p1",
            "Main channel",
            "taken-key",
            Settings,
            ApplicationTestData.PublishingContent());

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(UpdatePlatformResult.ReferenceKeyAlreadyExists, result);
    }

    [Fact]
    public async Task HandleAsync_LinkedTemplateMissing_ReturnsLinkedTemplateNotFound()
    {
        var modifier = new FakePlatformModifier();
        var handler = new UpdatePlatformHandler(
            new FakePlatformReader(getResult: ExistingPlatform()),
            modifier,
            new FakeTemplateReader());
        var command = new UpdatePlatformCommand(
            "p1",
            "Main channel",
            null,
            Settings,
            ApplicationTestData.PublishingContent("missing-template"));

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(UpdatePlatformResult.LinkedTemplateNotFound, result);
        Assert.Null(modifier.PlatformId);
    }

    [Fact]
    public async Task HandleAsync_NullCommand_Throws()
    {
        var handler = new UpdatePlatformHandler(
            new FakePlatformReader(getResult: ExistingPlatform()),
            new FakePlatformModifier(),
            new FakeTemplateReader());

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!, CancellationToken.None));
    }

    private sealed class FakePlatformModifier : IPlatformModifier
    {
        public UpdatePlatformResult UpdateResult { get; init; } = UpdatePlatformResult.Updated;

        public string? PlatformId { get; private set; }

        public string? Name { get; private set; }

        public string? ReferenceKey { get; private set; }

        public PublishSettings? PublishSettings { get; private set; }

        public PublishingContent? PublishingContent { get; private set; }

        public Task<CreatePlatformResult> CreateAsync(
            Platform platform,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<UpdatePlatformResult> UpdateAsync(
            string platformId,
            string name,
            string? referenceKey,
            PublishSettings publishSettings,
            PublishingContent publishingContent,
            CancellationToken cancellationToken)
        {
            PlatformId = platformId;
            Name = name;
            ReferenceKey = referenceKey;
            PublishSettings = publishSettings;
            PublishingContent = publishingContent;

            return Task.FromResult(UpdateResult);
        }

        public Task<DeletePlatformResult> DeleteAsync(
            string platformId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private static PlatformView ExistingPlatform() =>
        ApplicationTestData.Platform(
            platformId: "p1",
            name: "Main channel",
            referenceKey: "main-youtube",
            publishSettings: Settings);
}
