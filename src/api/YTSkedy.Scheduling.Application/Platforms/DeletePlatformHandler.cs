namespace YTSkedy.Scheduling.Application.Platforms;

/// <summary>
/// Deletes a configured platform located by id. The repository owns the locate
/// and the not-found outcome, so the handler forwards the command and returns
/// the repository result unchanged. Blocking deletion while a publication row is
/// <c>Publishing</c> and orphaning published rows are layered on once platform
/// publication state exists.
/// </summary>
public sealed class DeletePlatformHandler(IPlatformRepository platforms)
{
    public async Task<DeletePlatformResult> HandleAsync(
        DeletePlatformCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        return await platforms.DeleteAsync(command.PlatformId, cancellationToken);
    }
}
