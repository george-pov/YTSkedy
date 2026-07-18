using YTSkedy.Infrastructure.IntegrationTest.TestSupport;
using YTSkedy.Infrastructure.Templates;
using YTSkedy.Scheduling.Application.Templates;
using YTSkedy.Scheduling.Domain.Templates;
using YTSkedy.Scheduling.TestSupport;
using YTSkedy.TestSupport;

namespace YTSkedy.Infrastructure.IntegrationTest.Templates;

[Collection(AzuriteTableCollection.Name)]
public sealed class TemplateContractTests(AzuriteTableFixture fixture)
{
    [AzuriteFact]
    public async Task TemplateRepository_CRUDPartitionsAndDuplicateNames_WorkAgainstAzurite()
    {
        var table = await fixture.CreateTableAsync("Templates");
        var repository = new AzureTemplateRepository(
            table,
            new FixedTimeProvider(SchedulingSampleTimes.Now));
        var created = await repository.CreateAsync(
            new Template("Title", TemplateType.YouTube, "{{ text1 }}"),
            CancellationToken.None);
        var duplicate = await repository.CreateAsync(
            new Template("Title", TemplateType.YouTube, "Other"),
            CancellationToken.None);
        var otherPartition = await repository.CreateAsync(
            new Template("Title", TemplateType.WordPress, "{{ text1 }}"),
            CancellationToken.None);

        var updated = await repository.UpdateAsync(
            TemplateType.YouTube,
            created.TemplateId!,
            "Updated",
            "{{ text2 }}",
            CancellationToken.None);

        Assert.Equal(CreateTemplateStatus.Created, created.Status);
        Assert.Equal(CreateTemplateStatus.NameAlreadyExists, duplicate.Status);
        Assert.Equal(CreateTemplateStatus.Created, otherPartition.Status);
        Assert.Equal(UpdateTemplateResult.Updated, updated);
        Assert.Equal(
            "{{ text2 }}",
            (await repository.GetAsync(
                TemplateType.YouTube,
                created.TemplateId!,
                CancellationToken.None))!.Content);
        Assert.Single(await repository.ListAsync(
            TemplateType.YouTube,
            CancellationToken.None));
        Assert.Equal(
            DeleteTemplateResult.Deleted,
            await repository.DeleteAsync(
                TemplateType.YouTube,
                created.TemplateId!,
                CancellationToken.None));
    }

    [AzuriteFact]
    public async Task TemplateRepository_MissingTable_ReadsAreEmpty()
    {
        var repository = new AzureTemplateRepository(
            fixture.MissingTable("MissingTemplates"),
            new FixedTimeProvider(SchedulingSampleTimes.Now));

        Assert.Empty(await repository.ListAsync(null, CancellationToken.None));
        Assert.Null(await repository.GetAsync(
            TemplateType.YouTube,
            SchedulingSampleIds.TitleTemplateId,
            CancellationToken.None));
    }
}
