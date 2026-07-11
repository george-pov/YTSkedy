using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms.WordPressCategory;

public sealed class CategoryListHandler(
    IPlatformReader platforms,
    ICategoryReader categories)
{
    public async Task<CategoryListResult> HandleAsync(
        CategoryListQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var platform = await platforms.GetAsync(
            query.PlatformId,
            cancellationToken);
        if (platform is null)
        {
            return CategoryListResult.ForStatus(
                CategoryListStatus.PlatformNotFound);
        }

        if (platform.Type != PlatformType.WordPress ||
            platform.PublishSettings is not WordPressSettings settings)
        {
            return CategoryListResult.ForStatus(
                CategoryListStatus.InvalidPlatformType);
        }

        try
        {
            var page = await categories.ListAsync(
                settings,
                new CategoryQuery(
                    query.Search,
                    query.IncludeIds,
                    query.Page,
                    query.PageSize),
                cancellationToken);
            return CategoryListResult.Listed(page);
        }
        catch (CategoryReadException)
        {
            return CategoryListResult.ForStatus(
                CategoryListStatus.ProviderFailed);
        }
    }
}
