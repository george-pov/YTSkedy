namespace YTSkedy.AzureFunctions.Platforms;

/// <summary>
/// Request body for updating an existing platform. Only the name and publish
/// settings can change; the type is immutable because it drives the settings
/// schema and the provider adapter, so it is not accepted here.
/// </summary>
public sealed record UpdatePlatformRequest(
    string? Name,
    PublishSettingsPayload? PublishSettings);
