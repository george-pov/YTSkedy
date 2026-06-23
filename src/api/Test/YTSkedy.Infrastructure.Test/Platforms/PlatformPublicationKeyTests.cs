using YTSkedy.Infrastructure.Platforms;

namespace YTSkedy.Infrastructure.Test.Platforms;

public class PlatformPublicationKeyTests
{
    [Fact]
    public void PartitionKeyFor_PrefixesEvent()
    {
        Assert.Equal(
            "event-20260615T170000Z",
            PlatformPublicationKey.PartitionKeyFor("20260615T170000Z"));
    }

    [Fact]
    public void RowKeyFor_PrefixesPlatform()
    {
        Assert.Equal(
            "platform-4fb4a32f3f344de1a7c3a9f4a2f94918",
            PlatformPublicationKey.RowKeyFor("4fb4a32f3f344de1a7c3a9f4a2f94918"));
    }

    [Fact]
    public void EscapeLiteral_NoQuotes_ReturnsUnchanged()
    {
        Assert.Equal(
            "event-20260615T170000Z",
            PlatformPublicationKey.EscapeLiteral("event-20260615T170000Z"));
    }

    [Fact]
    public void EscapeLiteral_SingleQuotes_DoublesThem()
    {
        Assert.Equal("o''brien", PlatformPublicationKey.EscapeLiteral("o'brien"));
    }
}
