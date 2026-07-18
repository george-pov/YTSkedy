using YTSkedy.Infrastructure.IntegrationTest.TestSupport;
using YTSkedy.Infrastructure.Platforms;
using YTSkedy.Scheduling.Application.Platforms.Publications;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.Scheduling.TestSupport;
using YTSkedy.TestSupport;

namespace YTSkedy.Infrastructure.IntegrationTest.Platforms;

[Collection(AzuriteTableCollection.Name)]
public sealed class PublicationContractTests(AzuriteTableFixture fixture)
{
    [AzuriteFact]
    public async Task PublicationRepository_ReadGuardsAndConditionalMutations_WorkAgainstAzurite()
    {
        var table = await fixture.CreateTableAsync("Publications");
        var repository = new AzurePlatformPublicationRepository(
            table,
            new FixedTimeProvider(SchedulingSampleTimes.Now));
        var attempt = new PlatformPublicationAttempt(
            SchedulingSampleIds.CalendarEventId,
            SchedulingSampleIds.PlatformId,
            "Main YouTube channel",
            PlatformType.YouTube,
            SchedulingSamples.YouTubeSettings(),
            new ContentSnapshot("Title", "Description"));

        var started = await repository.StartPublishingAsync(
            attempt,
            CancellationToken.None);
        var duplicate = await repository.StartPublishingAsync(
            attempt,
            CancellationToken.None);

        Assert.Equal(StartPublicationResult.Started, started);
        Assert.Equal(StartPublicationResult.Conflict, duplicate);
        Assert.True(await repository.HasAnyForEventAsync(
            SchedulingSampleIds.CalendarEventId,
            CancellationToken.None));
        Assert.True(await repository.HasPublishingByPlatformAsync(
            SchedulingSampleIds.PlatformId,
            CancellationToken.None));
        Assert.Single(await repository.ListByEventAsync(
            SchedulingSampleIds.CalendarEventId,
            CancellationToken.None));
        Assert.NotNull(await repository.GetAsync(
            SchedulingSampleIds.CalendarEventId,
            SchedulingSampleIds.PlatformId,
            CancellationToken.None));

        Assert.NotNull(await repository.MarkPublishedAsync(
            SchedulingSampleIds.CalendarEventId,
            SchedulingSampleIds.PlatformId,
            SchedulingSampleIds.YouTubeBroadcastId,
            CancellationToken.None));
        Assert.False(await repository.HasPublishingByPlatformAsync(
            SchedulingSampleIds.PlatformId,
            CancellationToken.None));
    }

    [AzuriteFact]
    public async Task PublicationRepository_MissingTable_ReadsAreEmpty()
    {
        var repository = new AzurePlatformPublicationRepository(
            fixture.MissingTable("MissingPublications"),
            new FixedTimeProvider(SchedulingSampleTimes.Now));

        Assert.Empty(await repository.ListByEventAsync(
            SchedulingSampleIds.CalendarEventId,
            CancellationToken.None));
        Assert.False(await repository.HasAnyForEventAsync(
            SchedulingSampleIds.CalendarEventId,
            CancellationToken.None));
        Assert.False(await repository.HasPublishingByPlatformAsync(
            SchedulingSampleIds.PlatformId,
            CancellationToken.None));
        Assert.Null(await repository.GetAsync(
            SchedulingSampleIds.CalendarEventId,
            SchedulingSampleIds.PlatformId,
            CancellationToken.None));
    }
}
