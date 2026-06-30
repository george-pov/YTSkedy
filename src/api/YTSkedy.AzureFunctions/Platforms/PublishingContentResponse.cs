namespace YTSkedy.AzureFunctions.Platforms;

/// <summary>
/// Platform-owned title and description template selection returned by platform
/// reads. Null template ids mean the backend calculates that field from the
/// calendar event.
/// </summary>
internal sealed record PublishingContentResponse(
    string? TitleTemplateId,
    string? DescriptionTemplateId);
