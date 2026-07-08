using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Scheduling.Application.Test;

public class CreatePlatformHandlerTests
{
    private static readonly YouTubeSettings Settings =
        ApplicationTestData.YouTubeSettings();

    [Fact]
    public async Task HandleAsync_ValidCommand_CreatesPlatformAndReturnsCreatedWithId()
    {
        var modifier = new FakePlatformModifier
        {
            CreateResult = CreatePlatformResult.Created("p1")
        };
        var templates = ApplicationTestAdapters.DefaultTemplateReader();
        var publishingContent = ApplicationTestData.PublishingContent();
        var handler = new CreatePlatformHandler(modifier, templates);
        var command = new CreatePlatformCommand(
            "Main channel",
            PlatformType.YouTube,
            Settings,
            publishingContent,
            "main-youtube");

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(CreatePlatformStatus.Created, result.Status);
        Assert.Equal("p1", result.PlatformId);

        Assert.NotNull(modifier.CreatedPlatform);
        Assert.Equal("Main channel", modifier.CreatedPlatform!.Name);
        Assert.Equal(PlatformType.YouTube, modifier.CreatedPlatform.Type);
        Assert.Same(Settings, modifier.CreatedPlatform.PublishSettings);
        Assert.Equal("main-youtube", modifier.CreatedPlatform.ReferenceKey);
        Assert.Same(publishingContent, modifier.CreatedPlatform.PublishingContent);
        Assert.Equal(
            [
                (TemplateType.YouTube, ApplicationTestData.TitleTemplateId),
                (TemplateType.YouTube, ApplicationTestData.DescriptionTemplateId)
            ],
            templates.GetCalls);
    }

    [Fact]
    public async Task HandleAsync_DuplicateName_ReturnsNameAlreadyExists()
    {
        var modifier = new FakePlatformModifier
        {
            CreateResult = CreatePlatformResult.NameAlreadyExists()
        };
        var templates = ApplicationTestAdapters.DefaultTemplateReader();
        var handler = new CreatePlatformHandler(modifier, templates);
        var command = new CreatePlatformCommand(
            "Main channel",
            PlatformType.YouTube,
            Settings,
            ApplicationTestData.PublishingContent());

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(CreatePlatformStatus.NameAlreadyExists, result.Status);
        Assert.Null(result.PlatformId);
        Assert.Equal(
            [
                (TemplateType.YouTube, ApplicationTestData.TitleTemplateId),
                (TemplateType.YouTube, ApplicationTestData.DescriptionTemplateId)
            ],
            templates.GetCalls);
    }

    [Fact]
    public async Task HandleAsync_DuplicateReferenceKey_ReturnsReferenceKeyAlreadyExists()
    {
        var modifier = new FakePlatformModifier
        {
            CreateResult = CreatePlatformResult.ReferenceKeyAlreadyExists()
        };
        var handler = new CreatePlatformHandler(
            modifier,
            ApplicationTestAdapters.DefaultTemplateReader());
        var command = new CreatePlatformCommand(
            "Main channel",
            PlatformType.YouTube,
            Settings,
            ApplicationTestData.PublishingContent(),
            "main-youtube");

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(CreatePlatformStatus.ReferenceKeyAlreadyExists, result.Status);
        Assert.Null(result.PlatformId);
    }

    [Fact]
    public async Task HandleAsync_LinkedTemplateMissing_ReturnsLinkedTemplateNotFound()
    {
        var modifier = new FakePlatformModifier();
        var handler = new CreatePlatformHandler(modifier, new FakeTemplateReader());
        var command = new CreatePlatformCommand(
            "Main channel",
            PlatformType.YouTube,
            Settings,
            ApplicationTestData.PublishingContent("missing-template"));

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(CreatePlatformStatus.LinkedTemplateNotFound, result.Status);
        Assert.Null(result.PlatformId);
        Assert.Null(modifier.CreatedPlatform);
    }

    [Fact]
    public async Task HandleAsync_NullCommand_Throws()
    {
        var handler = new CreatePlatformHandler(
            new FakePlatformModifier(),
            new FakeTemplateReader());

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!, CancellationToken.None));
    }

    private sealed class FakePlatformModifier : IPlatformModifier
    {
        public CreatePlatformResult CreateResult { get; init; } =
            CreatePlatformResult.Created("platform-id");

        public Platform? CreatedPlatform { get; private set; }

        public Task<CreatePlatformResult> CreateAsync(
            Platform platform,
            CancellationToken cancellationToken)
        {
            CreatedPlatform = platform;

            return Task.FromResult(CreateResult);
        }

        public Task<UpdatePlatformResult> UpdateAsync(
            string platformId,
            string name,
            string? referenceKey,
            PublishSettings publishSettings,
            PublishingContent publishingContent,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<DeletePlatformResult> DeleteAsync(
            string platformId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
