using YTSkedy.Scheduling.Application.Settings;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Test;

public sealed class GetCalendarEventDefaultsHandlerTests
{
    [Fact]
    public async Task HandleAsync_CurrentSettings_ReturnsBothReaderValues()
    {
        var fields = new EventTextFields(
            [new EventTextField("Episode title", EventTextType.ShortText, 100)]);
        var startDefaults = new StartDefaults(
            DayOfWeek.Friday,
            new TimeOnly(9, 15),
            "UTC");
        var fieldsReader = new FakeEventTextFieldsReader(fields);
        var startDefaultsReader = new FakeStartDefaultsStore(startDefaults);
        var handler = new GetCalendarEventDefaultsHandler(
            fieldsReader,
            startDefaultsReader);

        var result = await handler.HandleAsync(CancellationToken.None);

        Assert.Same(fields, result.EventTextFields);
        Assert.Equal(startDefaults, result.StartDefaults);
        Assert.True(fieldsReader.WasCalled);
        Assert.Equal(1, startDefaultsReader.GetCallCount);
    }
}
