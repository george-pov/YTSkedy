using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Domain.Test.CalendarEvents;

public sealed class EventTextTypeParserTests
{
    [Theory]
    [InlineData("ShortText", EventTextType.ShortText)]
    [InlineData("shorttext", EventTextType.ShortText)]
    [InlineData("  LongText  ", EventTextType.LongText)]
    [InlineData("LONGTEXT", EventTextType.LongText)]
    public void TryParse_KnownType_ReturnsMatchingType(string value, EventTextType expected)
    {
        var parsed = EventTextTypeParser.TryParse(value, out var type);

        Assert.True(parsed);
        Assert.Equal(expected, type);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Unknown")]
    public void TryParse_UnknownType_ReturnsFalse(string? value)
    {
        var parsed = EventTextTypeParser.TryParse(value, out var type);

        Assert.False(parsed);
        Assert.Equal(default, type);
    }
}
