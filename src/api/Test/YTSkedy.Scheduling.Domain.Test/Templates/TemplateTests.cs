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
            "Live on {{ longDateEn }}");

        Assert.Equal("Weeknight stream", template.Name);
        Assert.Equal(TemplateType.YouTube, template.Type);
        Assert.Equal("Live on {{ longDateEn }}", template.Content);
    }

    [Fact]
    public void Constructor_NameWithSurroundingWhitespace_PreservesValue()
    {
        var template = new Template(
            "  Weeknight stream  ",
            TemplateType.YouTube,
            "content");

        Assert.Equal("  Weeknight stream  ", template.Name);
    }

    [Fact]
    public void Constructor_ContentWithSurroundingWhitespace_PreservesValue()
    {
        var template = new Template(
            "name",
            TemplateType.YouTube,
            "  Live on {{ longDateEn }}  ");

        Assert.Equal("  Live on {{ longDateEn }}  ", template.Content);
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
    [MemberData(nameof(ValidNameCases))]
    public void IsValidName_Value_ReturnsExpected(string? name, bool expected)
    {
        Assert.Equal(expected, Template.IsValidName(name));
    }

    [Theory]
    [MemberData(nameof(ValidContentCases))]
    public void IsValidContent_Value_ReturnsExpected(string? content, bool expected)
    {
        Assert.Equal(expected, Template.IsValidContent(content));
    }

    public static TheoryData<string?, bool> ValidNameCases => new()
    {
        { "a", true },
        { "Weeknight stream", true },
        { new string('n', Template.MaxNameLength), true },
        { $"  {new string('n', Template.MaxNameLength)}  ", false },
        { null, false },
        { "", false },
        { "   ", false },
        { new string('n', Template.MaxNameLength + 1), false }
    };

    public static TheoryData<string?, bool> ValidContentCases => new()
    {
        { new string('c', Template.MaxContentLength), true },
        { $"  {new string('c', Template.MaxContentLength)}  ", false },
        { null, false },
        { "", false },
        { "   ", false },
        { new string('c', Template.MaxContentLength + 1), false }
    };
}
