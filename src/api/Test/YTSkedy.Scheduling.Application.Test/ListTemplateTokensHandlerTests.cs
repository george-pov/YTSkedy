using YTSkedy.Scheduling.Application.Templates;
using YTSkedy.Scheduling.Application.Settings;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.CalendarEvents;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Test;

public class ListTemplateTokensHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsCurrentTextFieldDateAndReferenceKeyTokenCatalog()
    {
        var handler = new ListTemplateTokensHandler(
            new FakeEventTextFieldsReader(
                new EventTextFields(
                    [
                        new EventTextField(
                            "Episode title",
                            EventTextType.ShortText,
                            80),
                        new EventTextField(
                            "Episode details",
                            EventTextType.LongText,
                            2500)
                    ])),
            new FakePlatformReader(
                [
                    Platform(referenceKey: null),
                    Platform(referenceKey: "privateYouTube")
                ]));

        var tokens = await handler.HandleAsync(CancellationToken.None);

        Assert.Equal(
            [
                "text1",
                "text2",
                "longDateEn",
                "shortDateEn",
                "longDateRu",
                "shortDateRu",
                "longDateFr",
                "shortDateFr",
                "privateYouTube"
            ],
            tokens.Select(token => token.Name));
    }

    private static PlatformView Platform(string? referenceKey) =>
        new(
            $"platform-{referenceKey ?? "unset"}",
            $"Platform {referenceKey ?? "unset"}",
            referenceKey,
            PlatformType.YouTube,
            new YouTubeSettings(
                new YouTubeCredentials("client-id", "client-secret", "refresh-token"),
                "private",
                false),
            new PublishingContent("title-template", "description-template"));

    private sealed class FakeEventTextFieldsReader(EventTextFields eventTextFields) :
        IEventTextFieldsReader
    {
        public Task<EventTextFields> GetAsync(CancellationToken cancellationToken) =>
            Task.FromResult(eventTextFields);
    }

    private sealed class FakePlatformReader(IReadOnlyList<PlatformView> platforms) : IPlatformReader
    {
        public Task<IReadOnlyList<PlatformView>> ListAsync(
            PlatformType? type,
            CancellationToken cancellationToken) =>
            Task.FromResult(platforms);

        public Task<PlatformView?> GetAsync(
            string platformId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
