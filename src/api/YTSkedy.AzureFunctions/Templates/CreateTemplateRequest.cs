namespace YTSkedy.AzureFunctions.Templates;

internal sealed record CreateTemplateRequest(
    string Name,
    string Type,
    string Content);
