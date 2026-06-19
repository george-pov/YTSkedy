using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web.Resource;

namespace YTSkedy.AzureFunctions.Auth;

/// <summary>
/// Enforces the workspace-wide app-role requirement
/// (<see cref="AuthOptions.RequiredAppRole"/>) on every HTTP-triggered
/// function and the per-endpoint scope requirement
/// (<see cref="RequiredScopeAttribute"/>) when one is declared. Runs after
/// <see cref="BearerTokenMiddleware"/> so it can inspect the validated
/// <see cref="ClaimsPrincipal"/> on <see cref="HttpContext.User"/>.
///
/// Deny by default: an HTTP trigger whose handler cannot be resolved is denied
/// outright, because its scope and anonymous intent are unknown and must not
/// fail open to a role-only check. A resolved handler without an explicit
/// <see cref="AllowAnonymousAttribute"/> is still subject to the role check
/// even when it carries no <see cref="RequiredScopeAttribute"/>. New public
/// endpoints must opt out with <see cref="AllowAnonymousAttribute"/>. The
/// scope/role decision itself lives in <see cref="AuthorizationPolicy"/>.
/// </summary>
internal sealed class AuthorizationMiddleware(
    IOptionsMonitor<AuthOptions> authOptions,
    ILogger<AuthorizationMiddleware> logger) : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var httpContext = context.GetHttpContext();
        if (httpContext is null)
        {
            // Non-HTTP triggers (timers, queues, etc.) bypass authorization.
            await next(context);
            return;
        }

        var method = EndpointResolver.ResolveMethod(context.FunctionDefinition);
        var result = AuthorizationPolicy.Evaluate(
            method,
            authOptions.CurrentValue.RequiredAppRole,
            httpContext.User);

        if (result == AuthorizationResult.UnresolvedEndpoint)
        {
            // Fail-closed path: the handler could not be resolved, so the scope
            // requirement cannot be read. This is a wiring or deployment fault,
            // so surface it for operators rather than letting the 403 look like
            // an ordinary client denial.
            logger.LogWarning(
                "Denying request to function {FunctionName} ({EntryPoint}): the handler method "
                    + "could not be resolved, so its authorization requirements are unknown.",
                context.FunctionDefinition.Name,
                context.FunctionDefinition.EntryPoint);
        }

        if (result != AuthorizationResult.Allow)
        {
            httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        await next(context);
    }
}
