using YTSkedy.Infrastructure.Settings;
using YTSkedy.Infrastructure.Test.TestSupport;
using YTSkedy.Scheduling.Application.Settings;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Infrastructure.Test.Settings;

public sealed class AzureCalendarEventDefaultsRepositoryTests
{
    [Fact]
    public async Task SaveAsync_Defaults_SubmitsBothRowsInOneTransaction()
    {
        var table = new ApplicationSettingsTableClient();
        var repository = new AzureCalendarEventDefaultsRepository(table);
        var fields = new EventTextFields(
            [new EventTextField("Episode", EventTextType.ShortText, 120)]);
        var startDefaults = new StartDefaults(
            DayOfWeek.Friday,
            new TimeOnly(14, 0),
            "America/Vancouver");

        await repository.SaveAsync(
            new CalendarEventDefaults(fields, startDefaults),
            CancellationToken.None);

        Assert.False(table.CreateIfNotExistsCalled);
        Assert.True(table.SubmitTransactionCalled);
        Assert.Equal(2, table.Entities.Count);

        var storedFields = await ((IEventTextFieldsReader)repository)
            .GetAsync(CancellationToken.None);
        var storedStartDefaults = await ((IStartDefaultsReader)repository)
            .GetAsync(CancellationToken.None);

        Assert.Equal(["text1"], storedFields.Fields.Select(field => field.FieldKey));
        Assert.Equal(["Episode"], storedFields.Fields.Select(field => field.Label));
        Assert.Equal(startDefaults, storedStartDefaults);
    }
}
