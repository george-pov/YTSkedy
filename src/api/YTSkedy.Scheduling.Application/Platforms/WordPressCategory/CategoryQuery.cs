namespace YTSkedy.Scheduling.Application.Platforms.WordPressCategory;

public sealed record CategoryQuery(
    string? Search,
    IReadOnlyList<long> IncludeIds,
    int Page,
    int PageSize);
