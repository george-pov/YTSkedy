using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Domain.Test.Platforms;

public class PlatformActionPolicyTests
{
    [Theory]
    [InlineData(PublishStatus.NotPublished, false, true, true)]
    [InlineData(PublishStatus.NotPublished, false, false, false)]
    [InlineData(PublishStatus.NotPublished, true, true, false)]
    [InlineData(PublishStatus.Publishing, false, true, false)]
    [InlineData(PublishStatus.Publishing, true, true, false)]
    [InlineData(PublishStatus.Published, false, true, false)]
    [InlineData(PublishStatus.Published, true, true, false)]
    public void CanPublish_State_ReturnsExpected(
        PublishStatus status,
        bool isOrphaned,
        bool isFuture,
        bool expected)
    {
        var actual = PlatformActionPolicy.CanPublish(status, isOrphaned, isFuture);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(PublishStatus.Published, false, true, true, true)]
    [InlineData(PublishStatus.Published, false, true, false, false)]
    [InlineData(PublishStatus.Published, false, false, true, false)]
    [InlineData(PublishStatus.Published, true, true, true, false)]
    [InlineData(PublishStatus.Publishing, false, true, true, false)]
    [InlineData(PublishStatus.NotPublished, false, true, true, false)]
    public void CanDeletePublication_State_ReturnsExpected(
        PublishStatus status,
        bool isOrphaned,
        bool hasExternalResourceId,
        bool isFuture,
        bool expected)
    {
        var actual = PlatformActionPolicy.CanDeletePublication(
            status,
            isOrphaned,
            hasExternalResourceId,
            isFuture);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(PublishStatus.Publishing, true)]
    [InlineData(PublishStatus.NotPublished, false)]
    [InlineData(PublishStatus.Published, false)]
    public void BlocksPlatformDelete_Status_ReturnsExpected(
        PublishStatus status,
        bool expected)
    {
        Assert.Equal(expected, PlatformActionPolicy.BlocksPlatformDelete(status));
    }

    [Theory]
    [InlineData(PublishStatus.NotPublished, false, false, true)]
    [InlineData(PublishStatus.Publishing, false, true, true)]
    [InlineData(PublishStatus.Published, true, true, true)]
    [InlineData(PublishStatus.Published, false, false, false)]
    public void CanPreviewPublishingContent_State_ReturnsExpected(
        PublishStatus status,
        bool isOrphaned,
        bool hasContentSnapshot,
        bool expected)
    {
        var actual = PlatformActionPolicy.CanPreviewPublishingContent(
            status,
            isOrphaned,
            hasContentSnapshot);

        Assert.Equal(expected, actual);
    }
}
