namespace YTSkedy.Scheduling.Application.Platforms;

/// <summary>
/// Updates the name and publish settings of an existing platform located by id.
/// The repository owns the locate, the name-uniqueness check on rename, and the
/// not-found outcome, so the handler forwards the command and returns the
/// repository result unchanged.
/// </summary>
public sealed class UpdatePlatformHandler(IPlatformModifier platforms)
{
    public async Task<UpdatePlatformResult> HandleAsync(
        UpdatePlatformCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        return await platforms.UpdateAsync(
            command.PlatformId,
            command.Name,
            command.ReferenceKey,
            command.PublishSettings,
            cancellationToken);
    }
}
