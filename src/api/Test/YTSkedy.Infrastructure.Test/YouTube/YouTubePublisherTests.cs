using Microsoft.Extensions.Logging.Abstractions;
using YTSkedy.Infrastructure.YouTube;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Infrastructure.Test.YouTube;

public class YouTubePublisherTests
{
    [Fact]
    public void Type_IsYouTube()
    {
        var publisher = new YouTubePublisher(NullLogger<YouTubePublisher>.Instance);

        Assert.Equal(PlatformType.YouTube, publisher.Type);
    }

    [Fact]
    public async Task PublishAsync_NonYouTubeSettings_Throws()
    {
        var publisher = new YouTubePublisher(NullLogger<YouTubePublisher>.Instance);
        var request = Request(new OtherSettings());

        await Assert.ThrowsAsync<PlatformPublishException>(
            () => publisher.PublishAsync(request, CancellationToken.None));
    }

    private static PlatformPublishRequest Request(PublishSettings settings) =>
        new(
            "20260615T170000Z",
            "4fb4a32f3f344de1a7c3a9f4a2f94918",
            settings,
            "English title",
            "English description",
            new DateTimeOffset(2026, 6, 25, 17, 0, 0, TimeSpan.Zero));

    private sealed record OtherSettings : PublishSettings;
}
