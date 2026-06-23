namespace YTSkedy.AzureFunctions.Templates;

/// <summary>
/// Envelope returned by <c>GET /api/templates</c>. Each template carries its id
/// and type, so a client always has what the update and delete routes need.
/// </summary>
internal sealed record TemplateListResponse(
    IReadOnlyList<TemplateResponse> Templates);
