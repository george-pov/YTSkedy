namespace YTSkedy.AzureFunctions.Templates;

internal sealed record TemplateResponse(
    string Id,
    string Name,
    string Type,
    string Content);
