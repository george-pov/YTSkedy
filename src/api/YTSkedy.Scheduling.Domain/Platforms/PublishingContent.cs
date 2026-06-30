namespace YTSkedy.Scheduling.Domain.Platforms;

/// <summary>
/// Platform-owned title and description template selection.
/// </summary>
public sealed record PublishingContent
{
    public PublishingContent(
        string titleTemplateId,
        string descriptionTemplateId)
    {
        TitleTemplateId = NormalizeTemplateId(titleTemplateId, nameof(titleTemplateId));
        DescriptionTemplateId = NormalizeTemplateId(
            descriptionTemplateId,
            nameof(descriptionTemplateId));
    }

    public string TitleTemplateId { get; }

    public string DescriptionTemplateId { get; }

    private static string NormalizeTemplateId(string? templateId, string parameterName)
    {
        var trimmed = templateId?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            throw new ArgumentException(
                "Publishing content template id is required.",
                parameterName);
        }

        return trimmed;
    }
}
