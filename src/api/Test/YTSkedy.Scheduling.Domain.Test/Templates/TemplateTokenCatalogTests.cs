using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Scheduling.Domain.Test.Templates;

public class TemplateTokenCatalogTests
{
    [Fact]
    public void All_ReturnsTheCodeDefinedTokenNamesInOrder()
    {
        var names = TemplateTokenCatalog.All.Select(token => token.Name).ToArray();

        Assert.Equal(
            [
                "localizedDate",
                "localizedTime",
                "youTubeBroadcastId",
                "calendarEventTitle"
            ],
            names);
    }

    [Fact]
    public void All_HasFourTokens()
    {
        Assert.Equal(4, TemplateTokenCatalog.All.Count);
    }
}
