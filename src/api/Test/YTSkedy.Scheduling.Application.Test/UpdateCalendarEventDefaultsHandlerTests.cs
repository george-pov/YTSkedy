using YTSkedy.Scheduling.Application.Settings;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Test;

public sealed class UpdateCalendarEventDefaultsHandlerTests
{
    [Fact]
    public async Task HandleAsync_Replacement_SavesAndReturnsNormalizedSettings()
    {
        CalendarEventDefaults? saved = null;
        var modifier = new Mock<ICalendarEventDefaultsModifier>();
        modifier
            .Setup(candidate => candidate.SaveAsync(
                It.IsAny<CalendarEventDefaults>(),
                CancellationToken.None))
            .Callback<CalendarEventDefaults, CancellationToken>(
                (defaults, _) => saved = defaults)
            .Returns(Task.CompletedTask);
        var handler = new UpdateCalendarEventDefaultsHandler(modifier.Object);
        var command = new UpdateCalendarEventDefaultsCommand(
            [
                new EventTextField(" Title ", EventTextType.ShortText, 80),
                new EventTextField(" Description ", EventTextType.LongText, 2000)
            ],
            DayOfWeek.Sunday,
            new TimeOnly(10, 30),
            "America/Vancouver");

        var result = await handler.HandleAsync(command, CancellationToken.None);

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
        var modifier = new Mock<ICalendarEventDefaultsModifier>();
        var handler = new UpdateCalendarEventDefaultsHandler(modifier.Object);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.HandleAsync(
                new UpdateCalendarEventDefaultsCommand(
                    [new EventTextField("Title", EventTextType.ShortText, 50)],
                    null,
                    null,
                    "Unknown/Zone"),
                CancellationToken.None));

        modifier.Verify(candidate => candidate.SaveAsync(
            It.IsAny<CalendarEventDefaults>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task HandleAsync_NullCommand_Throws()
    {
        var modifier = new Mock<ICalendarEventDefaultsModifier>();
        var handler = new UpdateCalendarEventDefaultsHandler(modifier.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!, CancellationToken.None));

        modifier.Verify(candidate => candidate.SaveAsync(
            It.IsAny<CalendarEventDefaults>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }
}
