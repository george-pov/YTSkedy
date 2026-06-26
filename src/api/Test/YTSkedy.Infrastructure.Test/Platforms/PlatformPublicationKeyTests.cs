using YTSkedy.Infrastructure.Platforms;

namespace YTSkedy.Infrastructure.Test.Platforms;

public class PlatformPublicationKeyTests
{
    [Fact]
    public void PartitionKeyFor_PrefixesEvent()
    {
        Assert.Equal(
            "event-f81d4fae7dec11d0a76500a0c91e6bf6",
            PlatformPublicationKey.PartitionKeyFor("f81d4fae7dec11d0a76500a0c91e6bf6"));
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
            "event-f81d4fae7dec11d0a76500a0c91e6bf6",
            PlatformPublicationKey.EscapeLiteral("event-f81d4fae7dec11d0a76500a0c91e6bf6"));
    }

    [Fact]
    public void EscapeLiteral_SingleQuotes_DoublesThem()
    {
        Assert.Equal("o''brien", PlatformPublicationKey.EscapeLiteral("o'brien"));
    }
}
