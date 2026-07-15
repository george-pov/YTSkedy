using YTSkedy.Scheduling.Application.CalendarEvents.Starts;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Test.CalendarEvents.Starts;

public sealed class DefaultStartCalculatorTests
{
    [Fact]
    public void Calculate_NoDefaults_ReturnsEmptySuggestion()
    {
        var result = Calculate(StartDefaults.Empty);

        Assert.Equal(new DefaultStart(null, null, null), result);
    }

    [Fact]
    public void Calculate_PartialDefaults_ReturnsIndependentValues()
    {
        var defaults = new StartDefaults(DayOfWeek.Sunday, null, null);

        var noZone = Calculate(defaults);
        var fallback = Calculate(defaults, "America/Vancouver");

        Assert.Equal(new DefaultStart(null, null, null), noZone);
        Assert.Equal(new DateOnly(2026, 7, 12), fallback.LocalDate);
        Assert.Equal("America/Vancouver", fallback.TimeZoneId);
    }

    [Fact]
    public void Calculate_SavedZoneWinsAndSameDayFutureIsSelected()
    {
        var defaults = new StartDefaults(
            DayOfWeek.Sunday,
            new TimeOnly(10, 0),
            "America/Vancouver");

        var result = Calculate(defaults, "America/New_York");

        Assert.Equal(new DateOnly(2026, 7, 12), result.LocalDate);
        Assert.Equal("America/Vancouver", result.TimeZoneId);
    }

    [Theory]
    [InlineData("2026-07-12T17:00:00+00:00")]
    [InlineData("2026-07-12T18:00:00+00:00")]
    public void Calculate_EqualOrElapsedCandidate_AdvancesOneWeek(string now)
    {
        var result = Calculate(
            new StartDefaults(DayOfWeek.Sunday, new TimeOnly(10, 0), "America/Vancouver"),
            now: DateTimeOffset.Parse(now));

        Assert.Equal(new DateOnly(2026, 7, 19), result.LocalDate);
    }

    [Fact]
    public void Calculate_OccupiedUtcInstants_AdvancesSeveralWeeks()
    {
        var occupied = new HashSet<DateTimeOffset>
        {
            DateTimeOffset.Parse("2026-07-12T17:00:00+00:00"),
            DateTimeOffset.Parse("2026-07-19T17:00:00+00:00")
        };

        var result = Calculate(
            new StartDefaults(DayOfWeek.Sunday, new TimeOnly(10, 0), "America/Vancouver"),
            occupied: occupied);

        Assert.Equal(new DateOnly(2026, 7, 26), result.LocalDate);
    }

    [Fact]
    public void Calculate_DifferentLocalValueWithSameUtcInstant_IsOccupied()
    {
        var occupied = new HashSet<DateTimeOffset>
        {
            DateTimeOffset.Parse("2026-07-12T17:00:00+00:00")
        };

        var result = Calculate(
            new StartDefaults(DayOfWeek.Sunday, new TimeOnly(13, 0), "America/New_York"),
            occupied: occupied);

        Assert.Equal(new DateOnly(2026, 7, 19), result.LocalDate);
    }

    [Fact]
    public void Calculate_OffsetChange_PreservesWeeklyWallClockTime()
    {
        var result = DefaultStartCalculator.Calculate(
            new StartDefaults(DayOfWeek.Sunday, new TimeOnly(10, 0), "America/Los_Angeles"),
            null,
            new HashSet<DateTimeOffset>
            {
                DateTimeOffset.Parse("2026-10-25T17:00:00+00:00")
            },
            DateTimeOffset.Parse("2026-10-25T16:00:00+00:00"));

        Assert.Equal(new DateOnly(2026, 11, 1), result.LocalDate);
        Assert.Equal(new TimeOnly(10, 0), result.LocalTime);
        Assert.True(result.LocalDate.HasValue);
        Assert.True(result.LocalTime.HasValue);
        var conversion = ScheduledStartConverter.Convert(
            new ScheduledStart(
                result.LocalDate.GetValueOrDefault().ToDateTime(
                    result.LocalTime.GetValueOrDefault()),
                result.TimeZoneId!));
        Assert.Equal(DateTimeOffset.Parse("2026-11-01T18:00:00+00:00"), conversion.ScheduledStartUtc);
    }

    [Fact]
    public void Calculate_InvalidAndAmbiguousDstCandidates_AdvanceWeekly()
    {
        var spring = DefaultStartCalculator.Calculate(
            new StartDefaults(DayOfWeek.Sunday, new TimeOnly(2, 30), "America/Vancouver"),
            null,
            new HashSet<DateTimeOffset>(),
            DateTimeOffset.Parse("2026-03-08T09:00:00+00:00"));
        var fall = DefaultStartCalculator.Calculate(
            new StartDefaults(DayOfWeek.Sunday, new TimeOnly(1, 30), "America/Vancouver"),
            null,
            new HashSet<DateTimeOffset>(),
            DateTimeOffset.Parse("2026-11-01T07:00:00+00:00"));

        Assert.Equal(new DateOnly(2026, 3, 15), spring.LocalDate);
        Assert.Equal(new TimeOnly(2, 30), spring.LocalTime);
        Assert.Equal(new DateOnly(2026, 11, 8), fall.LocalDate);
        Assert.Equal(new TimeOnly(1, 30), fall.LocalTime);
    }

    private static DefaultStart Calculate(
        StartDefaults defaults,
        string? fallback = null,
        IReadOnlySet<DateTimeOffset>? occupied = null,
        DateTimeOffset? now = null) =>
        DefaultStartCalculator.Calculate(
            defaults,
            fallback,
            occupied ?? new HashSet<DateTimeOffset>(),
            now ?? DateTimeOffset.Parse("2026-07-12T16:00:00+00:00"));
}
