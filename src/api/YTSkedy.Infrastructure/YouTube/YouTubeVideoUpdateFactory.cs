using Google.Apis.YouTube.v3.Data;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Infrastructure.YouTube;

/// <summary>
/// Determines whether a newly created broadcast needs a video update and
/// creates replacement-safe request bodies for the included API parts.
/// </summary>
internal static class YouTubeVideoUpdateFactory
{
    internal static YouTubeVideoUpdateParts RequiredParts(YouTubeSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return new YouTubeVideoUpdateParts(
            IncludeSnippet:
                settings.CategoryId is not null ||
                settings.DefaultAudioLanguage is not null ||
                settings.DefaultLanguage is not null,
            IncludeStatus:
                settings.ContainsSyntheticMedia ||
                !string.Equals(settings.PrivacyStatus, "private", StringComparison.Ordinal));
    }

    internal static YouTubeVideoUpdate Create(
        Video current,
        YouTubeSettings settings,
        YouTubeVideoUpdateParts parts)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(current.Id);

        var update = new Video { Id = current.Id };

        if (parts.IncludeSnippet)
        {
            var snippet = current.Snippet
                ?? throw new InvalidOperationException(
                    "YouTube returned a video without its requested snippet part.");

            update.Snippet = new VideoSnippet
            {
                CategoryId = settings.CategoryId ?? snippet.CategoryId,
                DefaultAudioLanguage =
                    settings.DefaultAudioLanguage ?? snippet.DefaultAudioLanguage,
                DefaultLanguage = settings.DefaultLanguage ?? snippet.DefaultLanguage,
                Description = snippet.Description,
                Tags = snippet.Tags?.ToArray(),
                Title = snippet.Title
            };
        }

        if (parts.IncludeStatus)
        {
            var status = current.Status
                ?? throw new InvalidOperationException(
                    "YouTube returned a video without its requested status part.");

            update.Status = new VideoStatus
            {
                ContainsSyntheticMedia = settings.ContainsSyntheticMedia,
                Embeddable = status.Embeddable,
                License = status.License,
                PrivacyStatus = settings.PrivacyStatus,
                PublicStatsViewable = status.PublicStatsViewable,
                PublishAtDateTimeOffset = string.Equals(
                    settings.PrivacyStatus,
                    "private",
                    StringComparison.Ordinal)
                        ? status.PublishAtDateTimeOffset
                        : null,
                SelfDeclaredMadeForKids = settings.SelfDeclaredMadeForKids
            };
        }

        return new YouTubeVideoUpdate(
            update,
            parts.ApiValue ?? throw new ArgumentException(
                "At least one YouTube video part is required.",
                nameof(parts)));
    }
}

internal readonly record struct YouTubeVideoUpdate(Video Video, string Parts);

internal readonly record struct YouTubeVideoUpdateParts(
    bool IncludeSnippet,
    bool IncludeStatus)
{
    internal string? ApiValue => (IncludeSnippet, IncludeStatus) switch
    {
        (true, true) => "snippet,status",
        (true, false) => "snippet",
        (false, true) => "status",
        _ => null
    };
}
