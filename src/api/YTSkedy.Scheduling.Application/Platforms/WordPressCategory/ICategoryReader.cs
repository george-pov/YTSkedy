using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms.WordPressCategory;

public interface ICategoryReader
{
    Task<CategoryPage> ListAsync(
        WordPressSettings settings,
        CategoryQuery query,
        CancellationToken cancellationToken);
}
