using YTSkedy.Scheduling.Application.Templates;

namespace YTSkedy.Scheduling.Application.Test;

public class ListTemplateTokensHandlerTests
{
    [Fact]
    public void Handle_ReturnsTheCodeDefinedTokenCatalog()
    {
        var handler = new ListTemplateTokensHandler();

        var tokens = handler.Handle();

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
            tokens.Select(token => token.Name));
    }
}
