namespace YTSkedy.Scheduling.Application.CalendarEvents.Thumbnails;

public sealed record ThumbnailValidationResult(
    ThumbnailValidationError? Error,
    int Width,
    int Height)
{
    public bool IsValid => Error is null;

    public static ThumbnailValidationResult Valid(int width, int height) =>
        new(null, width, height);

    public static ThumbnailValidationResult Invalid(ThumbnailValidationError error) =>
        new(error, 0, 0);
}
