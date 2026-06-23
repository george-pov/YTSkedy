using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Domain.Test.Platforms;

public class PlatformActionPolicyTests
{
    [Fact]
    public void CanPublish_NotPublishedAndActive_ReturnsTrue()
    {
        Assert.True(PlatformActionPolicy.CanPublish(PublishStatus.NotPublished, isOrphaned: false));
    }

    [Fact]
    public void CanPublish_NotPublishedButOrphaned_ReturnsFalse()
    {
        Assert.False(PlatformActionPolicy.CanPublish(PublishStatus.NotPublished, isOrphaned: true));
    }

    [Theory]
    [InlineData(PublishStatus.Publishing)]
    [InlineData(PublishStatus.Published)]
    public void CanPublish_PublishingOrPublished_ReturnsFalse(PublishStatus status)
    {
        Assert.False(PlatformActionPolicy.CanPublish(status, isOrphaned: false));
    }

    [Theory]
    [InlineData(PublishStatus.Published)]
    [InlineData(PublishStatus.Publishing)]
    [InlineData(PublishStatus.NotPublished)]
    public void CanPublish_AnyOrphanedStatus_ReturnsFalse(PublishStatus status)
    {
        Assert.False(PlatformActionPolicy.CanPublish(status, isOrphaned: true));
    }

    [Fact]
    public void BlocksPlatformDelete_Publishing_ReturnsTrue()
    {
        Assert.True(PlatformActionPolicy.BlocksPlatformDelete(PublishStatus.Publishing));
    }

    [Theory]
    [InlineData(PublishStatus.NotPublished)]
    [InlineData(PublishStatus.Published)]
    public void BlocksPlatformDelete_NotPublishingStatus_ReturnsFalse(PublishStatus status)
    {
        Assert.False(PlatformActionPolicy.BlocksPlatformDelete(status));
    }
}
