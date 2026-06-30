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
                "title",
                "description",
                "titleRu",
                "descriptionRu",
                "longDate",
                "longDateRu",
                "shortDate",
                "shortDateRu"
            ],
            names);
    }

    [Fact]
    public void All_HasEightTokens()
    {
        Assert.Equal(8, TemplateTokenCatalog.All.Count);
    }
}
