using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms;

public interface IPlatformModifier
{
    /// <summary>
    /// Creates a platform after enforcing that its name is globally unique. On
    /// success the repository generates the platform id and returns it in
    /// <see cref="CreatePlatformResult"/> with
    /// <see cref="CreatePlatformStatus.Created"/>; a duplicate name yields
    /// <see cref="CreatePlatformStatus.NameAlreadyExists"/> with no id. The
    /// uniqueness check is check-then-write, so a rare concurrent create race is
    /// accepted for this slice. Storage identity and id generation stay inside
    /// infrastructure.
    /// </summary>
    Task<CreatePlatformResult> CreateAsync(
        Platform platform,
        CancellationToken cancellationToken);

    /// <summary>
    /// Replaces the name and publish settings of an existing platform located by
    /// <paramref name="platformId"/>. The type is immutable, so it is not
    /// accepted here. Returns <see cref="UpdatePlatformResult.NotFound"/> when no
    /// row has the id and <see cref="UpdatePlatformResult.NameAlreadyExists"/>
    /// when another platform already uses the new name.
    /// </summary>
    Task<UpdatePlatformResult> UpdateAsync(
        string platformId,
        string name,
        PublishSettings publishSettings,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes the configured platform located by <paramref name="platformId"/>.
    /// Returns <see cref="DeletePlatformResult.NotFound"/> when no row has the id.
    /// Preserving published publication rows as orphan history is layered on in a
    /// later phase once platform publication state exists.
    /// </summary>
    Task<DeletePlatformResult> DeleteAsync(
        string platformId,
        CancellationToken cancellationToken);
}
