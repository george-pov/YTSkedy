namespace YTSkedy.Scheduling.Application.Platforms;

/// <summary>
/// Outcome of reserving a publication row before a provider call.
/// <c>Reserved</c> means this caller created the <c>Publishing</c> row and may
/// proceed; <c>Conflict</c> means a row already exists (already publishing,
/// already published, or orphaned), so this caller must not call the provider.
/// The reservation write is conditional, so two concurrent publish attempts
/// cannot both reserve the same event/platform pair.
/// </summary>
public enum ReservePublicationResult
{
    Reserved,
    Conflict
}
