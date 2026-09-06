using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using YTSkedy.Scheduling.Application.Platforms.WordPressCategory;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Infrastructure.WordPress;

public sealed class WordPressCategoryReader(
    HttpClient httpClient,
    WordPressEndpointResolver endpointResolver,
    ILogger<WordPressCategoryReader> logger) : ICategoryReader
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public async Task<CategoryPage> ListAsync(
        WordPressSettings settings,
        CategoryQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(query);

        Uri? endpoint = null;
        try
        {
            var root = await endpointResolver.ResolveAsync(
                settings,
                cancellationToken);
            endpoint = root.BuildRoute(
                "/wp/v2/categories",
                BuildQuery(query));

            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            WordPressRequestHeaders.AddClientIdentification(request);
            request.Headers.Authorization =
                WordPressRequestSecurity.CreateAuthorizationHeader(settings);

            using var response = await httpClient.SendAsync(
                request,
                cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                LogProviderFailure(
                    WordPressRequestSecurity.GetLogHost(settings, endpoint),
                    response.StatusCode);
                throw new CategoryReadException(
                    "WordPress category lookup failed.");
            }

            var items = await response.Content.ReadFromJsonAsync<
                WordPressCategoryResponse[]>(
                JsonOptions,
                cancellationToken);
            var total = ReadPagingHeader(response, "X-WP-Total");
            var totalPages = ReadPagingHeader(response, "X-WP-TotalPages");

            if (items is null ||
                total is null ||
                totalPages is null ||
                items.Any(static item =>
                    item.Id <= 0 ||
                    item.Name is null ||
                    item.Slug is null))
            {
                logger.LogError(
                    "WordPress category lookup returned an invalid response at host {WordPressHost}.",
                    WordPressRequestSecurity.GetLogHost(settings, endpoint));
                throw new CategoryReadException(
                    "WordPress category lookup returned an invalid response.");
            }

            return new CategoryPage(
                items.Select(static item =>
                    new CategoryView(item.Id, item.Name!, item.Slug!))
                    .ToArray(),
                query.Page,
                query.PageSize,
                total.Value,
                totalPages.Value);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (CategoryReadException)
        {
            throw;
        }
        catch (JsonException exception)
        {
            logger.LogError(
                "WordPress category lookup returned malformed JSON at host {WordPressHost}.",
                WordPressRequestSecurity.GetLogHost(settings, endpoint));
            throw new CategoryReadException(
                "WordPress category lookup returned malformed JSON.",
                exception);
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(
                "WordPress category lookup request failed at host {WordPressHost}.",
                WordPressRequestSecurity.GetLogHost(settings, endpoint));
            throw new CategoryReadException(
                "WordPress category lookup request failed.",
                exception);
        }
    }

    private static IReadOnlyDictionary<string, string> BuildQuery(
        CategoryQuery query)
    {
        var values = new Dictionary<string, string>
        {
            ["context"] = "view",
            ["hide_empty"] = "false",
            ["orderby"] = "name",
            ["order"] = "asc",
            ["_fields"] = "id,name,slug",
            ["page"] = query.Page.ToString(CultureInfo.InvariantCulture),
            ["per_page"] = query.PageSize.ToString(CultureInfo.InvariantCulture)
        };

        if (query.Search is not null)
        {
            values["search"] = query.Search;
        }
        else if (query.IncludeIds.Count > 0)
        {
            values["include"] = string.Join(
                ",",
                query.IncludeIds.Select(static id =>
                    id.ToString(CultureInfo.InvariantCulture)));
        }

        return values;
    }

    private static int? ReadPagingHeader(
        HttpResponseMessage response,
        string name)
    {
        if (!response.Headers.TryGetValues(name, out var values))
        {
            return null;
        }

        var headerValues = values.ToArray();
        if (headerValues.Length != 1)
        {
            return null;
        }

        var value = headerValues[0];
        return int.TryParse(
            value,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out var parsed) && parsed >= 0
                ? parsed
                : null;
    }

    private void LogProviderFailure(string host, HttpStatusCode statusCode)
    {
        logger.LogError(
            "WordPress category lookup returned HTTP {StatusCode} at host {WordPressHost}.",
            (int)statusCode,
            host);
    }

    private sealed record WordPressCategoryResponse(
        long Id,
        string? Name,
        string? Slug);
}
