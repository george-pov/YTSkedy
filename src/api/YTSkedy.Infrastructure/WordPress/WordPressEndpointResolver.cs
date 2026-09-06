using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Infrastructure.WordPress;

public sealed class WordPressEndpointResolver(
    HttpClient httpClient,
    ILogger<WordPressEndpointResolver> logger,
    TimeProvider? timeProvider = null)
{
    private const string ApiRel = "https://api.w.org/";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ConcurrentDictionary<string, CachedRoot> _cachedRoots =
        new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _discoveryLocks =
        new(StringComparer.Ordinal);
    private readonly TimeProvider _clock = timeProvider ?? TimeProvider.System;

    internal async Task<WordPressRoot> ResolveAsync(
        WordPressSettings settings,
        CancellationToken cancellationToken,
        string? requestId = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        cancellationToken.ThrowIfCancellationRequested();

        var siteUri = CreateSiteUri(settings);
        var cacheKey = siteUri.AbsoluteUri;
        if (TryGetCachedRoot(cacheKey, out var cachedRoot))
        {
            return cachedRoot with
            {
                DiscoveryCacheHit = true,
                DiscoveryRequestCount = 0
            };
        }

        var discoveryLock = _discoveryLocks.GetOrAdd(
            cacheKey,
            static _ => new SemaphoreSlim(1, 1));
        await discoveryLock.WaitAsync(cancellationToken);
        try
        {
            if (TryGetCachedRoot(cacheKey, out cachedRoot))
            {
                return cachedRoot with
                {
                    DiscoveryCacheHit = true,
                    DiscoveryRequestCount = 0
                };
            }

            var root = await DiscoverAsync(siteUri, cancellationToken, requestId);
            _cachedRoots[cacheKey] = new CachedRoot(
                root,
                _clock.GetUtcNow() + CacheDuration);
            return root;
        }
        finally
        {
            discoveryLock.Release();
        }
    }

    private async Task<WordPressRoot> DiscoverAsync(
        Uri siteUri,
        CancellationToken cancellationToken,
        string? requestId)
    {
        var candidates = new List<Uri>();
        var requestCount = 1;

        var linkedRoot = await TryGetRootFromSiteAsync(
            siteUri,
            cancellationToken,
            requestId);
        AddCandidate(candidates, linkedRoot);
        AddCandidate(candidates, BuildPrettyRoot(siteUri));
        AddCandidate(candidates, BuildRouteRoot(siteUri));

        foreach (var candidate in candidates)
        {
            requestCount++;
            var root = await ProbeRootAsync(candidate, cancellationToken, requestId);
            if (root is not null)
            {
                return root with { DiscoveryRequestCount = requestCount };
            }
        }

        logger.LogError(
            "WordPress REST API discovery failed for host {WordPressHost}.",
            siteUri.Host);

        throw new HttpRequestException(
            $"WordPress REST API discovery failed for host '{siteUri.Host}'.");
    }

    private bool TryGetCachedRoot(string cacheKey, out WordPressRoot root)
    {
        if (_cachedRoots.TryGetValue(cacheKey, out var cachedRoot) &&
            cachedRoot.ExpiresAtUtc > _clock.GetUtcNow())
        {
            root = cachedRoot.Root;
            return true;
        }

        root = null!;
        return false;
    }

    private async Task<Uri?> TryGetRootFromSiteAsync(
        Uri siteUri,
        CancellationToken cancellationToken,
        string? requestId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Head, siteUri);
        WordPressRequestHeaders.AddClientIdentification(request, requestId);

        try
        {
            using var response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            return TryGetRootFromLinkHeaders(siteUri, response.Headers);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException exception)
        {
            logger.LogInformation(
                exception,
                "WordPress REST API discovery HEAD request failed for host {WordPressHost}.",
                siteUri.Host);

            return null;
        }
    }

    private static Uri? TryGetRootFromLinkHeaders(
        Uri siteUri,
        HttpResponseHeaders headers)
    {
        if (!headers.TryGetValues("Link", out var linkHeaders))
        {
            return null;
        }

        foreach (var link in linkHeaders.SelectMany(SplitLinkHeaderValue))
        {
            var uriEnd = link.IndexOf('>', StringComparison.Ordinal);
            if (!link.StartsWith('<') || uriEnd <= 1)
            {
                continue;
            }

            var parameters = link[(uriEnd + 1)..];
            if (!HasApiRel(parameters))
            {
                continue;
            }

            var value = link[1..uriEnd];
            if (Uri.TryCreate(siteUri, value, out var rootUri) &&
                IsSafeLinkedRoot(siteUri, rootUri))
            {
                return rootUri;
            }
        }

        return null;
    }

    private static bool IsSafeLinkedRoot(Uri siteUri, Uri rootUri) =>
        string.IsNullOrEmpty(rootUri.UserInfo) &&
        rootUri.Scheme.Equals(siteUri.Scheme, StringComparison.OrdinalIgnoreCase) &&
        rootUri.Host.Equals(siteUri.Host, StringComparison.OrdinalIgnoreCase) &&
        rootUri.Port == siteUri.Port;

    private async Task<WordPressRoot?> ProbeRootAsync(
        Uri rootUri,
        CancellationToken cancellationToken,
        string? requestId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, rootUri);
        WordPressRequestHeaders.AddClientIdentification(request, requestId);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
            using var response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode || !IsJsonResponse(response))
            {
                return null;
            }

            var index = await response.Content.ReadFromJsonAsync<WordPressIndex>(
                JsonOptions,
                cancellationToken);

            return IsSupportedApiIndex(index) ? new WordPressRoot(rootUri) : null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (JsonException exception)
        {
            logger.LogInformation(
                exception,
                "WordPress REST API discovery returned invalid JSON for host {WordPressHost}.",
                rootUri.Host);

            return null;
        }
        catch (HttpRequestException exception)
        {
            logger.LogInformation(
                exception,
                "WordPress REST API root probe failed for host {WordPressHost}.",
                rootUri.Host);

            return null;
        }
    }

    private static bool IsSupportedApiIndex(WordPressIndex? index)
    {
        if (index is null)
        {
            return false;
        }

        return index.Namespaces?.Contains("wp/v2", StringComparer.Ordinal) is true ||
            index.Routes?.Keys.Any(static route =>
                route.Equals("/wp/v2", StringComparison.Ordinal) ||
                route.StartsWith("/wp/v2/", StringComparison.Ordinal)) is true;
    }

    private static bool IsJsonResponse(HttpResponseMessage response)
    {
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        return mediaType?.Contains("json", StringComparison.OrdinalIgnoreCase) is true;
    }

    private static bool HasApiRel(string parameters)
    {
        foreach (var parameter in parameters.Split(';', StringSplitOptions.TrimEntries))
        {
            var separator = parameter.IndexOf('=', StringComparison.Ordinal);
            if (separator <= 0)
            {
                continue;
            }

            var name = parameter[..separator];
            var value = parameter[(separator + 1)..].Trim('"');
            if (name.Equals("rel", StringComparison.OrdinalIgnoreCase) &&
                value.Equals(ApiRel, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> SplitLinkHeaderValue(string header)
    {
        var start = 0;
        var inQuotes = false;
        for (var index = 0; index < header.Length; index++)
        {
            if (header[index] == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (header[index] == ',' && !inQuotes)
            {
                yield return header[start..index].Trim();
                start = index + 1;
            }
        }

        yield return header[start..].Trim();
    }

    private static Uri CreateSiteUri(WordPressSettings settings)
    {
        if (!WordPressSettings.IsValidSiteUrl(settings.SiteUrl) ||
            !Uri.TryCreate(settings.SiteUrl.Trim(), UriKind.Absolute, out var siteUri))
        {
            throw new HttpRequestException(
                "WordPress REST API discovery requires a safe absolute site URL.");
        }

        return siteUri;
    }

    private static Uri BuildPrettyRoot(Uri siteUri)
    {
        var builder = new UriBuilder(siteUri)
        {
            Fragment = string.Empty,
            Query = string.Empty,
            Path = $"{siteUri.AbsolutePath.TrimEnd('/')}/wp-json/"
        };

        return builder.Uri;
    }

    private static Uri BuildRouteRoot(Uri siteUri)
    {
        var builder = new UriBuilder(siteUri)
        {
            Fragment = string.Empty,
            Query = "rest_route=/",
            Path = $"{siteUri.AbsolutePath.TrimEnd('/')}/index.php"
        };

        return builder.Uri;
    }

    private static void AddCandidate(List<Uri> candidates, Uri? candidate)
    {
        if (candidate is null ||
            candidates.Any(existing =>
                string.Equals(
                    existing.AbsoluteUri,
                    candidate.AbsoluteUri,
                    StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        candidates.Add(candidate);
    }

    private sealed record WordPressIndex(
        string[]? Namespaces,
        Dictionary<string, JsonElement>? Routes);

    private sealed record CachedRoot(
        WordPressRoot Root,
        DateTimeOffset ExpiresAtUtc);
}

internal sealed record WordPressRoot(
    Uri RootUri,
    bool DiscoveryCacheHit = false,
    int DiscoveryRequestCount = 0)
{
    internal string EndpointStyle =>
        RootUri.Query.Contains("rest_route=", StringComparison.OrdinalIgnoreCase)
            ? "route_query"
            : "pretty_permalink";

    public Uri BuildRoute(
        string route,
        IReadOnlyDictionary<string, string>? query = null)
    {
        var normalizedRoute = NormalizeRoute(route);

        return HasRouteQuery()
            ? BuildRouteQuery(normalizedRoute, query)
            : BuildPrettyRoute(normalizedRoute, query);
    }

    private static string NormalizeRoute(string route)
    {
        if (string.IsNullOrWhiteSpace(route) ||
            !route.StartsWith("/", StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "WordPress REST API routes must be non-empty and start with '/'.",
                nameof(route));
        }

        return route;
    }

    private Uri BuildPrettyRoute(
        string route,
        IReadOnlyDictionary<string, string>? query)
    {
        var builder = new UriBuilder(RootUri)
        {
            Path = $"{RootUri.AbsolutePath.TrimEnd('/')}/{route.TrimStart('/')}",
            Query = BuildQuery(query)
        };

        return builder.Uri;
    }

    private Uri BuildRouteQuery(
        string route,
        IReadOnlyDictionary<string, string>? query)
    {
        var values = ParseQuery(RootUri.Query)
            .Where(parameter => !parameter.Name.Equals("rest_route", StringComparison.Ordinal))
            .ToList();

        values.Insert(0, new QueryParameter("rest_route", route));
        if (query is not null)
        {
            values.AddRange(query.Select(parameter =>
                new QueryParameter(parameter.Key, parameter.Value)));
        }

        var builder = new UriBuilder(RootUri)
        {
            Query = BuildQuery(values)
        };

        return builder.Uri;
    }

    private bool HasRouteQuery() =>
        ParseQuery(RootUri.Query).Any(parameter =>
            parameter.Name.Equals("rest_route", StringComparison.Ordinal));

    private static string BuildQuery(IReadOnlyDictionary<string, string>? query) =>
        query is null || query.Count == 0
            ? string.Empty
            : BuildQuery(query.Select(parameter =>
                new QueryParameter(parameter.Key, parameter.Value)));

    private static string BuildQuery(IEnumerable<QueryParameter> query) =>
        string.Join(
            "&",
            query.Select(parameter =>
                $"{Escape(parameter.Name)}={EscapeQueryValue(parameter.Value)}"));

    private static IReadOnlyList<QueryParameter> ParseQuery(string query)
    {
        var trimmed = query.TrimStart('?');
        if (string.IsNullOrEmpty(trimmed))
        {
            return [];
        }

        return trimmed
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(parameter =>
            {
                var separator = parameter.IndexOf('=', StringComparison.Ordinal);
                if (separator < 0)
                {
                    return new QueryParameter(Uri.UnescapeDataString(parameter), string.Empty);
                }

                return new QueryParameter(
                    Uri.UnescapeDataString(parameter[..separator]),
                    Uri.UnescapeDataString(parameter[(separator + 1)..]));
            })
            .ToArray();
    }

    private static string Escape(string value) =>
        Uri.EscapeDataString(value);

    private static string EscapeQueryValue(string value) =>
        value.StartsWith("/", StringComparison.Ordinal)
            ? value
            : Escape(value);

    private sealed record QueryParameter(string Name, string Value);
}
