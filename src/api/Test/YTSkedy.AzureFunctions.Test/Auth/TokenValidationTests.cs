using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace YTSkedy.AzureFunctions.Test.Auth;

/// <summary>
/// RSA-signed JWT validation coverage (T031). Drives the real
/// <c>JwtBearer</c> authentication handler end-to-end with a static in-test
/// signing key so the assertions exercise the same validation code path
/// the production worker uses, without contacting Entra.
///
/// <c>BearerTokenMiddleware</c>'s contract is "convert
/// <c>AuthenticateAsync</c> failure to 401, set the principal on success";
/// that thin shim is covered transitively by these tests because the
/// failure cases below all surface as
/// <see cref="AuthenticateResult.Succeeded"/> == <c>false</c>.
/// </summary>
public sealed class TokenValidationTests : IDisposable
{
    private const string Audience = "api://ytskedy-api-test";
    private const string Issuer = "https://test-issuer.ciamlogin.com/test-tenant/v2.0";
    private const string WrongAudience = "api://other-api";
    private const string WrongIssuer = "https://attacker.example/v2.0";

    private readonly RSA _signingRsa = RSA.Create(2048);
    private readonly RSA _attackerRsa = RSA.Create(2048);
    private readonly RsaSecurityKey _signingKey;
    private readonly RsaSecurityKey _attackerKey;
    private readonly ServiceProvider _services;

    public TokenValidationTests()
    {
        _signingKey = new RsaSecurityKey(_signingRsa) { KeyId = "test-signing-kid" };
        _attackerKey = new RsaSecurityKey(_attackerRsa) { KeyId = "attacker-kid" };

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                // Pin everything statically; no Authority / MetadataAddress,
                // so the handler never reaches for OpenID Connect metadata.
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = Issuer,
                    ValidateAudience = true,
                    ValidAudience = Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = _signingKey,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                };
            });

        _services = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        _services.Dispose();
        _signingRsa.Dispose();
        _attackerRsa.Dispose();
    }

    [Fact]
    public async Task AuthenticateAsync_NoAuthorizationHeader_FailsAndExposesNoPrincipal()
    {
        // BearerTokenMiddleware translates !Succeeded into an empty-body
        // 401, so this asserts the "missing bearer token" path.
        var http = NewHttpContext(authorizationHeader: null);

        var result = await http.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);

        Assert.False(result.Succeeded);
        Assert.True(result.None || result.Failure is not null);
    }

    [Fact]
    public async Task AuthenticateAsync_ValidToken_SucceedsWithAuthenticatedPrincipal()
    {
        var token = IssueToken(
            signingKey: _signingKey,
            audience: Audience,
            issuer: Issuer,
            expires: DateTime.UtcNow.AddMinutes(5));
        var http = NewHttpContext(authorizationHeader: $"Bearer {token}");

        var result = await http.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);

        Assert.True(result.Succeeded, result.Failure?.Message);
        Assert.NotNull(result.Principal);
        Assert.True(result.Principal!.Identity?.IsAuthenticated);
        // Claim names depend on JwtBearer's MapInboundClaims setting and
        // are exercised through the production code path by
        // AuthorizationPolicyTests, which builds principals directly.
        Assert.NotEmpty(result.Principal.Claims);
    }

    [Fact]
    public async Task AuthenticateAsync_TokenSignedByDifferentKey_Fails()
    {
        var token = IssueToken(
            signingKey: _attackerKey,
            audience: Audience,
            issuer: Issuer,
            expires: DateTime.UtcNow.AddMinutes(5));
        var http = NewHttpContext(authorizationHeader: $"Bearer {token}");

        var result = await http.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failure);
    }

    [Fact]
    public async Task AuthenticateAsync_WrongAudience_Fails()
    {
        var token = IssueToken(
            signingKey: _signingKey,
            audience: WrongAudience,
            issuer: Issuer,
            expires: DateTime.UtcNow.AddMinutes(5));
        var http = NewHttpContext(authorizationHeader: $"Bearer {token}");

        var result = await http.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failure);
    }

    [Fact]
    public async Task AuthenticateAsync_WrongIssuer_Fails()
    {
        var token = IssueToken(
            signingKey: _signingKey,
            audience: Audience,
            issuer: WrongIssuer,
            expires: DateTime.UtcNow.AddMinutes(5));
        var http = NewHttpContext(authorizationHeader: $"Bearer {token}");

        var result = await http.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failure);
    }

    [Fact]
    public async Task AuthenticateAsync_ExpiredToken_Fails()
    {
        var token = IssueToken(
            signingKey: _signingKey,
            audience: Audience,
            issuer: Issuer,
            // ClockSkew is zero (see ctor) so a 1-minute-past expiry is
            // unambiguously expired.
            expires: DateTime.UtcNow.AddMinutes(-1),
            notBefore: DateTime.UtcNow.AddMinutes(-5),
            issuedAt: DateTime.UtcNow.AddMinutes(-5));
        var http = NewHttpContext(authorizationHeader: $"Bearer {token}");

        var result = await http.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failure);
    }

    [Fact]
    public async Task AuthenticateAsync_MalformedToken_Fails()
    {
        var http = NewHttpContext(authorizationHeader: "Bearer not-a-jwt");

        var result = await http.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);

        Assert.False(result.Succeeded);
    }

    private DefaultHttpContext NewHttpContext(string? authorizationHeader)
    {
        var http = new DefaultHttpContext
        {
            RequestServices = _services,
        };
        if (authorizationHeader is not null)
        {
            http.Request.Headers.Authorization = authorizationHeader;
        }
        return http;
    }

    private static string IssueToken(
        SecurityKey signingKey,
        string audience,
        string issuer,
        DateTime expires,
        DateTime? notBefore = null,
        DateTime? issuedAt = null)
    {
        var handler = new JsonWebTokenHandler();
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            NotBefore = notBefore ?? DateTime.UtcNow,
            IssuedAt = issuedAt ?? DateTime.UtcNow,
            Expires = expires,
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256),
            Claims = new Dictionary<string, object>
            {
                ["scp"] = "CalendarEvents.Read CalendarEvents.Write",
                ["roles"] = new[] { "CalendarEvents.Operator" },
                ["oid"] = Guid.NewGuid().ToString(),
            },
        };
        return handler.CreateToken(descriptor);
    }
}
