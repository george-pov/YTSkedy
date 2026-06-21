namespace YTSkedy.AzureFunctions.Templates;

/// <summary>
/// Request body for updating an existing template. Only the name and content can
/// change; the type is immutable because it drives the storage partition, so it
/// travels in the route rather than the body.
/// </summary>
public sealed record UpdateTemplateRequest(
    string Name,
    string Content);
