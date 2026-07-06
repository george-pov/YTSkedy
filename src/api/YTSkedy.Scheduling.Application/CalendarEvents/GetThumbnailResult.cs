namespace YTSkedy.Scheduling.Application.CalendarEvents;

public sealed record GetThumbnailResult(
    GetThumbnailStatus Status,
    ThumbnailContent? Content = null)
{
    public static GetThumbnailResult Found(ThumbnailContent content) =>
        new(GetThumbnailStatus.Found, content);

    public static GetThumbnailResult EventNotFound { get; } =
        new(GetThumbnailStatus.EventNotFound);

    public static GetThumbnailResult ThumbnailNotFound { get; } =
        new(GetThumbnailStatus.ThumbnailNotFound);
}
