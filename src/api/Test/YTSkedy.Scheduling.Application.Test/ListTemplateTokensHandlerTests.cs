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
        var fields = new Mock<IEventTextFieldsReader>();
        fields
            .Setup(reader => reader.GetAsync(CancellationToken.None))
            .ReturnsAsync(new EventTextFields(
                [
                    new EventTextField(
                        "Episode title",
                        EventTextType.ShortText,
                        80),
                    new EventTextField(
                        "Episode details",
                        EventTextType.LongText,
                        2500)
                ]));
        var platforms = new Mock<IPlatformReader>();
        platforms
            .Setup(reader => reader.ListAsync(null, CancellationToken.None))
            .ReturnsAsync([
                Platform(referenceKey: null),
                Platform(referenceKey: "privateYouTube")
            ]);
        var handler = new ListTemplateTokensHandler(fields.Object, platforms.Object);

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
        ApplicationTestData.Platform(
            platformId: $"platform-{referenceKey ?? "unset"}",
            name: $"Platform {referenceKey ?? "unset"}",
            referenceKey: referenceKey);
}
