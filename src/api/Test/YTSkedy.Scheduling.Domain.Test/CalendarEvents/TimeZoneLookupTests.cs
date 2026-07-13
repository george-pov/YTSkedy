using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Domain.Test.CalendarEvents;

public sealed class TimeZoneLookupTests
{
    [Fact]
    public void TryFind_IanaTimeZone_ReturnsTimeZone()
    {
        var found = TimeZoneLookup.TryFind("America/Vancouver", out var timeZone);

        Assert.True(found);
        Assert.NotNull(timeZone);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Unknown/Zone")]
    public void TryFind_InvalidValue_ReturnsFalse(string? value) =>
        Assert.False(TimeZoneLookup.TryFind(value, out _));
}
