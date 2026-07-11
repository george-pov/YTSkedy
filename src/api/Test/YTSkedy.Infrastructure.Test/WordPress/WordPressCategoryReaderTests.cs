using Microsoft.Extensions.Logging;
using System.Net;
using YTSkedy.Infrastructure.Test.TestSupport;
using YTSkedy.Infrastructure.WordPress;
using YTSkedy.Scheduling.Application.Platforms.WordPressCategory;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.TestSupport;
using static YTSkedy.Infrastructure.Test.WordPress.WordPressTestResponses;

namespace YTSkedy.Infrastructure.Test.WordPress;

public class WordPressCategoryReaderTests
{
    private const string ApplicationPassword = "application-password-secret";

    [Fact]
    public async Task ListAsync_PrettyRoot_MapsRequestResponseAndPaging()
    {
        var handler = CategoryHandler(
            PagedJsonResponse(
                """[{"id":12,"name":"Events","slug":"events"}]""",
                total: "26",
                totalPages: "2"));
        var reader = CreateReader(handler);

        var result = await reader.ListAsync(
            Settings(),
            new CategoryQuery("live events", [], 2, 25),
            CancellationToken.None);

        var request = Assert.Single(handler.Requests, request =>
            request.RequestUri.AbsolutePath.EndsWith(
                "/wp/v2/categories",
                StringComparison.Ordinal));
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("Basic", request.Authorization!.Scheme);
        var query = ParseQuery(request.RequestUri);
        Assert.Equal("view", query["context"]);
        Assert.Equal("false", query["hide_empty"]);
        Assert.Equal("name", query["orderby"]);
        Assert.Equal("asc", query["order"]);
        Assert.Equal("id,name,slug", query["_fields"]);
        Assert.Equal("2", query["page"]);
        Assert.Equal("25", query["per_page"]);
        Assert.Equal("live events", query["search"]);
        Assert.DoesNotContain("include", query.Keys);
        var item = Assert.Single(result.Items);
        Assert.Equal(12, item.Id);
        Assert.Equal("Events", item.Name);
        Assert.Equal("events", item.Slug);
        Assert.Equal(2, result.Page);
        Assert.Equal(25, result.PageSize);
        Assert.Equal(26, result.Total);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task ListAsync_RouteRoot_UsesRestRouteAndIncludeIds()
    {
        var handler = CategoryHandler(
            PagedJsonResponse(
                """[{"id":34,"name":"News","slug":"news"},{"id":12,"name":"Events","slug":"events"}]""",
                total: "2"),
            routeRoot: true);
        var reader = CreateReader(handler);

        var result = await reader.ListAsync(
            Settings(),
            new CategoryQuery(null, [34, 12], 1, 100),
            CancellationToken.None);

        var request = handler.Requests.Last();
        var query = ParseQuery(request.RequestUri);
        Assert.Equal("/wp/v2/categories", query["rest_route"]);
        Assert.Equal("34,12", query["include"]);
        Assert.Equal("100", query["per_page"]);
        Assert.DoesNotContain("search", query.Keys);
        Assert.Equal([34, 12], result.Items.Select(item => item.Id));
    }

    [Fact]
    public async Task ListAsync_NoSearchOrIds_EmitsOnlyBaseQuery()
    {
        var handler = CategoryHandler(PagedJsonResponse("[]", "0", "0"));
        var reader = CreateReader(handler);

        var result = await reader.ListAsync(
            Settings(),
            new CategoryQuery(null, [], 1, 25),
            CancellationToken.None);

        var query = ParseQuery(handler.Requests.Last().RequestUri);
        Assert.DoesNotContain("search", query.Keys);
        Assert.DoesNotContain("include", query.Keys);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.Total);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public async Task ListAsync_NonSuccessStatus_ThrowsAndLogsSafeContext()
    {
        var logger = new CapturingLogger<WordPressCategoryReader>();
        var handler = CategoryHandler(
            PagedJsonResponse(
                """{"code":"rest_forbidden","message":"secret body"}""",
                statusCode: HttpStatusCode.Forbidden));
        var reader = CreateReader(handler, logger);

        var exception = await Assert.ThrowsAsync<CategoryReadException>(
            () => ListAsync(reader));

        Assert.Equal("WordPress category lookup failed.", exception.Message);
        Assert.Contains("403", logger.Text);
        Assert.Contains("example.com", logger.Text);
        Assert.DoesNotContain(ApplicationPassword, logger.Text);
        Assert.DoesNotContain("Basic", logger.Text);
        Assert.DoesNotContain("secret body", logger.Text);
        Assert.DoesNotContain("search", logger.Text);
    }

    [Fact]
    public async Task ListAsync_MalformedJson_ThrowsCategoryReadException()
    {
        var reader = CreateReader(CategoryHandler(PagedJsonResponse("{bad")));

        var exception = await Assert.ThrowsAsync<CategoryReadException>(
            () => ListAsync(reader));

        Assert.Contains("malformed JSON", exception.Message);
        Assert.IsType<System.Text.Json.JsonException>(exception.InnerException);
    }

    [Theory]
    [InlineData(null, "1")]
    [InlineData("1", null)]
    [InlineData("-1", "1")]
    [InlineData("one", "1")]
    [InlineData("1", "many")]
    public async Task ListAsync_InvalidPagingHeaders_ThrowsCategoryReadException(
        string? total,
        string? totalPages)
    {
        var response = JsonResponse("[]");
        if (total is not null)
        {
            response.Headers.TryAddWithoutValidation("X-WP-Total", total);
        }
        if (totalPages is not null)
        {
            response.Headers.TryAddWithoutValidation("X-WP-TotalPages", totalPages);
        }
        var reader = CreateReader(CategoryHandler(response));

        var exception = await Assert.ThrowsAsync<CategoryReadException>(
            () => ListAsync(reader));

        Assert.Contains("invalid response", exception.Message);
    }

    [Theory]
    [InlineData("""[{"id":0,"name":"Events","slug":"events"}]""")]
    [InlineData("""[{"id":12,"name":null,"slug":"events"}]""")]
    [InlineData("""[{"id":12,"name":"Events","slug":null}]""")]
    public async Task ListAsync_InvalidCategoryItem_ThrowsCategoryReadException(
        string json)
    {
        var reader = CreateReader(CategoryHandler(PagedJsonResponse(json)));

        var exception = await Assert.ThrowsAsync<CategoryReadException>(
            () => ListAsync(reader));

        Assert.Contains("invalid response", exception.Message);
    }

    [Fact]
    public async Task ListAsync_HttpFailure_ThrowsAndDoesNotLogCredentials()
    {
        var logger = new CapturingLogger<WordPressCategoryReader>();
        var handler = CategoryHandler(
            response: null,
            categoryException: new HttpRequestException(
                $"network down search=private-query password={ApplicationPassword}"));
        var reader = CreateReader(handler, logger);

        var exception = await Assert.ThrowsAsync<CategoryReadException>(
            () => ListAsync(reader));

        Assert.IsType<HttpRequestException>(exception.InnerException);
        Assert.DoesNotContain(ApplicationPassword, logger.Text);
        Assert.DoesNotContain("Basic", logger.Text);
        Assert.DoesNotContain("private-query", logger.Text);
        Assert.Contains("example.com", logger.Text);
    }

    [Fact]
    public async Task ListAsync_ResolverFailure_ThrowsCategoryReadException()
    {
        var handler = new FakeHttpMessageHandler(request =>
            request.Method == HttpMethod.Head
                ? new HttpResponseMessage(HttpStatusCode.OK)
                : JsonResponse("""{"namespaces":["oembed/1.0"]}"""));
        var reader = CreateReader(handler);

        var exception = await Assert.ThrowsAsync<CategoryReadException>(
            () => ListAsync(reader));

        Assert.IsType<HttpRequestException>(exception.InnerException);
    }

    [Fact]
    public async Task ListAsync_Cancellation_Propagates()
    {
        var cancellation = new OperationCanceledException();
        var reader = CreateReader(new FakeHttpMessageHandler(_ => throw cancellation));

        var exception = await Assert.ThrowsAsync<OperationCanceledException>(
            () => ListAsync(reader));

        Assert.Same(cancellation, exception);
    }

    private static Task<CategoryPage> ListAsync(WordPressCategoryReader reader) =>
        reader.ListAsync(
            Settings(),
            new CategoryQuery("events", [], 1, 25),
            CancellationToken.None);

    private static WordPressCategoryReader CreateReader(
        FakeHttpMessageHandler handler,
        ILogger<WordPressCategoryReader>? logger = null)
    {
        var client = new HttpClient(handler);
        return new WordPressCategoryReader(
            client,
            new WordPressEndpointResolver(
                client,
                new CapturingLogger<WordPressEndpointResolver>()),
            logger ?? new CapturingLogger<WordPressCategoryReader>());
    }

    private static FakeHttpMessageHandler CategoryHandler(
        HttpResponseMessage? response,
        bool routeRoot = false,
        Exception? categoryException = null) =>
        new(request =>
        {
            if (request.Method == HttpMethod.Head)
            {
                var root = routeRoot
                    ? "https://example.com/index.php?rest_route=/"
                    : "https://example.com/wp-json/";
                return LinkResponse($"<{root}>; rel=\"https://api.w.org/\"");
            }

            var query = ParseQuery(request.RequestUri!);
            var isRoot = routeRoot
                ? query["rest_route"] == "/"
                : request.RequestUri!.AbsolutePath.EndsWith(
                    "/wp-json/",
                    StringComparison.Ordinal);
            if (isRoot)
            {
                return JsonIndexResponse();
            }

            if (categoryException is not null)
            {
                throw categoryException;
            }

            return response!;
        });

    private static Dictionary<string, string> ParseQuery(Uri uri) =>
        uri.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(value => value.Split('=', 2))
            .ToDictionary(
                value => Uri.UnescapeDataString(value[0]),
                value => Uri.UnescapeDataString(value.Length > 1 ? value[1] : string.Empty),
                StringComparer.Ordinal);

    private static WordPressSettings Settings() =>
        new("https://example.com", "editor", ApplicationPassword, "publish", []);
}
