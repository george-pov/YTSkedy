using YTSkedy.Infrastructure.CalendarEvents;

namespace YTSkedy.Infrastructure.Test.CalendarEvents;

public class CalendarEventStorageKeyTests
{
    [Fact]
    public void NewCalendarEventId_ReturnsOpaqueLowercaseGuid()
    {
        var calendarEventId = CalendarEventStorageKey.NewCalendarEventId();

        Assert.Equal(32, calendarEventId.Length);
        Assert.All(calendarEventId, character => Assert.True(IsLowercaseHex(character)));
    }

    [Fact]
    public void RowKeyFor_ValidCalendarEventId_ReturnsEventRowKey()
    {
        var rowKey = CalendarEventStorageKey.RowKeyFor(
            "6f9619ff8b864fb5bdfd4f5c2f2f16a1");

        Assert.Equal("event-6f9619ff8b864fb5bdfd4f5c2f2f16a1", rowKey);
    }

    [Fact]
    public void TryGetAddress_RejectsUnknownFormat()
    {
        var result = CalendarEventStorageKey.TryGetAddress(
            "not-a-calendar-event-id",
            out var partitionKey,
            out var rowKey);

        Assert.False(result);
        Assert.Equal(string.Empty, partitionKey);
        Assert.Equal(string.Empty, rowKey);
    }

    [Fact]
    public void TryGetAddress_GuidId_ReturnsCalendarEventsAddress()
    {
        var result = CalendarEventStorageKey.TryGetAddress(
            "6f9619ff8b864fb5bdfd4f5c2f2f16a1",
            out var partitionKey,
            out var rowKey);

        Assert.True(result);
        Assert.Equal("calendar-events", partitionKey);
        Assert.Equal("event-6f9619ff8b864fb5bdfd4f5c2f2f16a1", rowKey);
    }

    [Fact]
    public void TryGetAddress_LegacyScheduledStartId_ReturnsFalse()
    {
        var result = CalendarEventStorageKey.TryGetAddress(
            "start-20260606T170000Z-6f9619ff8b864fb5bdfd4f5c2f2f16a1",
            out var partitionKey,
            out var rowKey);

        Assert.False(result);
        Assert.Equal(string.Empty, partitionKey);
        Assert.Equal(string.Empty, rowKey);
    }

    [Fact]
    public void PartitionFilter_ReturnsCalendarEventsPartitionFilter()
    {
        var filter = CalendarEventStorageKey.PartitionFilter();

        Assert.Equal("PartitionKey eq 'calendar-events'", filter);
    }

    private static bool IsLowercaseHex(char character) =>
        character is >= '0' and <= '9' or >= 'a' and <= 'f';
}
