using YTSkedy.Scheduling.Application.Templates;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms;

/// <summary>
/// Creates a configured platform. Publishing-content template ids are validated
/// against the platform type before the handler builds the domain
/// <see cref="Platform"/>, which re-validates name and settings as defense in
/// depth behind the API boundary. The repository owns name-uniqueness
/// enforcement and id generation. The repository outcome, including the new id
/// on success, is returned unchanged.
/// </summary>
public sealed class CreatePlatformHandler(
    IPlatformModifier platforms,
    ITemplateReader templates)
{
    public async Task<CreatePlatformResult> HandleAsync(
        CreatePlatformCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!await TemplateLinkValidator.TemplatesExistAsync(
                templates,
                command.Type,
                command.PublishingContent,
                cancellationToken))
        {
            return CreatePlatformResult.LinkedTemplateNotFound();
        }

        var platform = new Platform(
            command.Name,
            command.Type,
            command.PublishSettings,
            command.ReferenceKey,
            command.PublishingContent);

        return await platforms.CreateAsync(platform, cancellationToken);
    }
}
