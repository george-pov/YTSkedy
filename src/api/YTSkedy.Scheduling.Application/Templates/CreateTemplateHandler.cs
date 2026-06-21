using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Scheduling.Application.Templates;

/// <summary>
/// Creates a reusable template. The handler builds the domain
/// <see cref="Template"/>, which re-validates name and content as defense in
/// depth behind the API boundary, then delegates to the repository, which owns
/// name-uniqueness enforcement and id generation. The repository outcome,
/// including the new id on success, is returned unchanged.
/// </summary>
public sealed class CreateTemplateHandler(ITemplateRepository templates)
{
    public async Task<CreateTemplateResult> HandleAsync(
        CreateTemplateCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var template = new Template(
            command.Name,
            command.Type,
            command.Content);

        return await templates.CreateAsync(template, cancellationToken);
    }
}
