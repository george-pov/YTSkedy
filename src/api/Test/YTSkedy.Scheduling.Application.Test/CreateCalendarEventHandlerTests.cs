using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.Settings;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Test;

public class CreateCalendarEventHandlerTests
{
    private readonly Mock<IEventTextFieldsReader> _fields = new();
    private readonly Mock<ICalendarEventModifier> _modifier = new();
    private readonly CreateCalendarEventHandler _handler;

    public CreateCalendarEventHandlerTests()
    {
        _handler = new CreateCalendarEventHandler(_fields.Object, _modifier.Object);
    }

    [Fact]
    public async Task CreateCalendarEvent_ValidCommand_CreatesCalendarEventAndReturnsCalendarEventId()
    {
        CalendarEvent? createdCalendarEvent = null;
        DateTimeOffset? createdScheduledStartUtc = null;
        _modifier
            .Setup(candidate => candidate.CreateAsync(
                It.IsAny<CalendarEvent>(),
                It.IsAny<DateTimeOffset>(),
                CancellationToken.None))
            .Callback<CalendarEvent, DateTimeOffset, CancellationToken>(
                (calendarEvent, scheduledStartUtc, _) =>
                {
                    createdCalendarEvent = calendarEvent;
                    createdScheduledStartUtc = scheduledStartUtc;
                })
            .ReturnsAsync("1001");
        var settings = new EventTextFields(
            [
                new EventTextField("Title", EventTextType.ShortText, 50),
                new EventTextField("Description", EventTextType.LongText, 2500)
            ]);
        var reader = EventTextFieldsReader(settings);
        var start = new ScheduledStart(
            new DateTime(2026, 06, 05, 10, 00, 00),
            "America/Vancouver");
        var texts = new[]
        {
            new EventTextValue("text1", "English stream 1"),
            new EventTextValue("text2", "Description for stream 1 in English")
        };
        var command = new CreateCalendarEventCommand(start, texts);

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(CreateCalendarEventStatus.Created, result.Status);
        Assert.Equal("1001", result.CalendarEventId);
        Assert.NotNull(createdCalendarEvent);
        Assert.Equal(start, createdCalendarEvent!.Start);
        Assert.Equal(
            new DateTimeOffset(2026, 6, 5, 17, 0, 0, TimeSpan.Zero),
            createdScheduledStartUtc);
        Assert.Equal(["text1", "text2"], createdCalendarEvent.Text.Fields.Select(field => field.FieldKey));
        Assert.Equal(
            ["English stream 1", "Description for stream 1 in English"],
            createdCalendarEvent.Text.Values.Select(value => value.Value));
        reader.Verify(candidate => candidate.GetAsync(CancellationToken.None));
    }

    [Fact]
    public async Task CreateCalendarEvent_MissingRequiredText_ReturnsInvalidWithoutCreating()
    {
        EventTextFieldsReader(EventTextFields.Default);
        var command = new CreateCalendarEventCommand(
            new ScheduledStart(new DateTime(2026, 06, 05, 10, 00, 00), "America/Vancouver"),
            [new EventTextValue("text1", "English stream 1")]);

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(CreateCalendarEventStatus.Invalid, result.Status);
        Assert.False(string.IsNullOrWhiteSpace(result.ValidationError));
        Assert.Null(result.CalendarEventId);
        _modifier.Verify(candidate => candidate.CreateAsync(
            It.IsAny<CalendarEvent>(),
            It.IsAny<DateTimeOffset>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task HandleAsync_InvalidScheduledStart_ReturnsInvalid()
    {
        EventTextFieldsReader(EventTextFields.Default);
        var command = new CreateCalendarEventCommand(
            new ScheduledStart(new DateTime(2026, 3, 8, 2, 30, 0), "America/Vancouver"),
            [
                new EventTextValue("text1", "English stream 1"),
                new EventTextValue("text2", "Description for stream 1 in English")
            ]);

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(CreateCalendarEventStatus.Invalid, result.Status);
        Assert.Equal(
            "Scheduled start time does not exist in the specified time zone.",
            result.ValidationError);
        _modifier.Verify(candidate => candidate.CreateAsync(
            It.IsAny<CalendarEvent>(),
            It.IsAny<DateTimeOffset>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task HandleAsync_DuplicateScheduledStart_ReturnsDuplicateScheduledStartWithoutCreating()
    {
        var scheduledStartUtc = new DateTimeOffset(2026, 6, 5, 17, 0, 0, TimeSpan.Zero);
        _modifier
            .Setup(candidate => candidate.CreateAsync(
                It.IsAny<CalendarEvent>(),
                scheduledStartUtc,
                CancellationToken.None))
            .ThrowsAsync(new DuplicateScheduledStartException(scheduledStartUtc));
        EventTextFieldsReader(EventTextFields.Default);
        var command = new CreateCalendarEventCommand(
            new ScheduledStart(new DateTime(2026, 6, 5, 10, 0, 0), "America/Vancouver"),
            [
                new EventTextValue("text1", "English stream 1"),
                new EventTextValue("text2", "Description for stream 1 in English")
            ]);

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(CreateCalendarEventStatus.DuplicateScheduledStart, result.Status);
        Assert.Equal(scheduledStartUtc, result.ScheduledStartUtc);
    }

    private Mock<IEventTextFieldsReader> EventTextFieldsReader(EventTextFields fields)
    {
        _fields
            .Setup(candidate => candidate.GetAsync(CancellationToken.None))
            .ReturnsAsync(fields);
        return _fields;
    }

}
