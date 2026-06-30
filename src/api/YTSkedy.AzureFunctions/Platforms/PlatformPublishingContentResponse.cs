namespace YTSkedy.AzureFunctions.Platforms;

/// <summary>
/// Platform-owned title and description template selection returned by platform
/// reads.
/// </summary>
internal sealed record PlatformPublishingContentResponse(
    string TitleTemplateId,
    string DescriptionTemplateId);
