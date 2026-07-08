using System.Net.Http.Headers;
using System.Text;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Infrastructure.WordPress;

internal static class WordPressRequestSecurity
{
    internal static AuthenticationHeaderValue CreateAuthorizationHeader(
        WordPressSettings settings)
    {
        var credentials = $"{settings.Username}:{settings.ApplicationPassword}";
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));

        return new AuthenticationHeaderValue("Basic", encoded);
    }

    internal static string GetLogHost(WordPressSettings settings, Uri? endpoint)
    {
        if (endpoint is not null)
        {
            return endpoint.Host;
        }

        return Uri.TryCreate(settings.SiteUrl.Trim(), UriKind.Absolute, out var siteUri)
            ? siteUri.Host
            : "(invalid)";
    }
}
