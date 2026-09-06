using System.Net.Http.Headers;

namespace YTSkedy.Infrastructure.WordPress;

internal static class WordPressRequestHeaders
{
    private static readonly ProductInfoHeaderValue UserAgent = new("YTSkedy", "1.0");

    internal static void AddClientIdentification(
        HttpRequestMessage request,
        string? requestId = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Headers.UserAgent.Add(UserAgent);

        if (!string.IsNullOrWhiteSpace(requestId))
        {
            request.Headers.TryAddWithoutValidation("X-YTSkedy-Request-Id", requestId.Trim());
        }
    }
}
