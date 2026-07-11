namespace YTSkedy.Scheduling.Application.Platforms.WordPressCategory;

public sealed record CategoryListQuery(
    string PlatformId,
    string? Search,
    IReadOnlyList<long> IncludeIds,
    int Page,
    int PageSize);
