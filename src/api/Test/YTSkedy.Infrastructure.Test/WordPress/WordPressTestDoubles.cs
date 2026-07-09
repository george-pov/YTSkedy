using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace YTSkedy.Infrastructure.Test.WordPress;

internal sealed class FakeHttpMessageHandler(
    Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
{
    public int CallCount { get; private set; }

    public List<RequestSnapshot> Requests { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        CallCount++;
        Requests.Add(new RequestSnapshot(
            request.Method,
            request.RequestUri!,
            request.Headers.Authorization));

        return Task.FromResult(handler(request));
    }
}

internal sealed record RequestSnapshot(
    HttpMethod Method,
    Uri RequestUri,
    AuthenticationHeaderValue? Authorization);

internal static class WordPressTestResponses
{
    internal static HttpResponseMessage LinkResponse(string linkHeader)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.TryAddWithoutValidation("Link", linkHeader);

        return response;
    }

    internal static HttpResponseMessage JsonIndexResponse() =>
        JsonResponse("""{"namespaces":["wp/v2"]}""");

    internal static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    internal static HttpResponseMessage HtmlResponse() =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent("<html></html>", Encoding.UTF8, "text/html")
        };
}

internal static class WordPressDiscoveryHandlers
{
    internal static FakeHttpMessageHandler PrettyRoot(
        Func<HttpRequestMessage, HttpResponseMessage> requestHandler,
        string rootUrl = "https://example.com/wp-json/") =>
        new(request =>
        {
            if (request.Method == HttpMethod.Head)
            {
                return WordPressTestResponses.LinkResponse(
                    $"<{rootUrl}>; rel=\"https://api.w.org/\"");
            }

            return request.Method == HttpMethod.Get
                ? WordPressTestResponses.JsonIndexResponse()
                : requestHandler(request);
        });

    internal static FakeHttpMessageHandler RouteRoot(
        Func<HttpRequestMessage, HttpResponseMessage> requestHandler) =>
        new(request =>
        {
            if (request.Method == HttpMethod.Head)
            {
                return WordPressTestResponses.LinkResponse(
                    "<https://example.com/index.php?rest_route=/>; rel=\"https://api.w.org/\"");
            }

            return request.Method == HttpMethod.Get
                ? WordPressTestResponses.JsonIndexResponse()
                : requestHandler(request);
        });

    internal static FakeHttpMessageHandler UnsupportedDiscovery() =>
        new(request =>
            request.Method == HttpMethod.Head
                ? new HttpResponseMessage(HttpStatusCode.OK)
                : WordPressTestResponses.JsonResponse("""{"namespaces":["oembed/1.0"]}"""));

    internal static void AssertDiscoveryRequestsAreAnonymous(FakeHttpMessageHandler handler)
    {
        var discoveryRequests = handler.Requests.Where(request =>
            request.Method == HttpMethod.Head || request.Method == HttpMethod.Get);

        Assert.All(discoveryRequests, request => Assert.Null(request.Authorization));
    }
}
