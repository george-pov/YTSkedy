using System.ComponentModel.DataAnnotations;

namespace YTSkedy.AzureFunctions.Cors;

/// <summary>
/// Browser CORS allow-list for bearer-token calls from the SPA. Values come
/// from the <c>Cors</c> configuration section. Only the origins listed here
/// receive CORS headers on responses; everything else gets no
/// <c>Access-Control-Allow-Origin</c> and the browser blocks the call client
/// side.
/// </summary>
public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    /// <summary>
    /// Exact origins (scheme + host + port) allowed to call the API from a
    /// browser, e.g. <c>http://localhost:4200</c>. Trailing slashes are not
    /// expected and are not stripped here.
    /// </summary>
    [Required]
    [MinLength(1)]
    public IReadOnlyList<string> AllowedOrigins { get; init; } = Array.Empty<string>();
}
