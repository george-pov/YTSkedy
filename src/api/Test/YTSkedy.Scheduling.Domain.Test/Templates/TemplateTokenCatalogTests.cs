using YTSkedy.Scheduling.Domain.CalendarEvents;
using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Scheduling.Domain.Test.Templates;

public class TemplateTokenCatalogTests
{
    [Fact]
    public void From_EventTextFields_ReturnsTextAndDateTokenNamesInOrder()
    {
        var fields = new EventTextFields(
            [
                new EventTextField(string.Empty, "Episode title", EventTextType.ShortText, 80),
                new EventTextField(string.Empty, "Details", EventTextType.LongText, 2500),
                new EventTextField(string.Empty, "Social copy", EventTextType.ShortText, 140)
            ]);

        var names = TemplateTokenCatalog.From(fields).Select(token => token.Name).ToArray();

        Assert.Equal(
            [
                "text1",
                "text2",
                "text3",
                "longDateEn",
                "shortDateEn",
                "longDateRu",
                "shortDateRu",
                "longDateFr",
                "shortDateFr"
            ],
            names);
    }

    [Fact]
    public void DateTokens_HasSixTokens()
    {
        Assert.Equal(6, TemplateTokenCatalog.DateTokens.Count);
    }
}
