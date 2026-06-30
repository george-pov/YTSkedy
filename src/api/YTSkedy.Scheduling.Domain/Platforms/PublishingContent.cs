namespace YTSkedy.Scheduling.Domain.Platforms;

/// <summary>
/// Platform-owned title and description template selection. Null template ids
/// mean the publishing flow calculates that field from the calendar event.
/// </summary>
public sealed record PublishingContent
{
    public PublishingContent(
        string? titleTemplateId,
        string? descriptionTemplateId)
    {
        TitleTemplateId = NormalizeTemplateId(titleTemplateId);
        DescriptionTemplateId = NormalizeTemplateId(descriptionTemplateId);
    }

    public static PublishingContent None { get; } = new(null, null);

    public string? TitleTemplateId { get; }

    public string? DescriptionTemplateId { get; }

    private static string? NormalizeTemplateId(string? templateId)
    {
        var trimmed = templateId?.Trim();

        return string.IsNullOrEmpty(trimmed)
            ? null
            : trimmed;
    }
}
