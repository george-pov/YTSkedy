using YTSkedy.Scheduling.Application.Templates;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Scheduling.Application.Platforms;

internal static class TemplateLinkValidator
{
    internal static async Task<bool> TemplatesExistAsync(
        ITemplateReader templates,
        PlatformType platformType,
        PublishingContent publishingContent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(publishingContent);

        var templateType = ToTemplateType(platformType);
        foreach (var templateId in TemplateIds(publishingContent).Distinct(StringComparer.Ordinal))
        {
            if (await templates.GetAsync(templateType, templateId, cancellationToken) is null)
            {
                return false;
            }
        }

        return true;
    }

    internal static bool ReferencesTemplate(
        PublishingContent publishingContent,
        string templateId) =>
        string.Equals(
            publishingContent.TitleTemplateId,
            templateId,
            StringComparison.Ordinal) ||
        string.Equals(
            publishingContent.DescriptionTemplateId,
            templateId,
            StringComparison.Ordinal);

    internal static PlatformType ToPlatformType(TemplateType templateType) =>
        templateType switch
        {
            TemplateType.YouTube => PlatformType.YouTube,
            TemplateType.WordPress => PlatformType.WordPress,
            _ => throw new ArgumentOutOfRangeException(nameof(templateType), templateType, null)
        };

    internal static TemplateType ToTemplateType(PlatformType platformType) =>
        platformType switch
        {
            PlatformType.YouTube => TemplateType.YouTube,
            PlatformType.WordPress => TemplateType.WordPress,
            _ => throw new ArgumentOutOfRangeException(nameof(platformType), platformType, null)
        };

    private static IEnumerable<string> TemplateIds(PublishingContent publishingContent)
    {
        yield return publishingContent.TitleTemplateId;
        yield return publishingContent.DescriptionTemplateId;
    }
}
