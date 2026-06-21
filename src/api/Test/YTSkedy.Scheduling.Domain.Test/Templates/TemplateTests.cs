using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Scheduling.Domain.Test.Templates;

public class TemplateTests
{
    [Fact]
    public void Constructor_ValidInput_SetsProperties()
    {
        var template = new Template(
            "Weeknight stream",
            TemplateType.YouTube,
            "Live at {{ localizedTime }}");

        Assert.Equal("Weeknight stream", template.Name);
        Assert.Equal(TemplateType.YouTube, template.Type);
        Assert.Equal("Live at {{ localizedTime }}", template.Content);
    }

    [Fact]
    public void Constructor_NameAtMaxLength_IsAccepted()
    {
        var name = new string('n', Template.MaxNameLength);

        var template = new Template(name, TemplateType.WordPress, "content");

        Assert.Equal(name, template.Name);
    }

    [Fact]
    public void Constructor_ContentAtMaxLength_IsAccepted()
    {
        var content = new string('c', Template.MaxContentLength);

        var template = new Template("name", TemplateType.YouTube, content);

        Assert.Equal(content, template.Content);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyName_Throws(string? name)
    {
        Assert.Throws<ArgumentException>(
            () => new Template(name!, TemplateType.YouTube, "content"));
    }

    [Fact]
    public void Constructor_NameAboveMaxLength_Throws()
    {
        var name = new string('n', Template.MaxNameLength + 1);

        Assert.Throws<ArgumentException>(
            () => new Template(name, TemplateType.YouTube, "content"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyContent_Throws(string? content)
    {
        Assert.Throws<ArgumentException>(
            () => new Template("name", TemplateType.YouTube, content!));
    }

    [Fact]
    public void Constructor_ContentAboveMaxLength_Throws()
    {
        var content = new string('c', Template.MaxContentLength + 1);

        Assert.Throws<ArgumentException>(
            () => new Template("name", TemplateType.YouTube, content));
    }

    [Theory]
    [InlineData("a")]
    [InlineData("Weeknight stream")]
    public void IsValidName_NonEmptyWithinLimit_ReturnsTrue(string name)
    {
        Assert.True(Template.IsValidName(name));
    }

    [Fact]
    public void IsValidName_AtMaxLength_ReturnsTrue()
    {
        Assert.True(Template.IsValidName(new string('n', Template.MaxNameLength)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValidName_NullOrWhiteSpace_ReturnsFalse(string? name)
    {
        Assert.False(Template.IsValidName(name));
    }

    [Fact]
    public void IsValidName_AboveMaxLength_ReturnsFalse()
    {
        Assert.False(Template.IsValidName(new string('n', Template.MaxNameLength + 1)));
    }

    [Fact]
    public void IsValidContent_AtMaxLength_ReturnsTrue()
    {
        Assert.True(Template.IsValidContent(new string('c', Template.MaxContentLength)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValidContent_NullOrWhiteSpace_ReturnsFalse(string? content)
    {
        Assert.False(Template.IsValidContent(content));
    }

    [Fact]
    public void IsValidContent_AboveMaxLength_ReturnsFalse()
    {
        Assert.False(Template.IsValidContent(new string('c', Template.MaxContentLength + 1)));
    }
}
