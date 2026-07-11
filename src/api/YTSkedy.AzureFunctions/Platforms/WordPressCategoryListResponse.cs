namespace YTSkedy.AzureFunctions.Platforms;

public sealed record WordPressCategoryListResponse(
    IReadOnlyList<WordPressCategoryResponse> Items,
    int Page,
    int PageSize,
    int Total,
    int TotalPages);
