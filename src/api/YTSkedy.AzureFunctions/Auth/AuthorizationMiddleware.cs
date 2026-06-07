using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
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
/// Deny by default: HTTP triggers without an explicit
/// <see cref="AllowAnonymousAttribute"/> are subject to the role check
/// even if the handler method cannot be resolved or carries no
/// <see cref="RequiredScopeAttribute"/>. New public endpoints must opt out
/// with <see cref="AllowAnonymousAttribute"/>. The scope/role decision
/// itself lives in <see cref="AuthorizationPolicy"/>.
/// </summary>
internal sealed class AuthorizationMiddleware(IOptionsMonitor<AuthOptions> authOptions) : IFunctionsWorkerMiddleware
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
        var outcome = AuthorizationPolicy.Evaluate(
            method,
            authOptions.CurrentValue.RequiredAppRole,
            httpContext.User);

        if (outcome != AuthorizationOutcome.Allow)
        {
            httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        await next(context);
    }
}
