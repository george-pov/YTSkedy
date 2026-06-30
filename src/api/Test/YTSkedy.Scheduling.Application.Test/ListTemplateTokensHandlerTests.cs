using YTSkedy.Scheduling.Application.Templates;
using YTSkedy.Scheduling.Application.Settings;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Test;

public class ListTemplateTokensHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsCurrentTextFieldAndDateTokenCatalog()
    {
        var handler = new ListTemplateTokensHandler(
            new FakeEventTextFieldsReader(
                new EventTextFields(
                    [
                        new EventTextField(
                            string.Empty,
                            "Episode title",
                            EventTextType.ShortText,
                            80),
                        new EventTextField(
                            string.Empty,
                            "Episode details",
                            EventTextType.LongText,
                            2500)
                    ])));

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
                "shortDateFr"
            ],
            tokens.Select(token => token.Name));
    }

    private sealed class FakeEventTextFieldsReader(EventTextFields eventTextFields) :
        IEventTextFieldsReader
    {
        public Task<EventTextFields> GetAsync(CancellationToken cancellationToken) =>
            Task.FromResult(eventTextFields);
    }
}
