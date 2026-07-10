using System.Text.Json;
using YTSkedy.Infrastructure.CalendarEvents;

namespace YTSkedy.Infrastructure.Test.CalendarEvents;

public sealed class PublishedPlatformIdsJsonTests
{
    private const string CalendarEventId = "6f9619ff8b864fb5bdfd4f5c2f2f16a1";

    [Fact]
    public void Serialize_UnorderedDuplicateIds_WritesDistinctOrdinalSortedArray()
    {
        var json = PublishedPlatformIdsJson.Serialize(
            ["platform-b", "platform-a", "Platform-a", "platform-a"]);

        using var document = JsonDocument.Parse(json);
        Assert.Equal(
            ["Platform-a", "platform-a", "platform-b"],
            document.RootElement.EnumerateArray().Select(item => item.GetString()));
    }

    [Fact]
    public void Serialize_EmptySet_WritesEmptyArray()
    {
        var json = PublishedPlatformIdsJson.Serialize([]);

        Assert.Equal("[]", json);
    }

    [Fact]
    public void Deserialize_DuplicateIds_ReturnsDistinctOrdinalSet()
    {
        var platformIds = PublishedPlatformIdsJson.Deserialize(
            """["platform-a","Platform-a","platform-a"]""",
            CalendarEventId);

        Assert.Equal(2, platformIds.Count);
        Assert.Contains("platform-a", platformIds);
        Assert.Contains("Platform-a", platformIds);
        Assert.DoesNotContain("PLATFORM-A", platformIds);
    }

    [Fact]
    public void Deserialize_EmptyArray_ReturnsEmptySet()
    {
        var platformIds = PublishedPlatformIdsJson.Deserialize("[]", CalendarEventId);

        Assert.Empty(platformIds);
    }

    [Fact]
    public void Deserialize_MissingJson_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(
            () => PublishedPlatformIdsJson.Deserialize(null, CalendarEventId));
    }

    [Fact]
    public void Deserialize_BlankJson_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(
            () => PublishedPlatformIdsJson.Deserialize(" ", CalendarEventId));
    }

    [Fact]
    public void Deserialize_MalformedJson_ThrowsInvalidOperationException()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => PublishedPlatformIdsJson.Deserialize("[", CalendarEventId));

        Assert.Contains(CalendarEventId, exception.Message);
    }

    [Fact]
    public void Deserialize_NonArrayJson_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(
            () => PublishedPlatformIdsJson.Deserialize("{}", CalendarEventId));
    }

    [Fact]
    public void Deserialize_NullJsonValue_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(
            () => PublishedPlatformIdsJson.Deserialize("null", CalendarEventId));
    }

    [Fact]
    public void Deserialize_NullEntry_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(
            () => PublishedPlatformIdsJson.Deserialize(
                """["platform-a",null]""",
                CalendarEventId));
    }

    [Fact]
    public void Deserialize_BlankId_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(
            () => PublishedPlatformIdsJson.Deserialize(
                """["platform-a"," "]""",
                CalendarEventId));
    }
}
