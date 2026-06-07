using Microsoft.AspNetCore.Http;
using YTSkedy.AzureFunctions.Cors;

namespace YTSkedy.AzureFunctions.Test.Cors;

/// <summary>
/// CORS contract for browser bearer-token calls (T029a). The Functions
/// worker pipeline boots are exercised by manual <c>.http</c> checks and
/// by <see cref="CorsMiddleware"/>'s direct delegation to
/// <see cref="CorsPolicy"/>; the policy itself is tested here against a
/// real ASP.NET <see cref="HttpContext"/> so every assertion runs against
/// the production header semantics.
/// </summary>
public sealed class CorsPolicyTests
{
    private static readonly CorsOptions Options = new()
    {
        AllowedOrigins = new[]
        {
            "http://localhost:4200",
            "https://app.example.com",
        },
    };

    [Theory]
    // Exact match against the allow-list.
    [InlineData("http://localhost:4200", true)]
    [InlineData("https://app.example.com", true)]
    // Origin matching is ordinal: browsers normalize Origin to lowercase
    // before sending and bytewise-compare the echoed
    // Access-Control-Allow-Origin, so a mixed-case allow-list entry
    // would never match a real request.
    [InlineData("HTTP://LOCALHOST:4200", false)]
    // Trailing slash makes it not an origin. Reject.
    [InlineData("http://localhost:4200/", false)]
    // Wrong port, scheme, or host must not match.
    [InlineData("http://localhost:4201", false)]
    [InlineData("https://localhost:4200", false)]
    [InlineData("http://other.example", false)]
    // Host-prefix attacks must not match: `attacker.localhost:4200` is
    // a different origin from `localhost:4200`.
    [InlineData("http://attacker.localhost:4200", false)]
    // Empty origin header is treated as "no Origin" upstream (covered by
    // Evaluate_RequestWithoutOriginHeader_PassesThroughUnchanged) and is
    // not exercised here.
    public void Evaluate_PreflightFromOrigin_EchoesOriginOnlyWhenAllowed(
        string origin,
        bool expectAllowed)
    {
        var httpContext = NewHttpContext(
            method: HttpMethods.Options,
            origin: origin,
            requestMethod: "GET");

        var decision = CorsPolicy.Evaluate(httpContext, Options);

        Assert.True(decision.IsPreflight);
        Assert.Equal(StatusCodes.Status204NoContent, httpContext.Response.StatusCode);

        var allowOrigin = httpContext.Response.Headers["Access-Control-Allow-Origin"].ToString();
        if (expectAllowed)
        {
            // Origin echoed back verbatim. Browsers compare bytewise, so
            // the response must use the request's exact casing.
            Assert.Equal(origin, allowOrigin);
            Assert.Equal(
                CorsPolicy.AllowedMethods,
                httpContext.Response.Headers["Access-Control-Allow-Methods"]);
            Assert.Equal(
                CorsPolicy.AllowedHeaders,
                httpContext.Response.Headers["Access-Control-Allow-Headers"]);
            Assert.Contains("Origin", httpContext.Response.Headers["Vary"].ToString());
        }
        else
        {
            Assert.Equal(string.Empty, allowOrigin);
            Assert.False(httpContext.Response.Headers.ContainsKey("Access-Control-Allow-Methods"));
            Assert.False(httpContext.Response.Headers.ContainsKey("Access-Control-Allow-Headers"));
        }
    }

    [Fact]
    public void Evaluate_PreflightRequest_ReturnsPreflightDecisionForCallerShortCircuit()
    {
        // CorsMiddleware uses IsPreflight to short-circuit before
        // BearerTokenMiddleware would reject the unauthenticated OPTIONS
        // call. Asserting it here guards the contract the middleware
        // depends on.
        var httpContext = NewHttpContext(
            method: HttpMethods.Options,
            origin: "http://localhost:4200",
            requestMethod: "GET");

        var decision = CorsPolicy.Evaluate(httpContext, Options);

        Assert.True(decision.IsPreflight);
    }

    [Fact]
    public void Evaluate_ActualRequestFromAllowedOrigin_StampsAllowOriginHeader()
    {
        var httpContext = NewHttpContext(
            method: HttpMethods.Get,
            origin: "http://localhost:4200");

        var decision = CorsPolicy.Evaluate(httpContext, Options);

        Assert.False(decision.IsPreflight);
        // Status not set to 204 because the actual handler will set it.
        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
        Assert.Equal(
            "http://localhost:4200",
            httpContext.Response.Headers["Access-Control-Allow-Origin"]);
        Assert.False(
            httpContext.Response.Headers.ContainsKey("Access-Control-Allow-Methods"),
            "Methods header is preflight-only.");
    }

    [Fact]
    public void Evaluate_ActualRequestFromDisallowedOrigin_StampsNoCorsHeaders()
    {
        var httpContext = NewHttpContext(
            method: HttpMethods.Get,
            origin: "http://evil.example");

        var decision = CorsPolicy.Evaluate(httpContext, Options);

        Assert.False(decision.IsPreflight);
        Assert.False(httpContext.Response.Headers.ContainsKey("Access-Control-Allow-Origin"));
    }

    [Fact]
    public void Evaluate_RequestWithoutOriginHeader_PassesThroughUnchanged()
    {
        var httpContext = NewHttpContext(method: HttpMethods.Get, origin: null);

        var decision = CorsPolicy.Evaluate(httpContext, Options);

        Assert.False(decision.IsPreflight);
        Assert.False(httpContext.Response.Headers.ContainsKey("Access-Control-Allow-Origin"));
        Assert.False(httpContext.Response.Headers.ContainsKey("Vary"));
    }

    [Fact]
    public void Evaluate_OptionsWithoutPreflightMethodHeader_IsNotTreatedAsPreflight()
    {
        // OPTIONS calls that lack Access-Control-Request-Method are not
        // CORS preflights; they should flow into the normal pipeline
        // (which will currently 405 because no trigger declares OPTIONS).
        var httpContext = NewHttpContext(
            method: HttpMethods.Options,
            origin: "http://localhost:4200");

        var decision = CorsPolicy.Evaluate(httpContext, Options);

        Assert.False(decision.IsPreflight);
    }

    private static HttpContext NewHttpContext(
        string method,
        string? origin,
        string? requestMethod = null)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = method;
        if (origin is not null)
        {
            httpContext.Request.Headers["Origin"] = origin;
        }
        if (requestMethod is not null)
        {
            httpContext.Request.Headers["Access-Control-Request-Method"] = requestMethod;
        }
        return httpContext;
    }
}
