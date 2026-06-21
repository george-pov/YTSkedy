namespace YTSkedy.AzureFunctions.Templates;

public sealed record CreateTemplateRequest(
    string Name,
    string Type,
    string Content);
