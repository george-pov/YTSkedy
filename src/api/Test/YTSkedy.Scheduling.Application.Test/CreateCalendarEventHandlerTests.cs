using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.Settings;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Test;

public class CreateCalendarEventHandlerTests
{
    [Fact]
    public async Task CreateCalendarEvent_ValidCommand_CreatesCalendarEventAndReturnsCalendarEventId()
    {
        var modifier = new FakeCalendarEventModifier("1001");
        var settings = new EventTextFields(
            [
                new EventTextField("ignored", "Title", EventTextType.ShortText, 50),
                new EventTextField("ignored", "Description", EventTextType.LongText, 2500)
            ]);
        var reader = new FakeEventTextFieldsReader(settings);
        var handler = new CreateCalendarEventHandler(reader, modifier);
        var start = new ScheduledStart(
            new DateTime(2026, 06, 05, 10, 00, 00),
            "America/Vancouver");
        var texts = new[]
        {
            new EventTextValue("text1", "English stream 1"),
            new EventTextValue("text2", "Description for stream 1 in English")
        };
        var command = new CreateCalendarEventCommand(start, texts);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal("1001", result.CalendarEventId);
        var createdCalendarEvent = modifier.CreatedCalendarEvent;
        Assert.NotNull(createdCalendarEvent);
        Assert.Equal(start, createdCalendarEvent!.Start);
        Assert.Equal(["text1", "text2"], createdCalendarEvent.Text.Fields.Select(field => field.FieldKey));
        Assert.Equal(
            ["English stream 1", "Description for stream 1 in English"],
            createdCalendarEvent.Text.Values.Select(value => value.Value));
        Assert.True(reader.WasCalled);
    }

    [Fact]
    public async Task CreateCalendarEvent_MissingRequiredText_Throws()
    {
        var modifier = new FakeCalendarEventModifier("1001");
        var handler = new CreateCalendarEventHandler(
            new FakeEventTextFieldsReader(EventTextFields.Default),
            modifier);
        var command = new CreateCalendarEventCommand(
            new ScheduledStart(new DateTime(2026, 06, 05, 10, 00, 00), "America/Vancouver"),
            [new EventTextValue("text1", "English stream 1")]);

        await Assert.ThrowsAsync<ArgumentException>(
            () => handler.HandleAsync(command, CancellationToken.None));

        Assert.Null(modifier.CreatedCalendarEvent);
    }

    private sealed class FakeEventTextFieldsReader(EventTextFields eventTextFields) :
        IEventTextFieldsReader
    {
        public bool WasCalled { get; private set; }

        public Task<EventTextFields> GetAsync(CancellationToken cancellationToken)
        {
            WasCalled = true;

            return Task.FromResult(eventTextFields);
        }
    }

    private sealed class FakeCalendarEventModifier(string calendarEventId) : ICalendarEventModifier
    {
        public CalendarEvent? CreatedCalendarEvent { get; private set; }

        public Task<string> CreateAsync(
            CalendarEvent calendarEvent,
            CancellationToken cancellationToken)
        {
            CreatedCalendarEvent = calendarEvent;

            return Task.FromResult(calendarEventId);
        }

        public Task<bool> UpdateTextAsync(
            string calendarEventId,
            EventTextSnapshot text,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task DeleteAsync(
            string calendarEventId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
