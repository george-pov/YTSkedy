using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web.Resource;
using AuthorizationPolicy = YTSkedy.AzureFunctions.Auth.AuthorizationPolicy;
using YTSkedy.AzureFunctions.Auth;
using YTSkedy.TestSupport;

namespace YTSkedy.AzureFunctions.Test.Auth;

/// <summary>
/// Synthesized-principal coverage for the scope/role decision (T031).
/// Tests opt into scopes and roles per case by minting a fresh
/// <see cref="ClaimsPrincipal"/> rather than going through real token
/// validation; <c>TokenValidationTests</c> covers the JWT pipeline.
/// </summary>
public sealed class AuthorizationPolicyTests
{
    private const string ReadScope = "CalendarEvents.Read";
    private const string WriteScope = "CalendarEvents.Write";
    private const string OperatorRole = "CalendarEvents.Operator";

    private static readonly string[] ReadOnly = [ReadScope];

    [Fact]
    public void Evaluate_PrincipalWithRequiredScopeAndRole_Allow()
    {
        var user = ClaimsPrincipalFactory.WithRawClaims(ReadScope, OperatorRole);

        var result = AuthorizationPolicy.Evaluate(ReadOnly, OperatorRole, user);

        Assert.Equal(AzureFunctions.Auth.AuthorizationResult.Allow, result);
    }

    [Fact]
    public void Evaluate_PrincipalWithRoleButMissingScope_InsufficientScope()
    {
        var user = ClaimsPrincipalFactory.WithRawClaims(WriteScope, OperatorRole);

        var result = AuthorizationPolicy.Evaluate(ReadOnly, OperatorRole, user);

        Assert.Equal(AzureFunctions.Auth.AuthorizationResult.InsufficientScope, result);
    }

    [Fact]
    public void Evaluate_PrincipalWithScopeButNoRole_MissingRole()
    {
        var user = ClaimsPrincipalFactory.WithRawClaims(ReadScope);

        var result = AuthorizationPolicy.Evaluate(ReadOnly, OperatorRole, user);

        Assert.Equal(AzureFunctions.Auth.AuthorizationResult.MissingRole, result);
    }

    [Fact]
    public void Evaluate_PrincipalWithScopeButWrongRole_MissingRole()
    {
        var user = ClaimsPrincipalFactory.WithRawClaims(ReadScope, "CalendarEvents.Reader");

        var result = AuthorizationPolicy.Evaluate(ReadOnly, OperatorRole, user);

        Assert.Equal(AzureFunctions.Auth.AuthorizationResult.MissingRole, result);
    }

    [Fact]
    public void Evaluate_AnonymousPrincipal_InsufficientScope()
    {
        // Empty principal mirrors the "missing principal" case from the
        // task: BearerTokenMiddleware fails AuthenticateAsync and the
        // pipeline returns 401 before reaching the authorization step.
        // If anything ever reached the policy with an empty principal,
        // the scope check fails first.
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        var result = AuthorizationPolicy.Evaluate(ReadOnly, OperatorRole, user);

        Assert.Equal(AzureFunctions.Auth.AuthorizationResult.InsufficientScope, result);
    }

    [Fact]
    public void Evaluate_ScpClaimWithMultipleSpaceSeparatedScopes_MatchesAny()
    {
        // Entra emits delegated scopes as a single space-separated string
        // in `scp` (e.g. "CalendarEvents.Read CalendarEvents.Write").
        var user = ClaimsPrincipalFactory.WithRawClaims(
            $"{ReadScope} {WriteScope}",
            OperatorRole);

        var result = AuthorizationPolicy.Evaluate(
            new[] { WriteScope },
            OperatorRole,
            user);

        Assert.Equal(AzureFunctions.Auth.AuthorizationResult.Allow, result);
    }

    [Fact]
    public void Evaluate_ScopeOnSchemaClaimUrl_IsAccepted()
    {
        // Microsoft.Identity.Web's claim mapping can surface scopes under
        // the long schema URL instead of the short `scp` name.
        var user = ClaimsPrincipalFactory.WithSchemaScope(ReadScope, OperatorRole);

        var result = AuthorizationPolicy.Evaluate(ReadOnly, OperatorRole, user);

        Assert.Equal(AzureFunctions.Auth.AuthorizationResult.Allow, result);
    }

    [Fact]
    public void Evaluate_NoAcceptedScopes_ShortCircuitsToRoleCheck()
    {
        // Defensive contract: an endpoint that accepts zero scopes (no
        // RequiredScope attribute or an empty AcceptedScope) does not
        // require a scope, but still requires the role.
        var user = ClaimsPrincipalFactory.WithRawClaims(scopeClaim: null, OperatorRole);

        var result = AuthorizationPolicy.Evaluate([], OperatorRole, user);

        Assert.Equal(AzureFunctions.Auth.AuthorizationResult.Allow, result);
    }

    [Fact]
    public void Evaluate_NoRequiredRole_AllowsAnyAuthenticatedScope()
    {
        // Defensive contract: if Auth:RequiredAppRole is unset, only the
        // scope is enforced.
        var user = ClaimsPrincipalFactory.WithRawClaims(ReadScope);

        var result = AuthorizationPolicy.Evaluate(ReadOnly, requiredRole: "", user);

        Assert.Equal(AzureFunctions.Auth.AuthorizationResult.Allow, result);
    }

    [Fact]
    public void Evaluate_RoleViaClaimTypesRole_IsAccepted()
    {
        // Microsoft.Identity.Web maps `roles` to ClaimTypes.Role so
        // IsInRole works against a mapped principal.
        var user = ClaimsPrincipalFactory.WithMappedRole(ReadScope, OperatorRole);

        var result = AuthorizationPolicy.Evaluate(ReadOnly, OperatorRole, user);

        Assert.Equal(AzureFunctions.Auth.AuthorizationResult.Allow, result);
    }
}

/// <summary>
/// Coverage for the <c>MethodInfo</c> overload that resolves
/// <c>[RequiredScope]</c> and the <c>[AllowAnonymous]</c> opt-out the
/// middleware delegates to. Asserts the deny-by-default contract so a
/// future HTTP trigger added without a scope attribute still falls under
/// the workspace-wide role check.
/// </summary>
public sealed class AuthorizationPolicyMethodInfoTests
{
    private const string ReadScope = "CalendarEvents.Read";
    private const string WriteScope = "CalendarEvents.Write";
    private const string OperatorRole = "CalendarEvents.Operator";

    private static MethodInfo MethodOf(string name) =>
        typeof(SampleEndpoints).GetMethod(name)
        ?? throw new InvalidOperationException($"Test fixture missing method {name}.");

    [Fact]
    public void Evaluate_NullMethod_DeniesAsUnresolvedEndpoint()
    {
        // Entry point could not be resolved (typo, renamed handler, etc.).
        // Fail closed: an unresolved endpoint is denied outright rather than
        // downgraded to a role-only check by an empty scope set.
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        var result = AuthorizationPolicy.Evaluate(method: null, OperatorRole, user);

        Assert.Equal(AzureFunctions.Auth.AuthorizationResult.UnresolvedEndpoint, result);
    }

    [Fact]
    public void Evaluate_NullMethod_DeniesEvenForFullyPrivilegedPrincipal()
    {
        // The crux of the fix: a principal carrying both the scope and the
        // role is still denied when the method cannot be resolved, because the
        // endpoint's true scope requirement is unknown. Previously the missing
        // [RequiredScope] read as "no scope required" and the request fell
        // through to a role-only check.
        var user = ClaimsPrincipalFactory.WithRawClaims(ReadScope, OperatorRole);

        var result = AuthorizationPolicy.Evaluate(method: null, OperatorRole, user);

        Assert.Equal(AzureFunctions.Auth.AuthorizationResult.UnresolvedEndpoint, result);
    }

    [Fact]
    public void Evaluate_MethodWithoutAnyAttribute_RequiresRole()
    {
        // A future HTTP trigger landing without [RequiredScope] and
        // without [AllowAnonymous] must still pass the role gate.
        var user = ClaimsPrincipalFactory.WithRawClaims(scopeClaim: null);

        var result = AuthorizationPolicy.Evaluate(
            MethodOf(nameof(SampleEndpoints.Bare)),
            OperatorRole,
            user);

        Assert.Equal(AzureFunctions.Auth.AuthorizationResult.MissingRole, result);
    }

    [Fact]
    public void Evaluate_MethodWithoutAnyAttribute_AllowsWhenRoleClaimPresent()
    {
        // The role gate is the only check; a principal with the operator
        // role passes even without any scope claim.
        var user = ClaimsPrincipalFactory.WithRawClaims(scopeClaim: null, OperatorRole);

        var result = AuthorizationPolicy.Evaluate(
            MethodOf(nameof(SampleEndpoints.Bare)),
            OperatorRole,
            user);

        Assert.Equal(AzureFunctions.Auth.AuthorizationResult.Allow, result);
    }

    [Fact]
    public void Evaluate_AllowAnonymousMethod_SkipsBothChecksForAnonymousUser()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        var result = AuthorizationPolicy.Evaluate(
            MethodOf(nameof(SampleEndpoints.Anonymous)),
            OperatorRole,
            user);

        Assert.Equal(AzureFunctions.Auth.AuthorizationResult.Allow, result);
    }

    [Fact]
    public void Evaluate_RequiredScopeWithoutRoleClaim_DeniesWithMissingRole()
    {
        // The scope is satisfied but the role is not; the existing
        // contract continues to apply through the MethodInfo overload.
        var user = ClaimsPrincipalFactory.WithRawClaims(ReadScope);

        var result = AuthorizationPolicy.Evaluate(
            MethodOf(nameof(SampleEndpoints.ReadOnly)),
            OperatorRole,
            user);

        Assert.Equal(AzureFunctions.Auth.AuthorizationResult.MissingRole, result);
    }

    [Fact]
    public void Evaluate_RequiredScopeMissing_DeniesWithInsufficientScope()
    {
        var user = ClaimsPrincipalFactory.WithRawClaims(WriteScope, OperatorRole);

        var result = AuthorizationPolicy.Evaluate(
            MethodOf(nameof(SampleEndpoints.ReadOnly)),
            OperatorRole,
            user);

        Assert.Equal(AzureFunctions.Auth.AuthorizationResult.InsufficientScope, result);
    }

    [Fact]
    public void Evaluate_RequiredScopeAndRolePresent_Allows()
    {
        var user = ClaimsPrincipalFactory.WithRawClaims(ReadScope, OperatorRole);

        var result = AuthorizationPolicy.Evaluate(
            MethodOf(nameof(SampleEndpoints.ReadOnly)),
            OperatorRole,
            user);

        Assert.Equal(AzureFunctions.Auth.AuthorizationResult.Allow, result);
    }
    // Fixture standing in for handler methods on a Functions class. The
    // policy only reads attributes from the MethodInfo, so the bodies and
    // signatures here are intentionally trivial.
    private sealed class SampleEndpoints
    {
        public void Bare() { }

        [AllowAnonymous]
        public void Anonymous() { }

        [RequiredScope(ReadScope)]
        public void ReadOnly() { }
    }
}
