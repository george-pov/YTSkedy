namespace YTSkedy.AzureFunctions.Platforms;

/// <summary>
/// Envelope returned by <c>GET /api/platforms</c>. Each item carries its id and
/// type, so a client always has what the update and delete routes need.
/// </summary>
public sealed record PlatformListResponse(
    IReadOnlyList<PlatformResponse> Items);
