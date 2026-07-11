namespace YTSkedy.Scheduling.Application.Platforms.WordPressCategory;

public sealed record CategoryListResult(
    CategoryListStatus Status,
    CategoryPage? Page)
{
    public static CategoryListResult Listed(CategoryPage page)
    {
        ArgumentNullException.ThrowIfNull(page);

        return new(CategoryListStatus.Listed, page);
    }

    public static CategoryListResult ForStatus(CategoryListStatus status) =>
        new(status, null);
}
