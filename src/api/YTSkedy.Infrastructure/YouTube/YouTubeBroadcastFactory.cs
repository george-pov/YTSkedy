using Google.Apis.YouTube.v3.Data;

namespace YTSkedy.Infrastructure.YouTube;

/// <summary>
/// Maps resolved publish content and YouTube settings to a Google
/// <see cref="LiveBroadcast"/> for <c>liveBroadcasts.insert</c>. Kept separate
/// from the publisher so the request-to-broadcast mapping is unit tested without
/// a live YouTube service. A null description becomes an empty string because the
/// API rejects a null description.
/// </summary>
internal static class YouTubeBroadcastFactory
{
    public static LiveBroadcast Create(
        string title,
        string? description,
        DateTimeOffset scheduledStartUtc,
        string privacyStatus,
        bool selfDeclaredMadeForKids) =>
        new()
        {
            Snippet = new LiveBroadcastSnippet
            {
                Title = title,
                Description = description ?? string.Empty,
                ScheduledStartTimeDateTimeOffset = scheduledStartUtc
            },
            Status = new LiveBroadcastStatus
            {
                PrivacyStatus = privacyStatus,
                SelfDeclaredMadeForKids = selfDeclaredMadeForKids
            }
        };
}
