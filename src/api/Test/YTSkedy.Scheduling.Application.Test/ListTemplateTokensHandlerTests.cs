using YTSkedy.Scheduling.Application.Templates;
using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Scheduling.Application.Test;

public class ListTemplateTokensHandlerTests
{
    [Fact]
    public void Handle_ReturnsTheCodeDefinedTokenCatalog()
    {
        var handler = new ListTemplateTokensHandler();

        var tokens = handler.Handle();

        Assert.Equal(TemplateTokenCatalog.All, tokens);
    }
}
