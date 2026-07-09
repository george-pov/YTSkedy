using System.Security.Claims;

namespace YTSkedy.TestSupport;

public static class ClaimsPrincipalFactory
{
    public const string ScopeSchemaClaim = "http://schemas.microsoft.com/identity/claims/scope";

    public static ClaimsPrincipal WithRawClaims(string? scopeClaim, params string[] roles)
    {
        var identity = new ClaimsIdentity(authenticationType: "test");
        if (scopeClaim is not null)
        {
            identity.AddClaim(new Claim("scp", scopeClaim));
        }

        foreach (var role in roles)
        {
            identity.AddClaim(new Claim("roles", role));
        }

        return new ClaimsPrincipal(identity);
    }

    public static ClaimsPrincipal WithSchemaScope(string scopeClaim, params string[] roles)
    {
        var identity = new ClaimsIdentity(authenticationType: "test");
        identity.AddClaim(new Claim(ScopeSchemaClaim, scopeClaim));
        foreach (var role in roles)
        {
            identity.AddClaim(new Claim("roles", role));
        }

        return new ClaimsPrincipal(identity);
    }

    public static ClaimsPrincipal WithMappedRole(string? scopeClaim, params string[] roles)
    {
        var identity = new ClaimsIdentity(
            authenticationType: "test",
            nameType: ClaimsIdentity.DefaultNameClaimType,
            roleType: ClaimTypes.Role);

        if (scopeClaim is not null)
        {
            identity.AddClaim(new Claim("scp", scopeClaim));
        }

        foreach (var role in roles)
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, role));
        }

        return new ClaimsPrincipal(identity);
    }
}
