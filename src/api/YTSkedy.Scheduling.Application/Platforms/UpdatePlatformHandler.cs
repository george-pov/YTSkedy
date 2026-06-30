using YTSkedy.Scheduling.Application.Templates;

namespace YTSkedy.Scheduling.Application.Platforms;

/// <summary>
/// Updates the name, publishing content, and publish settings of an existing
/// platform located by id. The handler reads the platform first because the
/// type is immutable and publishing-content template ids must match that type.
/// The repository owns the name-uniqueness check on rename and final not-found
/// outcome, so the storage result is returned unchanged after validation.
/// </summary>
public sealed class UpdatePlatformHandler(
    IPlatformReader platformReader,
    IPlatformModifier platformModifier,
    ITemplateReader templates)
{
    public async Task<UpdatePlatformResult> HandleAsync(
        UpdatePlatformCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var platform = await platformReader.GetAsync(command.PlatformId, cancellationToken);
        if (platform is null)
        {
            return UpdatePlatformResult.NotFound;
        }

        if (!await TemplateLinkValidator.TemplatesExistAsync(
                templates,
                platform.Type,
                command.PublishingContent,
                cancellationToken))
        {
            return UpdatePlatformResult.LinkedTemplateNotFound;
        }

        return await platformModifier.UpdateAsync(
            command.PlatformId,
            command.Name,
            command.ReferenceKey,
            command.PublishSettings,
            command.PublishingContent,
            cancellationToken);
    }
}
