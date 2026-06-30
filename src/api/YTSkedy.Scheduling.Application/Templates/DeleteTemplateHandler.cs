using YTSkedy.Scheduling.Application.Platforms;

namespace YTSkedy.Scheduling.Application.Templates;

/// <summary>
/// Deletes a template located by type and id. Platforms of the same provider
/// family are checked first so template rows cannot be removed while active
/// platforms still reference them.
/// </summary>
public sealed class DeleteTemplateHandler(
    ITemplateModifier templates,
    IPlatformReader platforms)
{
    public async Task<DeleteTemplateResult> HandleAsync(
        DeleteTemplateCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var linkedPlatforms = await platforms.ListAsync(
            TemplateLinkValidator.ToPlatformType(command.Type),
            cancellationToken);
        if (linkedPlatforms.Any(platform =>
                TemplateLinkValidator.ReferencesTemplate(
                    platform.PublishingContent,
                    command.Id)))
        {
            return DeleteTemplateResult.ReferencedByPlatform;
        }

        return await templates.DeleteAsync(
            command.Type,
            command.Id,
            cancellationToken);
    }
}
