namespace YTSkedy.Scheduling.Application.Platforms.Publications;

/// <summary>
/// Outcome of starting a publication row before a provider call.
/// <c>Started</c> means this caller created a new <c>Publishing</c> row or
/// conditionally replaced an active <c>Failed</c> row and may proceed.
/// <c>Conflict</c> means another state or writer won, so this caller must not
/// call the provider. The write is conditional, so two concurrent initial
/// attempts or retries cannot both start the same event/platform pair.
/// </summary>
public enum StartPublicationResult
{
    Started,
    Conflict
}
