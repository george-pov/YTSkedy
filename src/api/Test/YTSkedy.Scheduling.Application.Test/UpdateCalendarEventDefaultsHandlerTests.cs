using YTSkedy.Scheduling.Application.Settings;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Test;

public sealed class UpdateCalendarEventDefaultsHandlerTests
{
    [Fact]
    public async Task HandleAsync_Replacement_SavesAndReturnsNormalizedSettings()
    {
        var modifier = new FakeDefaultsModifier();
        var handler = new UpdateCalendarEventDefaultsHandler(modifier);
        var command = new UpdateCalendarEventDefaultsCommand(
            [
                new EventTextField(" Title ", EventTextType.ShortText, 80),
                new EventTextField(" Description ", EventTextType.LongText, 2000)
            ],
            DayOfWeek.Sunday,
            new TimeOnly(10, 30),
            "America/Vancouver");

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Same(result, modifier.Saved);
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
        var modifier = new FakeDefaultsModifier();
        var handler = new UpdateCalendarEventDefaultsHandler(modifier);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.HandleAsync(
                new UpdateCalendarEventDefaultsCommand(
                    [new EventTextField("Title", EventTextType.ShortText, 50)],
                    null,
                    null,
                    "Unknown/Zone"),
                CancellationToken.None));

        Assert.Null(modifier.Saved);
    }

    [Fact]
    public async Task HandleAsync_NullCommand_Throws()
    {
        var handler = new UpdateCalendarEventDefaultsHandler(new FakeDefaultsModifier());

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!, CancellationToken.None));
    }

    private sealed class FakeDefaultsModifier : ICalendarEventDefaultsModifier
    {
        public CalendarEventDefaults? Saved { get; private set; }

        public Task SaveAsync(
            CalendarEventDefaults defaults,
            CancellationToken cancellationToken)
        {
            Saved = defaults;
            return Task.CompletedTask;
        }
    }
}
