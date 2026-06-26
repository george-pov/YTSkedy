using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Domain.Test.Platforms;

public class PlatformActionPolicyTests
{
    [Fact]
    public void CanPublish_NotPublishedFutureActiveRow_ReturnsTrue()
    {
        Assert.True(PlatformActionPolicy.CanPublish(
            PublishStatus.NotPublished,
            isOrphaned: false,
            isFuture: true));
    }

    [Fact]
    public void CanPublish_NotPublishedPastRow_ReturnsFalse()
    {
        Assert.False(PlatformActionPolicy.CanPublish(
            PublishStatus.NotPublished,
            isOrphaned: false,
            isFuture: false));
    }

    [Fact]
    public void CanPublish_NotPublishedButOrphaned_ReturnsFalse()
    {
        Assert.False(PlatformActionPolicy.CanPublish(
            PublishStatus.NotPublished,
            isOrphaned: true,
            isFuture: true));
    }

    [Theory]
    [InlineData(PublishStatus.Publishing)]
    [InlineData(PublishStatus.Published)]
    public void CanPublish_PublishingOrPublished_ReturnsFalse(PublishStatus status)
    {
        Assert.False(PlatformActionPolicy.CanPublish(
            status,
            isOrphaned: false,
            isFuture: true));
    }

    [Theory]
    [InlineData(PublishStatus.Published)]
    [InlineData(PublishStatus.Publishing)]
    [InlineData(PublishStatus.NotPublished)]
    public void CanPublish_AnyOrphanedStatus_ReturnsFalse(PublishStatus status)
    {
        Assert.False(PlatformActionPolicy.CanPublish(
            status,
            isOrphaned: true,
            isFuture: true));
    }

    [Fact]
    public void CanDeletePublication_PublishedFutureRowWithExternalId_ReturnsTrue()
    {
        Assert.True(PlatformActionPolicy.CanDeletePublication(
            PublishStatus.Published,
            isOrphaned: false,
            hasExternalResourceId: true,
            isFuture: true));
    }

    [Fact]
    public void CanDeletePublication_PublishedEqualOrPastRow_ReturnsFalse()
    {
        Assert.False(PlatformActionPolicy.CanDeletePublication(
            PublishStatus.Published,
            isOrphaned: false,
            hasExternalResourceId: true,
            isFuture: false));
    }

    [Fact]
    public void CanDeletePublication_PublishedRowWithoutExternalId_ReturnsFalse()
    {
        Assert.False(PlatformActionPolicy.CanDeletePublication(
            PublishStatus.Published,
            isOrphaned: false,
            hasExternalResourceId: false,
            isFuture: true));
    }

    [Fact]
    public void CanDeletePublication_OrphanedPublishedRow_ReturnsFalse()
    {
        Assert.False(PlatformActionPolicy.CanDeletePublication(
            PublishStatus.Published,
            isOrphaned: true,
            hasExternalResourceId: true,
            isFuture: true));
    }

    [Theory]
    [InlineData(PublishStatus.Publishing)]
    [InlineData(PublishStatus.NotPublished)]
    public void CanDeletePublication_NotPublishedStatus_ReturnsFalse(PublishStatus status)
    {
        Assert.False(PlatformActionPolicy.CanDeletePublication(
            status,
            isOrphaned: false,
            hasExternalResourceId: true,
            isFuture: true));
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
