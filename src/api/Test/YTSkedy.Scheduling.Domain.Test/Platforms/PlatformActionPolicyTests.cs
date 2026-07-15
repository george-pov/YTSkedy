using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Domain.Test.Platforms;

public class PlatformActionPolicyTests
{
    public static TheoryData<CanPublishCase> CanPublishCases => new()
    {
        new(
            "FutureActiveNotPublished",
            PublishStatus.NotPublished,
            IsOrphaned: false,
            IsFuture: true,
            Expected: true),
        new(
            "PastActiveNotPublished",
            PublishStatus.NotPublished,
            IsOrphaned: false,
            IsFuture: false,
            Expected: false),
        new(
            "FutureOrphanNotPublished",
            PublishStatus.NotPublished,
            IsOrphaned: true,
            IsFuture: true,
            Expected: false),
        new(
            "FutureActivePublishing",
            PublishStatus.Publishing,
            IsOrphaned: false,
            IsFuture: true,
            Expected: false),
        new(
            "FutureOrphanPublishing",
            PublishStatus.Publishing,
            IsOrphaned: true,
            IsFuture: true,
            Expected: false),
        new(
            "FutureActivePublished",
            PublishStatus.Published,
            IsOrphaned: false,
            IsFuture: true,
            Expected: false),
        new(
            "FutureOrphanPublished",
            PublishStatus.Published,
            IsOrphaned: true,
            IsFuture: true,
            Expected: false),
        new(
            "FutureActiveFailed",
            PublishStatus.Failed,
            IsOrphaned: false,
            IsFuture: true,
            Expected: true),
        new(
            "PastActiveFailed",
            PublishStatus.Failed,
            IsOrphaned: false,
            IsFuture: false,
            Expected: false),
        new(
            "FutureOrphanFailed",
            PublishStatus.Failed,
            IsOrphaned: true,
            IsFuture: true,
            Expected: false)
    };

    public static TheoryData<CanDeletePublicationCase> CanDeletePublicationCases => new()
    {
        new(
            "PublishedActiveFutureWithResource",
            PublishStatus.Published,
            IsOrphaned: false,
            HasExternalResourceId: true,
            IsFuture: true,
            Expected: true),
        new(
            "PublishedActiveFutureMissingResource",
            PublishStatus.Published,
            IsOrphaned: false,
            HasExternalResourceId: false,
            IsFuture: true,
            Expected: false),
        new(
            "PublishedActivePastWithResource",
            PublishStatus.Published,
            IsOrphaned: false,
            HasExternalResourceId: true,
            IsFuture: false,
            Expected: false),
        new(
            "PublishedOrphanFutureWithResource",
            PublishStatus.Published,
            IsOrphaned: true,
            HasExternalResourceId: true,
            IsFuture: true,
            Expected: false),
        new(
            "PublishingActiveFutureWithResource",
            PublishStatus.Publishing,
            IsOrphaned: false,
            HasExternalResourceId: true,
            IsFuture: true,
            Expected: false),
        new(
            "NotPublishedActiveFutureWithResource",
            PublishStatus.NotPublished,
            IsOrphaned: false,
            HasExternalResourceId: true,
            IsFuture: true,
            Expected: false),
        new(
            "FailedActiveFutureWithResource",
            PublishStatus.Failed,
            IsOrphaned: false,
            HasExternalResourceId: true,
            IsFuture: true,
            Expected: false)
    };

    public static TheoryData<CanPreviewPublishingContentCase>
        CanPreviewPublishingContentCases => new()
        {
            new(
                "ActiveNotPublishedWithoutSnapshot",
                PublishStatus.NotPublished,
                IsOrphaned: false,
                HasContentSnapshot: false,
                Expected: true),
            new(
                "ActivePublishingWithSnapshot",
                PublishStatus.Publishing,
                IsOrphaned: false,
                HasContentSnapshot: true,
                Expected: true),
            new(
                "OrphanPublishedWithSnapshot",
                PublishStatus.Published,
                IsOrphaned: true,
                HasContentSnapshot: true,
                Expected: true),
            new(
                "ActivePublishedWithoutSnapshot",
                PublishStatus.Published,
                IsOrphaned: false,
                HasContentSnapshot: false,
                Expected: false),
            new(
                "ActiveFailedWithSnapshot",
                PublishStatus.Failed,
                IsOrphaned: false,
                HasContentSnapshot: true,
                Expected: true),
            new(
                "OrphanFailedWithSnapshot",
                PublishStatus.Failed,
                IsOrphaned: true,
                HasContentSnapshot: true,
                Expected: false)
        };

    [Theory]
    [MemberData(nameof(CanPublishCases))]
    public void CanPublish_State_ReturnsExpected(CanPublishCase scenario)
    {
        var actual = PlatformActionPolicy.CanPublish(
            scenario.Status,
            scenario.IsOrphaned,
            scenario.IsFuture);

        Assert.Equal(scenario.Expected, actual);
    }

    [Theory]
    [MemberData(nameof(CanDeletePublicationCases))]
    public void CanDeletePublication_State_ReturnsExpected(
        CanDeletePublicationCase scenario)
    {
        var actual = PlatformActionPolicy.CanDeletePublication(
            scenario.Status,
            scenario.IsOrphaned,
            scenario.HasExternalResourceId,
            scenario.IsFuture);

        Assert.Equal(scenario.Expected, actual);
    }

    [Theory]
    [InlineData(PublishStatus.Publishing, true)]
    [InlineData(PublishStatus.NotPublished, false)]
    [InlineData(PublishStatus.Published, false)]
    [InlineData(PublishStatus.Failed, false)]
    public void BlocksPlatformDelete_Status_ReturnsExpected(
        PublishStatus status,
        bool expected)
    {
        Assert.Equal(expected, PlatformActionPolicy.BlocksPlatformDelete(status));
    }

    [Theory]
    [MemberData(nameof(CanPreviewPublishingContentCases))]
    public void CanPreviewPublishingContent_State_ReturnsExpected(
        CanPreviewPublishingContentCase scenario)
    {
        var actual = PlatformActionPolicy.CanPreviewPublishingContent(
            scenario.Status,
            scenario.IsOrphaned,
            scenario.HasContentSnapshot);

        Assert.Equal(scenario.Expected, actual);
    }

    public sealed record CanPublishCase(
        string Name,
        PublishStatus Status,
        bool IsOrphaned,
        bool IsFuture,
        bool Expected)
    {
        public override string ToString() => Name;
    }

    public sealed record CanDeletePublicationCase(
        string Name,
        PublishStatus Status,
        bool IsOrphaned,
        bool HasExternalResourceId,
        bool IsFuture,
        bool Expected)
    {
        public override string ToString() => Name;
    }

    public sealed record CanPreviewPublishingContentCase(
        string Name,
        PublishStatus Status,
        bool IsOrphaned,
        bool HasContentSnapshot,
        bool Expected)
    {
        public override string ToString() => Name;
    }
}
