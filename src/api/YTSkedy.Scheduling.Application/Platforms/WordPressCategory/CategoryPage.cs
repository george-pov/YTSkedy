namespace YTSkedy.Scheduling.Application.Platforms.WordPressCategory;

public sealed record CategoryPage(
    IReadOnlyList<CategoryView> Items,
    int Page,
    int PageSize,
    int Total,
    int TotalPages);
