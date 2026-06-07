using System.ComponentModel.DataAnnotations;

namespace YTSkedy.AzureFunctions.Auth;

/// <summary>
/// Non-secret Entra External ID configuration for the API. Bound from the
/// <c>Auth</c> configuration section in <see cref="Microsoft.Extensions.Configuration.IConfiguration"/>.
/// No client secret lives here (Decision #22 in feature 006).
/// </summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>
    /// Authority host for OpenID Connect metadata discovery,
    /// e.g. <c>https://&lt;tenantSubdomain&gt;.ciamlogin.com/</c>.
    /// </summary>
    [Required]
    public string Instance { get; init; } = string.Empty;

    /// <summary>
    /// Entra External ID tenant identifier (GUID).
    /// </summary>
    [Required]
    public string TenantId { get; init; } = string.Empty;

    /// <summary>
    /// API app registration's Application (client) ID. This is the
    /// expected audience for bearer tokens addressed to the API.
    /// </summary>
    [Required]
    public string ClientId { get; init; } = string.Empty;

    /// <summary>
    /// Optional explicit issuer override. Entra External ID's issuer host
    /// uses the tenant-GUID subdomain while the authority uses the tenant
    /// short subdomain, so the library's computed issuer may not match the
    /// token. Setting this pins <c>TokenValidationParameters.ValidIssuer</c>
    /// to the value observed on the user-flow Endpoints tab.
    /// </summary>
    public string? Issuer { get; init; }

    /// <summary>
    /// App role value required on every protected endpoint, e.g.
    /// <c>CalendarEvents.Operator</c>. Matches a role declared on the API
    /// app registration and assigned to operators via Enterprise Application
    /// assignments.
    /// </summary>
    [Required]
    public string RequiredAppRole { get; init; } = string.Empty;
}
