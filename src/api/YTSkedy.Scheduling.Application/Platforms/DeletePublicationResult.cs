namespace YTSkedy.Scheduling.Application.Platforms;

/// <summary>
/// Result of deleting one platform publication. Successful statuses carry the
/// recomputed event-platform row so the HTTP boundary can return the same shape
/// used by the calendar event details response.
/// </summary>
public sealed record DeletePublicationResult(
    DeletePublicationStatus Status,
    EventPlatformView? Platform)
{
    public static DeletePublicationResult ForStatus(DeletePublicationStatus status) =>
        new(status, null);

    public static DeletePublicationResult Success(
        DeletePublicationStatus status,
        EventPlatformView platform) =>
        new(status, platform);
}
