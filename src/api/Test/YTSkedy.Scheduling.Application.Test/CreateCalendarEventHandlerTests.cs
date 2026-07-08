using YTSkedy.Scheduling.Application.CalendarEvents;
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
                new EventTextField("Title", EventTextType.ShortText, 50),
                new EventTextField("Description", EventTextType.LongText, 2500)
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

        Assert.Equal(CreateCalendarEventStatus.Created, result.Status);
        Assert.Equal("1001", result.CalendarEventId);
        var createdCalendarEvent = modifier.CreatedCalendarEvent;
        Assert.NotNull(createdCalendarEvent);
        Assert.Equal(start, createdCalendarEvent!.Start);
        Assert.Equal(
            new DateTimeOffset(2026, 6, 5, 17, 0, 0, TimeSpan.Zero),
            modifier.CreatedScheduledStartUtc);
        Assert.Equal(["text1", "text2"], createdCalendarEvent.Text.Fields.Select(field => field.FieldKey));
        Assert.Equal(
            ["English stream 1", "Description for stream 1 in English"],
            createdCalendarEvent.Text.Values.Select(value => value.Value));
        Assert.True(reader.WasCalled);
    }

    [Fact]
    public async Task CreateCalendarEvent_MissingRequiredText_ReturnsInvalidWithoutCreating()
    {
        var modifier = new FakeCalendarEventModifier("1001");
        var handler = new CreateCalendarEventHandler(
            new FakeEventTextFieldsReader(EventTextFields.Default),
            modifier);
        var command = new CreateCalendarEventCommand(
            new ScheduledStart(new DateTime(2026, 06, 05, 10, 00, 00), "America/Vancouver"),
            [new EventTextValue("text1", "English stream 1")]);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(CreateCalendarEventStatus.Invalid, result.Status);
        Assert.False(string.IsNullOrWhiteSpace(result.ValidationError));
        Assert.Null(result.CalendarEventId);
        Assert.Null(modifier.CreatedCalendarEvent);
    }

    [Fact]
    public async Task HandleAsync_InvalidScheduledStart_ReturnsInvalid()
    {
        var modifier = new FakeCalendarEventModifier("1001");
        var handler = new CreateCalendarEventHandler(
            new FakeEventTextFieldsReader(EventTextFields.Default),
            modifier);
        var command = new CreateCalendarEventCommand(
            new ScheduledStart(new DateTime(2026, 3, 8, 2, 30, 0), "America/Vancouver"),
            [
                new EventTextValue("text1", "English stream 1"),
                new EventTextValue("text2", "Description for stream 1 in English")
            ]);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(CreateCalendarEventStatus.Invalid, result.Status);
        Assert.Equal(
            "Scheduled start time does not exist in the specified time zone.",
            result.ValidationError);
        Assert.Null(modifier.CreatedCalendarEvent);
    }

    [Fact]
    public async Task HandleAsync_DuplicateScheduledStart_ReturnsDuplicateScheduledStartWithoutCreating()
    {
        var scheduledStartUtc = new DateTimeOffset(2026, 6, 5, 17, 0, 0, TimeSpan.Zero);
        var modifier = new FakeCalendarEventModifier("1001")
        {
            DuplicateScheduledStartUtc = scheduledStartUtc
        };
        var handler = new CreateCalendarEventHandler(
            new FakeEventTextFieldsReader(EventTextFields.Default),
            modifier);
        var command = new CreateCalendarEventCommand(
            new ScheduledStart(new DateTime(2026, 6, 5, 10, 0, 0), "America/Vancouver"),
            [
                new EventTextValue("text1", "English stream 1"),
                new EventTextValue("text2", "Description for stream 1 in English")
            ]);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(CreateCalendarEventStatus.DuplicateScheduledStart, result.Status);
        Assert.Equal(scheduledStartUtc, result.ScheduledStartUtc);
    }

    private sealed class FakeCalendarEventModifier(string calendarEventId) : ICalendarEventModifier
    {
        public CalendarEvent? CreatedCalendarEvent { get; private set; }

        public DateTimeOffset? CreatedScheduledStartUtc { get; private set; }

        public DateTimeOffset? DuplicateScheduledStartUtc { get; init; }

        public Task<string> CreateAsync(
            CalendarEvent calendarEvent,
            DateTimeOffset scheduledStartUtc,
            CancellationToken cancellationToken)
        {
            if (DuplicateScheduledStartUtc is { } duplicateScheduledStartUtc)
            {
                throw new DuplicateScheduledStartException(duplicateScheduledStartUtc);
            }

            CreatedCalendarEvent = calendarEvent;
            CreatedScheduledStartUtc = scheduledStartUtc;

            return Task.FromResult(calendarEventId);
        }

        public Task<bool> UpdateAsync(
            string calendarEventId,
            CalendarEvent calendarEvent,
            DateTimeOffset scheduledStartUtc,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task DeleteAsync(
            string calendarEventId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
