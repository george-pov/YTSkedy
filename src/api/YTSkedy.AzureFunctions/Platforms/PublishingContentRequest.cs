namespace YTSkedy.AzureFunctions.Platforms;

/// <summary>
/// Nested title and description template request used by platform create and
/// update requests. Both template ids are required.
/// </summary>
internal sealed record PublishingContentRequest(
    string? TitleTemplateId,
    string? DescriptionTemplateId);
