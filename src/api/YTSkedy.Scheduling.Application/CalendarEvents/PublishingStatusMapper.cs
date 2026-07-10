namespace YTSkedy.Scheduling.Application.CalendarEvents;

public static class PublishingStatusMapper
{
    public static PublishingStatus Map(
        IReadOnlySet<string> publishedPlatformIds,
        IReadOnlySet<string> activePlatformIds)
    {
        if (publishedPlatformIds.Count == 0)
        {
            return PublishingStatus.NotPublished;
        }

        var publishedIds = new HashSet<string>(
            publishedPlatformIds,
            StringComparer.Ordinal);

        return activePlatformIds.Count > 0 &&
            activePlatformIds.All(publishedIds.Contains)
                ? PublishingStatus.FullyPublished
                : PublishingStatus.PartiallyPublished;
    }
}
