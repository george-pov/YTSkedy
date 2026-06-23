namespace YTSkedy.AzureFunctions.Templates;

/// <summary>
/// Envelope returned by <c>GET /api/template-tokens</c>. Lists the code-defined
/// placeholder tokens a client can offer for template content.
/// </summary>
internal sealed record TemplateTokenListResponse(
    IReadOnlyList<TemplateTokenResponse> Tokens);
