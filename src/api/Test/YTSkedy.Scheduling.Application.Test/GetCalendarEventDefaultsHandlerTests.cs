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
        var fieldsReader = new Mock<IEventTextFieldsReader>();
        fieldsReader
            .Setup(reader => reader.GetAsync(CancellationToken.None))
            .ReturnsAsync(fields);
        var startDefaultsReader = new Mock<IStartDefaultsReader>();
        startDefaultsReader
            .Setup(reader => reader.GetAsync(CancellationToken.None))
            .ReturnsAsync(startDefaults);
        var handler = new GetCalendarEventDefaultsHandler(
            fieldsReader.Object,
            startDefaultsReader.Object);

        var result = await handler.HandleAsync(CancellationToken.None);

        Assert.Same(fields, result.EventTextFields);
        Assert.Equal(startDefaults, result.StartDefaults);
        fieldsReader.Verify(reader => reader.GetAsync(CancellationToken.None));
        startDefaultsReader.Verify(reader => reader.GetAsync(CancellationToken.None));
    }
}
