using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Domain.Test.CalendarEvents;

public sealed class StartDefaultsTests
{
    [Fact]
    public void Constructor_AllNull_ReturnsEmptyValues()
    {
        var defaults = new StartDefaults(null, null, null);

        Assert.Null(defaults.DayOfWeek);
        Assert.Null(defaults.LocalTime);
        Assert.Null(defaults.TimeZoneId);
        Assert.Equal(defaults, StartDefaults.Empty);
    }

    [Theory]
    [InlineData(DayOfWeek.Monday, null, null)]
    [InlineData(null, "10:30", null)]
    [InlineData(null, null, "America/Vancouver")]
    public void Constructor_PartialValues_PreservesValues(
        DayOfWeek? dayOfWeek,
        string? localTime,
        string? timeZoneId)
    {
        TimeOnly? time = localTime is null ? null : TimeOnly.Parse(localTime);

        var defaults = new StartDefaults(dayOfWeek, time, timeZoneId);

        Assert.Equal(dayOfWeek, defaults.DayOfWeek);
        Assert.Equal(time, defaults.LocalTime);
        Assert.Equal(timeZoneId, defaults.TimeZoneId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Unknown/Zone")]
    public void Constructor_InvalidTimeZone_Throws(string timeZoneId) =>
        Assert.Throws<ArgumentException>(() => new StartDefaults(null, null, timeZoneId));
}
