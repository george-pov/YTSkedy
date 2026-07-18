using YTSkedy.Infrastructure.IntegrationTest.TestSupport;
using YTSkedy.Infrastructure.Platforms;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.Scheduling.TestSupport;
using YTSkedy.TestSupport;

namespace YTSkedy.Infrastructure.IntegrationTest.Platforms;

[Collection(AzuriteTableCollection.Name)]
public sealed class PlatformContractTests(AzuriteTableFixture fixture)
{
    [AzuriteFact]
    public async Task PlatformRepository_CRUDIdsAndReferenceKeyUniqueness_WorkAgainstAzurite()
    {
        var table = await fixture.CreateTableAsync("Platforms");
        var repository = new AzurePlatformRepository(
            table,
            new FixedTimeProvider(SchedulingSampleTimes.Now));
        var created = await repository.CreateAsync(
            Platform("Main", "YouTube-1"),
            CancellationToken.None);
        var duplicate = await repository.CreateAsync(
            Platform("Backup", "youtube-1"),
            CancellationToken.None);

        Assert.Equal(CreatePlatformStatus.Created, created.Status);
        Assert.Equal(CreatePlatformStatus.ReferenceKeyAlreadyExists, duplicate.Status);
        Assert.Contains(created.PlatformId!, await repository.ListIdsAsync(
            CancellationToken.None));
        Assert.Equal(
            "Main",
            (await repository.GetAsync(
                created.PlatformId!,
                CancellationToken.None))!.Name);
        Assert.Equal(
            DeletePlatformResult.Deleted,
            await repository.DeleteAsync(
                created.PlatformId!,
                CancellationToken.None));
    }

    [AzuriteFact]
    public async Task PlatformRepository_MissingTable_ReadsAreEmpty()
    {
        var repository = new AzurePlatformRepository(
            fixture.MissingTable("MissingPlatforms"),
            new FixedTimeProvider(SchedulingSampleTimes.Now));

        Assert.Empty(await repository.ListAsync(null, CancellationToken.None));
        Assert.Empty(await repository.ListIdsAsync(CancellationToken.None));
        Assert.Null(await repository.GetAsync(
            SchedulingSampleIds.PlatformId,
            CancellationToken.None));
    }

    private static Platform Platform(string name, string referenceKey) =>
        new(
            name,
            PlatformType.YouTube,
            SchedulingSamples.YouTubeSettings(),
            SchedulingSamples.PublishingContent(),
            referenceKey);
}
