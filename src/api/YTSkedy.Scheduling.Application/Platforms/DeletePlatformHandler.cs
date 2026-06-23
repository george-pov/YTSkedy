namespace YTSkedy.Scheduling.Application.Platforms;

/// <summary>
/// Deletes a configured platform located by id and preserves its publish
/// history. A missing platform is <c>NotFound</c>. Deletion is blocked with
/// <c>Conflict</c> while any publication row for the platform is <c>Publishing</c>
/// so a provider call cannot race the delete. Published rows are orphaned (kept
/// as read-only history with the platform-deleted instant stamped) before the
/// platform row is removed, so the history survives even if the delete is
/// interrupted afterward.
/// </summary>
public sealed class DeletePlatformHandler(
    IPlatformReader platforms,
    IPlatformModifier platformModifier,
    IPlatformPublicationReader publications,
    IPlatformPublicationRepository publicationRepository)
{
    public async Task<DeletePlatformResult> HandleAsync(
        DeletePlatformCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var platform = await platforms.GetAsync(command.PlatformId, cancellationToken);

        if (platform is null)
        {
            return DeletePlatformResult.NotFound;
        }

        var publishing = await publications.ListPublishingByPlatformAsync(
            command.PlatformId,
            cancellationToken);

        if (publishing.Count > 0)
        {
            return DeletePlatformResult.Conflict;
        }

        // Orphan before removing the platform row so published history is never
        // left without a deleted marker if the delete is interrupted afterward.
        await publicationRepository.OrphanPublishedByPlatformAsync(
            command.PlatformId,
            cancellationToken);

        return await platformModifier.DeleteAsync(command.PlatformId, cancellationToken);
    }
}
