using YTSkedy.Scheduling.Application.Settings;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Test;

public sealed class UpdateCalendarEventDefaultsHandlerTests
{
    private readonly Mock<ICalendarEventDefaultsModifier> _modifier = new();
    private readonly UpdateCalendarEventDefaultsHandler _handler;

    public UpdateCalendarEventDefaultsHandlerTests()
    {
        _handler = new UpdateCalendarEventDefaultsHandler(_modifier.Object);
    }

    [Fact]
    public async Task HandleAsync_Replacement_SavesAndReturnsNormalizedSettings()
    {
        CalendarEventDefaults? saved = null;
        _modifier
            .Setup(candidate => candidate.SaveAsync(
                It.IsAny<CalendarEventDefaults>(),
                CancellationToken.None))
            .Callback<CalendarEventDefaults, CancellationToken>(
                (defaults, _) => saved = defaults)
            .Returns(Task.CompletedTask);
        var command = new UpdateCalendarEventDefaultsCommand(
            [
                new EventTextField(" Title ", EventTextType.ShortText, 80),
                new EventTextField(" Description ", EventTextType.LongText, 2000)
            ],
            DayOfWeek.Sunday,
            new TimeOnly(10, 30),
            "America/Vancouver");

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.Same(result, saved);
        Assert.Equal(
            ["text1", "text2"],
            result.EventTextFields.Fields.Select(field => field.FieldKey));
        Assert.Equal(
            ["Title", "Description"],
            result.EventTextFields.Fields.Select(field => field.Label));
        Assert.Equal(
            new StartDefaults(
                DayOfWeek.Sunday,
                new TimeOnly(10, 30),
                "America/Vancouver"),
            result.StartDefaults);
    }

    [Fact]
    public async Task HandleAsync_InvalidTimeZone_DoesNotSave()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _handler.HandleAsync(
                new UpdateCalendarEventDefaultsCommand(
                    [new EventTextField("Title", EventTextType.ShortText, 50)],
                    null,
                    null,
                    "Unknown/Zone"),
                CancellationToken.None));

        _modifier.Verify(candidate => candidate.SaveAsync(
            It.IsAny<CalendarEventDefaults>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task HandleAsync_NullCommand_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _handler.HandleAsync(null!, CancellationToken.None));

        _modifier.Verify(candidate => candidate.SaveAsync(
            It.IsAny<CalendarEventDefaults>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }
}
