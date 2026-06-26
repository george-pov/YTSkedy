using YTSkedy.Infrastructure.CalendarEvents;

namespace YTSkedy.Infrastructure.Test.CalendarEvents;

public class CalendarEventStorageKeyTests
{
    [Fact]
    public void NewCalendarEventId_ReturnsNonReusableAddress()
    {
        var scheduledStartUtc = new DateTimeOffset(2026, 6, 6, 17, 0, 0, TimeSpan.Zero);

        var first = CalendarEventStorageKey.NewCalendarEventId(scheduledStartUtc);
        var second = CalendarEventStorageKey.NewCalendarEventId(scheduledStartUtc);

        Assert.NotEqual(first, second);
        Assert.StartsWith("start-20260606T170000Z-", first, StringComparison.Ordinal);
        Assert.True(CalendarEventStorageKey.TryGetAddress(
            first,
            out var parsedScheduledStartUtc,
            out var rowKey));
        Assert.Equal(scheduledStartUtc, parsedScheduledStartUtc);
        Assert.Equal("start-20260606T170000Z", rowKey);
        Assert.All(first[^32..], character => Assert.True(IsLowercaseHex(character)));
    }

    [Fact]
    public void RowKeyForScheduledStart_UsesScheduledStartUtc()
    {
        var rowKey = CalendarEventStorageKey.RowKeyForScheduledStart(
            new DateTimeOffset(2026, 6, 6, 17, 0, 0, TimeSpan.Zero));

        Assert.Equal("start-20260606T170000Z", rowKey);
    }

    [Fact]
    public void TryGetAddress_RejectsUnknownFormat()
    {
        var result = CalendarEventStorageKey.TryGetAddress(
            "6f9619ff8b864fb5bdfd4f5c2f2f16a1",
            out var scheduledStartUtc,
            out var rowKey);

        Assert.False(result);
        Assert.Equal(default, scheduledStartUtc);
        Assert.Equal(string.Empty, rowKey);
    }

    [Fact]
    public void PartitionFilter_EscapesSingleQuotes()
    {
        var filter = CalendarEventStorageKey.PartitionFilter("month'value");

        Assert.Equal("PartitionKey eq 'month''value'", filter);
    }

    private static bool IsLowercaseHex(char character) =>
        character is >= '0' and <= '9' or >= 'a' and <= 'f';
}
