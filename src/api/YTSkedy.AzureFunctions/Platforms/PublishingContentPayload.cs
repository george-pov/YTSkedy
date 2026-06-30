namespace YTSkedy.AzureFunctions.Platforms;

/// <summary>
/// Platform-owned title and description template selection carried by platform
/// create and update requests. Both template ids are required.
/// </summary>
internal sealed record PublishingContentPayload(
    string? TitleTemplateId,
    string? DescriptionTemplateId);
