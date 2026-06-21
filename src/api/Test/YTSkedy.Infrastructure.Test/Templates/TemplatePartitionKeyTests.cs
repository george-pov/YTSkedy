using YTSkedy.Infrastructure.Templates;
using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Infrastructure.Test.Templates;

public class TemplatePartitionKeyTests
{
    [Fact]
    public void ForType_YouTube_ReturnsYouTubePartitionKey()
    {
        Assert.Equal("templates-youtube", TemplatePartitionKey.ForType(TemplateType.YouTube));
    }

    [Fact]
    public void ForType_WordPress_ReturnsWordPressPartitionKey()
    {
        Assert.Equal("templates-wordpress", TemplatePartitionKey.ForType(TemplateType.WordPress));
    }

    [Fact]
    public void ForType_UnknownType_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => TemplatePartitionKey.ForType((TemplateType)999));
    }
}
