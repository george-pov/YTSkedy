using Microsoft.Extensions.Logging;
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

internal sealed class CapturingLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, string Message)> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter) =>
        Entries.Add((logLevel, formatter(state, exception)));
}

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
