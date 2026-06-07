using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Options;

namespace YTSkedy.AzureFunctions.Cors;

/// <summary>
/// Handles CORS for browser bearer-token calls in code rather than via the
/// Functions host's <c>--cors</c> flag, so the allow-list lives with the
/// rest of the worker configuration and preflight returns <c>204</c>
/// without invoking the authentication pipeline (Decision #23, T029a).
///
/// Pipeline contract: must run before <c>BearerTokenMiddleware</c>.
/// Disallowed origins receive no CORS headers; the browser blocks the call
/// client side. Non-CORS requests (no <c>Origin</c> header) pass through
/// unchanged.
/// </summary>
internal sealed class CorsMiddleware : IFunctionsWorkerMiddleware
{
    private readonly IOptionsMonitor<CorsOptions> _corsOptions;

    public CorsMiddleware(IOptionsMonitor<CorsOptions> corsOptions)
    {
        _corsOptions = corsOptions;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var httpContext = context.GetHttpContext();
        if (httpContext is null)
        {
            await next(context);
            return;
        }

        var decision = CorsPolicy.Evaluate(httpContext, _corsOptions.CurrentValue);
        if (decision.IsPreflight)
        {
            // Always answer preflight ourselves so it does not flow into
            // BearerTokenMiddleware, which would reject the unauthenticated
            // OPTIONS call with 401.
            return;
        }

        await next(context);
    }
}
