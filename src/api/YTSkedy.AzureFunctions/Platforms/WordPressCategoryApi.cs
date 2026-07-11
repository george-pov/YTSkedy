using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Identity.Web.Resource;
using System.Globalization;
using YTSkedy.AzureFunctions.Http;
using YTSkedy.Scheduling.Application.Platforms.WordPressCategory;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.AzureFunctions.Platforms;

public sealed class WordPressCategoryApi(CategoryListHandler handler)
{
    internal const int DefaultPage = 1;
    internal const int DefaultPageSize = 25;
    internal const int MaxPageSize = 100;
    internal const int MaxSearchLength = 100;

    [Function("ListWordPressCategories")]
    [RequiredScope("CalendarEvents.Write")]
    public async Task<IActionResult> ListAsync(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "get",
            Route = "platforms/{platformId}/wordpress/categories")]
        HttpRequest request,
        string platformId,
        CancellationToken cancellationToken)
    {
        if (!TryBuildQuery(request, platformId, out var query, out var error))
        {
            return error;
        }

        var result = await handler.HandleAsync(query, cancellationToken);
        return ToResult(result, platformId);
    }

    internal static bool TryBuildQuery(
        HttpRequest request,
        string platformId,
        out CategoryListQuery query,
        out IActionResult error)
    {
        ArgumentNullException.ThrowIfNull(request);

        query = default!;
        error = new EmptyResult();

        if (!HttpQuery.TryGetSingleValue(
                request,
                "search",
                out var searchValue,
                out error) ||
            !HttpQuery.TryGetSingleValue(
                request,
                "includeIds",
                out var includeValue,
                out error))
        {
            return false;
        }

        var search = searchValue?.Trim();
        if (search is not null && search.Length > MaxSearchLength)
        {
            error = new BadRequestObjectResult(
                $"Query parameter 'search' must be at most {MaxSearchLength} characters.");
            return false;
        }

        if (search is not null && includeValue is not null)
        {
            error = new BadRequestObjectResult(
                "Query parameters 'search' and 'includeIds' cannot be used together.");
            return false;
        }

        if (!TryParseIncludeIds(includeValue, out var includeIds))
        {
            error = new BadRequestObjectResult(
                "Query parameter 'includeIds' must contain distinct positive integers separated by commas.");
            return false;
        }

        if (!TryParsePageValue(
                request,
                "page",
                DefaultPage,
                int.MaxValue,
                out var page,
                out error) ||
            !TryParsePageValue(
                request,
                "pageSize",
                DefaultPageSize,
                MaxPageSize,
                out var pageSize,
                out error))
        {
            return false;
        }

        query = new CategoryListQuery(
            platformId,
            search,
            includeIds,
            page,
            pageSize);
        return true;
    }

    internal static bool TryParseIncludeIds(
        string? value,
        out IReadOnlyList<long> includeIds)
    {
        includeIds = [];
        if (value is null)
        {
            return true;
        }

        var values = value.Split(',', StringSplitOptions.None);
        var parsed = new long[values.Length];
        for (var index = 0; index < values.Length; index++)
        {
            if (!long.TryParse(
                    values[index],
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var id))
            {
                return false;
            }

            parsed[index] = id;
        }

        if (!WordPressSettings.AreValidCategoryIds(parsed))
        {
            return false;
        }

        includeIds = parsed;
        return true;
    }

    internal static IActionResult ToResult(
        CategoryListResult result,
        string platformId) =>
        result.Status switch
        {
            CategoryListStatus.Listed when result.Page is not null =>
                new OkObjectResult(ToResponse(result.Page)),
            CategoryListStatus.PlatformNotFound => new NotFoundObjectResult(
                $"Platform '{platformId}' was not found."),
            CategoryListStatus.InvalidPlatformType => new ConflictObjectResult(
                $"Platform '{platformId}' is not a WordPress platform."),
            CategoryListStatus.ProviderFailed => new ObjectResult(
                "WordPress categories could not be loaded.")
            {
                StatusCode = StatusCodes.Status502BadGateway
            },
            _ => new StatusCodeResult(StatusCodes.Status500InternalServerError)
        };

    private static WordPressCategoryListResponse ToResponse(CategoryPage page) =>
        new(
            page.Items.Select(static item =>
                new WordPressCategoryResponse(item.Id, item.Name, item.Slug))
                .ToArray(),
            page.Page,
            page.PageSize,
            page.Total,
            page.TotalPages);

    private static bool TryParsePageValue(
        HttpRequest request,
        string name,
        int defaultValue,
        int maxValue,
        out int value,
        out IActionResult error)
    {
        value = defaultValue;
        if (!HttpQuery.TryParseOptionalInt(
                request,
                name,
                out var parsed,
                out var hasValue,
                out error,
                $"Query parameter '{name}' must be an integer."))
        {
            return false;
        }

        if (!hasValue)
        {
            return true;
        }

        if (!HttpQuery.TryValidateRange(name, parsed, 1, maxValue, out error))
        {
            return false;
        }

        value = parsed;
        return true;
    }
}
