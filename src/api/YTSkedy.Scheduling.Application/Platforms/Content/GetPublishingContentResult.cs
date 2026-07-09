using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms.Content;

public sealed record GetPublishingContentResult(
    GetPublishingContentStatus Status,
    PublishingContentType? Type,
    RenderedContent? Content)
{
    public static GetPublishingContentResult Preview(RenderedContent content)
    {
        ArgumentNullException.ThrowIfNull(content);

        return new(
            GetPublishingContentStatus.Found,
            PublishingContentType.Preview,
            content);
    }

    public static GetPublishingContentResult Snapshot(ContentSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return new(
            GetPublishingContentStatus.Found,
            PublishingContentType.Snapshot,
            new RenderedContent(snapshot.Title, snapshot.Description));
    }

    public static GetPublishingContentResult ForStatus(GetPublishingContentStatus status) =>
        new(status, null, null);
}
