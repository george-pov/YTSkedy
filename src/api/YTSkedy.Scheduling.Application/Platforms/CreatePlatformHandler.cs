namespace YTSkedy.Scheduling.Application.Platforms;

/// <summary>
/// Creates a configured platform. The handler builds the domain
/// <see cref="Domain.Platforms.Platform"/>, which re-validates name and settings
/// as defense in depth behind the API boundary, then delegates to the
/// repository, which owns name-uniqueness enforcement and id generation. The
/// repository outcome, including the new id on success, is returned unchanged.
/// </summary>
public sealed class CreatePlatformHandler(IPlatformModifier platforms)
{
    public async Task<CreatePlatformResult> HandleAsync(
        CreatePlatformCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var platform = new Domain.Platforms.Platform(
            command.Name,
            command.Type,
            command.PublishSettings);

        return await platforms.CreateAsync(platform, cancellationToken);
    }
}
