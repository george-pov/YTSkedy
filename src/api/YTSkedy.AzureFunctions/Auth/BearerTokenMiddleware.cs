using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace YTSkedy.AzureFunctions.Auth;

/// <summary>
/// Authenticates HTTP-triggered requests with the JWT bearer scheme
/// configured by <c>Microsoft.Identity.Web</c>, then forwards execution.
/// Non-HTTP triggers (timers, queues, etc.) and endpoints marked with
/// <see cref="Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute"/>
/// bypass authentication.
/// Returns <c>401</c> when no valid bearer token is present;
/// <c>Microsoft.Identity.Web</c> handles signing-key rotation, OIDC metadata
/// caching, audience, expiration, and clock skew under
/// <see cref="JwtBearerDefaults.AuthenticationScheme"/>.
/// </summary>
internal sealed class BearerTokenMiddleware : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var httpContext = context.GetHttpContext();
        if (httpContext is null)
        {
            await next(context);
            return;
        }

        if (EndpointResolver.AllowsAnonymous(context.FunctionDefinition))
        {
            await next(context);
            return;
        }

        var result = await httpContext.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
        if (!result.Succeeded)
        {
            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        httpContext.User = result.Principal!;
        await next(context);
    }
}
