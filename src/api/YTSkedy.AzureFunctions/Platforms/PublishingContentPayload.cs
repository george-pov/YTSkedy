namespace YTSkedy.AzureFunctions.Platforms;

/// <summary>
/// Platform-owned title and description template selection carried by platform
/// create and update requests. Null template ids mean the backend calculates
/// that field from the calendar event.
/// </summary>
internal sealed record PublishingContentPayload(
    string? TitleTemplateId,
    string? DescriptionTemplateId);
