namespace YTSkedy.AzureFunctions.Templates;

public sealed record TemplateResponse(
    string Id,
    string Name,
    string Type,
    string Content);
