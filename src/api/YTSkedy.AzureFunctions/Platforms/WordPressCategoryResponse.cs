namespace YTSkedy.AzureFunctions.Platforms;

public sealed record WordPressCategoryResponse(
    long Id,
    string Name,
    string Slug);
