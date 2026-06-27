using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms;

public interface IPlatformModifier
{
    /// <summary>
    /// Creates a platform after enforcing that its name is globally unique and
    /// its non-empty reference key is globally unique using case-insensitive
    /// key comparison. On success the repository generates the platform id and
    /// returns it in <see cref="CreatePlatformResult"/> with
    /// <see cref="CreatePlatformStatus.Created"/>; a duplicate name yields
    /// <see cref="CreatePlatformStatus.NameAlreadyExists"/>, and a duplicate
    /// non-empty reference key yields
    /// <see cref="CreatePlatformStatus.ReferenceKeyAlreadyExists"/>. The
    /// uniqueness check is check-then-write, so a rare concurrent create race is
    /// accepted for this slice. Storage identity and id generation stay inside
    /// infrastructure.
    /// </summary>
    Task<CreatePlatformResult> CreateAsync(
        Platform platform,
        CancellationToken cancellationToken);

    /// <summary>
    /// Replaces the name, optional reference key, and publish settings of an
    /// existing platform located by <paramref name="platformId"/>. The type is
    /// immutable, so it is not accepted here. Returns
    /// <see cref="UpdatePlatformResult.NotFound"/> when no row has the id,
    /// <see cref="UpdatePlatformResult.NameAlreadyExists"/> when another
    /// platform already uses the new name, and
    /// <see cref="UpdatePlatformResult.ReferenceKeyAlreadyExists"/> when
    /// another platform already uses the same non-empty reference key using
    /// case-insensitive comparison.
    /// </summary>
    Task<UpdatePlatformResult> UpdateAsync(
        string platformId,
        string name,
        string? referenceKey,
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
