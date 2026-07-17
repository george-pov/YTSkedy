using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.Settings;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Test;

public class CreateCalendarEventHandlerTests
{
    [Fact]
    public async Task CreateCalendarEvent_ValidCommand_CreatesCalendarEventAndReturnsCalendarEventId()
    {
        var modifier = new Mock<ICalendarEventModifier>();
        CalendarEvent? createdCalendarEvent = null;
        DateTimeOffset? createdScheduledStartUtc = null;
        modifier
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
        var handler = new CreateCalendarEventHandler(reader.Object, modifier.Object);
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
        var modifier = new Mock<ICalendarEventModifier>();
        var handler = new CreateCalendarEventHandler(
            EventTextFieldsReader(EventTextFields.Default).Object,
            modifier.Object);
        var command = new CreateCalendarEventCommand(
            new ScheduledStart(new DateTime(2026, 06, 05, 10, 00, 00), "America/Vancouver"),
            [new EventTextValue("text1", "English stream 1")]);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(CreateCalendarEventStatus.Invalid, result.Status);
        Assert.False(string.IsNullOrWhiteSpace(result.ValidationError));
        Assert.Null(result.CalendarEventId);
        modifier.Verify(candidate => candidate.CreateAsync(
            It.IsAny<CalendarEvent>(),
            It.IsAny<DateTimeOffset>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task HandleAsync_InvalidScheduledStart_ReturnsInvalid()
    {
        var modifier = new Mock<ICalendarEventModifier>();
        var handler = new CreateCalendarEventHandler(
            EventTextFieldsReader(EventTextFields.Default).Object,
            modifier.Object);
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
        modifier.Verify(candidate => candidate.CreateAsync(
            It.IsAny<CalendarEvent>(),
            It.IsAny<DateTimeOffset>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task HandleAsync_DuplicateScheduledStart_ReturnsDuplicateScheduledStartWithoutCreating()
    {
        var scheduledStartUtc = new DateTimeOffset(2026, 6, 5, 17, 0, 0, TimeSpan.Zero);
        var modifier = new Mock<ICalendarEventModifier>();
        modifier
            .Setup(candidate => candidate.CreateAsync(
                It.IsAny<CalendarEvent>(),
                scheduledStartUtc,
                CancellationToken.None))
            .ThrowsAsync(new DuplicateScheduledStartException(scheduledStartUtc));
        var handler = new CreateCalendarEventHandler(
            EventTextFieldsReader(EventTextFields.Default).Object,
            modifier.Object);
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

    private static Mock<IEventTextFieldsReader> EventTextFieldsReader(EventTextFields fields)
    {
        var reader = new Mock<IEventTextFieldsReader>();
        reader
            .Setup(candidate => candidate.GetAsync(CancellationToken.None))
            .ReturnsAsync(fields);
        return reader;
    }

}
