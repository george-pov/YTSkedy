using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms;

public sealed record GetPublishingContentResult(
    GetPublishingContentStatus Status,
    PublishingContentKind? Kind,
    RenderedContent? Content)
{
    public static GetPublishingContentResult Preview(RenderedContent content)
    {
        ArgumentNullException.ThrowIfNull(content);

        return new(
            GetPublishingContentStatus.Found,
            PublishingContentKind.Preview,
            content);
    }

    public static GetPublishingContentResult Snapshot(ContentSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return new(
            GetPublishingContentStatus.Found,
            PublishingContentKind.Snapshot,
            new RenderedContent(snapshot.Title, snapshot.Description));
    }

    public static GetPublishingContentResult ForStatus(GetPublishingContentStatus status) =>
        new(status, null, null);
}
