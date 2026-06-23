using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web.Resource;

namespace YTSkedy.AzureFunctions.Auth;

/// <summary>
/// Pure scope/role authorization logic for HTTP-triggered functions.
/// Lives separate from <see cref="AuthorizationMiddleware"/> so it can be
/// tested directly against a synthesized <see cref="ClaimsPrincipal"/>
/// without booting the Functions worker, mirroring the
/// <c>CorsPolicy</c>/<c>CorsMiddleware</c> split.
/// </summary>
internal static class AuthorizationPolicy
{
    // Entra emits delegated scopes in the short JWT claim `scp`. Some
    // legacy paths (and Microsoft.Identity.Web's claim mapping) surface
    // the same value under the long schema URL; honor both.
    private const string ScopeSchemaClaim = "http://schemas.microsoft.com/identity/claims/scope";

    /// <summary>
    /// Resolves accepted scopes and the opt-out marker from
    /// <paramref name="method"/> and delegates to the primitive overload.
    /// Fail closed: a null method means the endpoint could not be resolved, so
    /// its scope and anonymous intent are unknown and the request is denied with
    /// <see cref="AuthorizationResult.UnresolvedEndpoint"/> rather than letting
    /// an empty scope set read as "no scope required" and downgrade the endpoint
    /// to role-only. A resolved method without
    /// <see cref="RequiredScopeAttribute"/> is legitimately scope-less and is
    /// still subject to the role check.
    /// </summary>
    internal static AuthorizationResult Evaluate(
        MethodInfo? method,
        string requiredRole,
        ClaimsPrincipal user)
    {
        if (method is null)
        {
            // The endpoint's handler could not be resolved, so its declared
            // scope and anonymous intent are unknown. Deny instead of treating
            // "no scopes resolved" as "no scope required".
            return AuthorizationResult.UnresolvedEndpoint;
        }

        if (method.GetCustomAttribute<AllowAnonymousAttribute>() is not null)
        {
            return AuthorizationResult.Allow;
        }

        var acceptedScopes =
            method.GetCustomAttribute<RequiredScopeAttribute>()?.AcceptedScope ?? [];

        return Evaluate(acceptedScopes, requiredRole, user);
    }

    /// <summary>
    /// Decides whether <paramref name="user"/> may invoke an endpoint
    /// whose <c>[RequiredScope]</c> accepts any of
    /// <paramref name="acceptedScopes"/> and whose workspace-wide
    /// app-role requirement is <paramref name="requiredRole"/>.
    /// </summary>
    internal static AuthorizationResult Evaluate(
        IReadOnlyCollection<string> acceptedScopes,
        string requiredRole,
        ClaimsPrincipal user)
    {
        if (!UserHasAnyScope(user, acceptedScopes))
        {
            return AuthorizationResult.InsufficientScope;
        }

        if (!UserHasRole(user, requiredRole))
        {
            return AuthorizationResult.MissingRole;
        }

        return AuthorizationResult.Allow;
    }

    private static bool UserHasAnyScope(ClaimsPrincipal user, IReadOnlyCollection<string> required)
    {
        if (required.Count == 0)
        {
            return true;
        }

        var scopes = GetScopeClaims(user);
        return required.Any(scopes.Contains);
    }

    private static HashSet<string> GetScopeClaims(ClaimsPrincipal user)
    {
        var scopes = new HashSet<string>(StringComparer.Ordinal);
        AddSpaceSeparated(scopes, user.FindFirst("scp")?.Value);
        AddSpaceSeparated(scopes, user.FindFirst(ScopeSchemaClaim)?.Value);
        return scopes;
    }

    private static void AddSpaceSeparated(HashSet<string> sink, string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        foreach (var token in raw.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            sink.Add(token);
        }
    }

    private static bool UserHasRole(ClaimsPrincipal user, string requiredRole)
    {
        if (string.IsNullOrWhiteSpace(requiredRole))
        {
            return true;
        }

        // Microsoft.Identity.Web maps `roles` to ClaimTypes.Role so IsInRole
        // works in production, but check the raw claim too in case the
        // mapping is suppressed by host configuration.
        return user.IsInRole(requiredRole)
            || user.FindAll("roles").Any(claim => string.Equals(claim.Value, requiredRole, StringComparison.Ordinal));
    }
}

internal enum AuthorizationResult
{
    Allow,
    InsufficientScope,
    MissingRole,
    UnresolvedEndpoint,
}
