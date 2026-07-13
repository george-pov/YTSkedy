using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.CalendarEvents.Starts;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Test;

public sealed class ScheduledStartConverterTests
{
    [Fact]
    public void Convert_ValidScheduledStart_ReturnsUtcInstant()
    {
        var result = ScheduledStartConverter.Convert(
            new ScheduledStart(
                new DateTime(2026, 6, 15, 10, 0, 0),
                "America/Vancouver"));

        Assert.Equal(
            new DateTimeOffset(2026, 6, 15, 17, 0, 0, TimeSpan.Zero),
            result.ScheduledStartUtc);
    }

    [Fact]
    public void Convert_RecognizedIanaUtcZone_ReturnsSameUtcInstant()
    {
        var result = ScheduledStartConverter.Convert(
            new ScheduledStart(
                new DateTime(2026, 6, 15, 10, 0, 0),
                "Etc/UTC"));

        Assert.Equal(
            new DateTimeOffset(2026, 6, 15, 10, 0, 0, TimeSpan.Zero),
            result.ScheduledStartUtc);
    }

    [Fact]
    public void Convert_InvalidLocalTime_ThrowsInvalidScheduledStartException()
    {
        var exception = Assert.Throws<InvalidScheduledStartException>(() =>
            ScheduledStartConverter.Convert(
                new ScheduledStart(
                    new DateTime(2026, 3, 8, 2, 30, 0),
                    "America/Vancouver")));

        Assert.Equal(
            "Scheduled start time does not exist in the specified time zone.",
            exception.ValidationError);
    }

    [Fact]
    public void Convert_AmbiguousLocalTime_ThrowsInvalidScheduledStartException()
    {
        var exception = Assert.Throws<InvalidScheduledStartException>(() =>
            ScheduledStartConverter.Convert(
                new ScheduledStart(
                    new DateTime(2026, 11, 1, 1, 30, 0),
                    "America/Vancouver")));

        Assert.Equal(
            "Scheduled start time is ambiguous in the specified time zone.",
            exception.ValidationError);
    }

    [Fact]
    public void Convert_MissingTimeZone_ThrowsInvalidScheduledStartException()
    {
        var exception = Assert.Throws<InvalidScheduledStartException>(() =>
            ScheduledStartConverter.Convert(
                new ScheduledStart(
                    new DateTime(2026, 6, 15, 10, 0, 0),
                    "   ")));

        Assert.Equal(
            "Start local date-time and time zone id are required.",
            exception.ValidationError);
    }

    [Fact]
    public void Convert_UnknownTimeZone_ThrowsInvalidScheduledStartException()
    {
        var exception = Assert.Throws<InvalidScheduledStartException>(() =>
            ScheduledStartConverter.Convert(
                new ScheduledStart(
                    new DateTime(2026, 6, 15, 10, 0, 0),
                    "Unknown/Zone")));

        Assert.Equal("Time zone id is not recognized.", exception.ValidationError);
    }
}
