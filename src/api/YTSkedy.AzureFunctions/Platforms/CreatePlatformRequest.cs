namespace YTSkedy.AzureFunctions.Platforms;

/// <summary>
/// Request body for creating a platform. The type is immutable after create, so
/// it travels in the body here but not in the update request.
/// </summary>
internal sealed record CreatePlatformRequest(
    string? Name,
    string? Type,
    string? ReferenceKey,
    PublishSettingsRequest? PublishSettings,
    PublishingContentRequest? PublishingContent = null);
