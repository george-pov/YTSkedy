using YTSkedy.Scheduling.Application.Platforms.PublicationThumbnails;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Test;

public sealed class ThumbnailPublicationPolicyTests
{
    [Theory]
    [InlineData(PlatformType.YouTube, true)]
    [InlineData(PlatformType.WordPress, false)]
    public void SupportsThumbnails_PlatformType_ReturnsCapability(
        PlatformType platformType,
        bool expected)
    {
        var supportsThumbnails = ThumbnailPublicationPolicy.SupportsThumbnails(platformType);

        Assert.Equal(expected, supportsThumbnails);
    }

    [Fact]
    public void InitialStatusFor_YouTube_ReturnsNotConfigured()
    {
        var status = ThumbnailPublicationPolicy.InitialStatusFor(PlatformType.YouTube);

        Assert.Equal(ThumbnailPublishStatus.NotConfigured, status);
    }

    [Fact]
    public void InitialStatusFor_WordPress_ReturnsNull()
    {
        var status = ThumbnailPublicationPolicy.InitialStatusFor(PlatformType.WordPress);

        Assert.Null(status);
    }
}
