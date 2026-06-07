using Microsoft.AspNetCore.Http;

namespace YTSkedy.AzureFunctions.Cors;

/// <summary>
/// Pure CORS policy logic for browser bearer-token calls. Lives separate
/// from <see cref="CorsMiddleware"/> so it can be tested directly against
/// an <see cref="HttpContext"/> without booting the Functions worker.
/// </summary>
public static class CorsPolicy
{
    private const string OriginHeader = "Origin";
    private const string PreflightMethodHeader = "Access-Control-Request-Method";

    private const string AllowOriginHeader = "Access-Control-Allow-Origin";
    private const string AllowMethodsHeader = "Access-Control-Allow-Methods";
    private const string AllowHeadersHeader = "Access-Control-Allow-Headers";
    private const string VaryHeader = "Vary";

    public const string AllowedMethods = "GET, POST, OPTIONS";
    public const string AllowedHeaders = "Content-Type, Authorization";

    /// <summary>
    /// Inspects the request, stamps CORS response headers when the origin
    /// is allowed, and returns whether the request is a preflight that the
    /// caller should short-circuit (status 204, no further pipeline).
    /// </summary>
    public static CorsDecision Evaluate(HttpContext httpContext, CorsOptions options)
    {
        var origin = httpContext.Request.Headers[OriginHeader].ToString();
        if (string.IsNullOrEmpty(origin))
        {
            return CorsDecision.NotCors;
        }

        var originAllowed = IsOriginAllowed(origin, options);
        var isPreflight = HttpMethods.IsOptions(httpContext.Request.Method)
            && httpContext.Request.Headers.ContainsKey(PreflightMethodHeader);

        if (isPreflight)
        {
            httpContext.Response.StatusCode = StatusCodes.Status204NoContent;
            if (originAllowed)
            {
                ApplyCorsHeaders(httpContext, origin, includePreflightHeaders: true);
            }
            return CorsDecision.Preflight;
        }

        if (originAllowed)
        {
            ApplyCorsHeaders(httpContext, origin, includePreflightHeaders: false);
        }

        return CorsDecision.PassThrough;
    }

    private static bool IsOriginAllowed(string origin, CorsOptions options)
    {
        foreach (var allowed in options.AllowedOrigins)
        {
            // Browsers normalize the Origin header to lowercase before sending
            // and bytewise-compare the echoed Access-Control-Allow-Origin, so
            // matching must be ordinal too — case-insensitive matching would
            // let `HTTP://...` through the server but the browser would still
            // reject the response.
            if (string.Equals(allowed, origin, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static void ApplyCorsHeaders(
        HttpContext httpContext,
        string origin,
        bool includePreflightHeaders)
    {
        var headers = httpContext.Response.Headers;
        headers[AllowOriginHeader] = origin;
        headers.Append(VaryHeader, OriginHeader);

        if (includePreflightHeaders)
        {
            headers[AllowMethodsHeader] = AllowedMethods;
            headers[AllowHeadersHeader] = AllowedHeaders;
        }
    }
}

public readonly struct CorsDecision
{
    public bool IsPreflight { get; }

    private CorsDecision(bool isPreflight)
    {
        IsPreflight = isPreflight;
    }

    public static CorsDecision NotCors { get; } = new(false);
    public static CorsDecision PassThrough { get; } = new(false);
    public static CorsDecision Preflight { get; } = new(true);
}
