using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Scheduling.Application.Templates;

public interface ITemplateRepository
{
    /// <summary>
    /// Creates a template after enforcing that its name is unique within the
    /// type. On success the repository generates the GUID id and returns it in
    /// <see cref="CreateTemplateResult"/> with
    /// <see cref="CreateTemplateStatus.Created"/>; a duplicate name yields
    /// <see cref="CreateTemplateStatus.NameAlreadyExists"/> with no id. The
    /// uniqueness check is check-then-write, so a rare concurrent create race is
    /// accepted for this slice. Storage identity and id generation stay inside
    /// infrastructure.
    /// </summary>
    Task<CreateTemplateResult> CreateAsync(
        Template template,
        CancellationToken cancellationToken);

    /// <summary>
    /// Replaces the name and content of an existing template located by
    /// <paramref name="type"/> and <paramref name="id"/>. The type is immutable
    /// because it drives the partition. Returns
    /// <see cref="UpdateTemplateResult.NotFound"/> when no row has the id and
    /// <see cref="UpdateTemplateResult.NameAlreadyExists"/> when another row in
    /// the type already uses the new name.
    /// </summary>
    Task<UpdateTemplateResult> UpdateAsync(
        TemplateType type,
        string id,
        string name,
        string content,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes the template located by <paramref name="type"/> and
    /// <paramref name="id"/>. Returns <see cref="DeleteTemplateResult.NotFound"/>
    /// when no row has the id. The delete is id-based; storage identity and ETags
    /// stay inside infrastructure.
    /// </summary>
    Task<DeleteTemplateResult> DeleteAsync(
        TemplateType type,
        string id,
        CancellationToken cancellationToken);
}
