namespace YTSkedy.Scheduling.Application.Platforms.Publications;

/// <summary>
/// Outcome of starting a publication row before a provider call.
/// <c>Started</c> means this caller created the <c>Publishing</c> row and may
/// proceed; <c>Conflict</c> means a row already exists (already publishing,
/// already published, or orphaned), so this caller must not call the provider.
/// The start write is conditional, so two concurrent publish attempts cannot
/// both start the same event/platform pair.
/// </summary>
public enum StartPublicationResult
{
    Started,
    Conflict
}
