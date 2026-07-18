using YTSkedy.Infrastructure.Templates;
using YTSkedy.Infrastructure.Test.TestSupport;
using YTSkedy.Scheduling.Application.Templates;
using YTSkedy.Scheduling.Domain.Templates;
using YTSkedy.TestSupport;

namespace YTSkedy.Infrastructure.Test.Templates;

public sealed class AzureTemplateRepositoryTests
{
    [Fact]
    public async Task CreateAsync_NewName_UsesNarrowUniquenessProjectionAndCreates()
    {
        var table = new TemplateTableClient();
        var repository = Repository(table);

        var result = await repository.CreateAsync(
            new Template("Title", TemplateType.YouTube, "{{ text1 }}"),
            CancellationToken.None);

        Assert.Equal(CreateTemplateStatus.Created, result.Status);
        Assert.Equal(
            [nameof(TemplateEntity.RowKey), nameof(TemplateEntity.Name)],
            table.LastQuerySelect);
        Assert.DoesNotContain(nameof(TemplateEntity.Content), table.LastQuerySelect!);
        Assert.False(table.CreateIfNotExistsCalled);
    }

    [Fact]
    public async Task UpdateAsync_ExistingTemplate_UsesPointReadAndUpdatesContent()
    {
        var table = new TemplateTableClient();
        var repository = Repository(table);
        var created = await repository.CreateAsync(
            new Template("Title", TemplateType.YouTube, "{{ text1 }}"),
            CancellationToken.None);

        var result = await repository.UpdateAsync(
            TemplateType.YouTube,
            created.TemplateId!,
            "Updated",
            "{{ text2 }}",
            CancellationToken.None);

        Assert.Equal(UpdateTemplateResult.Updated, result);
        Assert.Equal(
            "{{ text2 }}",
            (await repository.GetAsync(
                TemplateType.YouTube,
                created.TemplateId!,
                CancellationToken.None))!.Content);
    }

    [Fact]
    public async Task ListAndGetAsync_EmptyTable_ReturnEmptyResults()
    {
        var repository = Repository(new TemplateTableClient());

        Assert.Empty(await repository.ListAsync(null, CancellationToken.None));
        Assert.Null(await repository.GetAsync(
            TemplateType.YouTube,
            "missing",
            CancellationToken.None));
    }

    private static AzureTemplateRepository Repository(TemplateTableClient table) =>
        new(
            table,
            new FixedTimeProvider(
                new DateTimeOffset(2026, 7, 17, 12, 0, 0, TimeSpan.Zero)));
}
