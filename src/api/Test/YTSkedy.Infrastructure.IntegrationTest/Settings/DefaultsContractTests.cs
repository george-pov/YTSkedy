using Azure.Data.Tables;
using YTSkedy.Infrastructure.IntegrationTest.TestSupport;
using YTSkedy.Infrastructure.Settings;
using YTSkedy.Scheduling.Application.Settings;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Infrastructure.IntegrationTest.Settings;

[Collection(AzuriteTableCollection.Name)]
public sealed class DefaultsContractTests(AzuriteTableFixture fixture)
{
    [AzuriteFact]
    public async Task DefaultsRepository_DefaultAndTransactionalSave_WorkAgainstAzurite()
    {
        var missingRepository = new AzureCalendarEventDefaultsRepository(
            fixture.MissingTable("MissingSettings"));
        Assert.Equal(
            EventTextFields.Default,
            await ((IEventTextFieldsReader)missingRepository)
                .GetAsync(CancellationToken.None));
        Assert.Equal(
            StartDefaults.Empty,
            await ((IStartDefaultsReader)missingRepository)
                .GetAsync(CancellationToken.None));

        var table = await fixture.CreateTableAsync("Settings");
        var repository = new AzureCalendarEventDefaultsRepository(table);
        var fields = new EventTextFields(
            [new EventTextField("Episode", EventTextType.ShortText, 100)]);
        var starts = new StartDefaults(
            DayOfWeek.Friday,
            new TimeOnly(14, 0),
            "America/Vancouver");

        await repository.SaveAsync(
            new CalendarEventDefaults(fields, starts),
            CancellationToken.None);

        var savedFields = await ((IEventTextFieldsReader)repository)
            .GetAsync(CancellationToken.None);
        var savedStarts = await ((IStartDefaultsReader)repository)
            .GetAsync(CancellationToken.None);

        Assert.Equal(["Episode"], savedFields.Fields.Select(field => field.Label));
        Assert.Equal(starts, savedStarts);
        Assert.Equal(2, table.Query<TableEntity>().Count());
    }
}
