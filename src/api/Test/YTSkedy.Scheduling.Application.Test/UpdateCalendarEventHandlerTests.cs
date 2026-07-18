using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Platforms.Publications;
using YTSkedy.Scheduling.Domain.CalendarEvents;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Test;

public class UpdateCalendarEventHandlerTests
{
    private const string CalendarEventId = "6f9619ff8b864fb5bdfd4f5c2f2f16a1";
    private static readonly DateTimeOffset StartUtc =
        new(2026, 06, 06, 17, 00, 00, TimeSpan.Zero);

    private static readonly EventTextValue[] Texts =
    [
        new("text1", "Updated title"),
        new("text2", "Updated description")
    ];
    private readonly Mock<ICalendarEventReader> _calendarEvents = new();
    private readonly Mock<IPlatformPublicationReader> _publications = new();
    private readonly Mock<ICalendarEventModifier> _modifier = new();
    private readonly UpdateCalendarEventHandler _handler;

    public UpdateCalendarEventHandlerTests()
    {
        _handler = new UpdateCalendarEventHandler(
            _calendarEvents.Object,
            new CalendarEventPublicationLock(_publications.Object),
            _modifier.Object);
    }

    [Fact]
    public async Task HandleAsync_ExistingEvent_UpdatesStartAndTextAndReturnsUpdated()
    {
        CalendarEvent? updatedCalendarEvent = null;
        DateTimeOffset? updatedScheduledStartUtc = null;
        _modifier
            .Setup(candidate => candidate.UpdateAsync(
                CalendarEventId,
                It.IsAny<CalendarEvent>(),
                It.IsAny<DateTimeOffset>(),
                CancellationToken.None))
            .Callback<string, CalendarEvent, DateTimeOffset, CancellationToken>(
                (_, calendarEvent, scheduledStartUtc, _) =>
                {
                    updatedCalendarEvent = calendarEvent;
                    updatedScheduledStartUtc = scheduledStartUtc;
                })
            .Returns(Task.FromResult(CalendarEventChangeResult.Applied));
        var handler = CreateHandler(CreateCalendarEventView());
        var updatedStart = ValidUpdatedStart();

        var result = await handler.HandleAsync(
            new UpdateCalendarEventCommand(CalendarEventId, updatedStart, Texts),
            CancellationToken.None);

        Assert.Equal(UpdateCalendarEventStatus.Updated, result.Status);
        Assert.NotNull(updatedCalendarEvent);
        Assert.Equal(updatedStart, updatedCalendarEvent!.Start);
        Assert.Equal(
            ["Updated title", "Updated description"],
            updatedCalendarEvent.Text.Values.Select(value => value.Value));
        Assert.Equal(
            new DateTimeOffset(2026, 07, 20, 08, 30, 00, TimeSpan.Zero),
            updatedScheduledStartUtc);
    }

    [Fact]
    public async Task HandleAsync_ExistingEvent_InvalidText_ReturnsInvalidWithoutUpdating()
    {
        var handler = CreateHandler(CreateCalendarEventView());

        var result = await handler.HandleAsync(
            new UpdateCalendarEventCommand(
                CalendarEventId,
                ValidUpdatedStart(),
                [new EventTextValue("text1", "Updated title")]),
            CancellationToken.None);

        Assert.Equal(UpdateCalendarEventStatus.Invalid, result.Status);
        Assert.False(string.IsNullOrWhiteSpace(result.ValidationError));
        VerifyNoUpdate();
    }

    [Fact]
    public async Task HandleAsync_InvalidScheduledStart_ReturnsInvalid()
    {
        var handler = CreateHandler(CreateCalendarEventView());

        var result = await handler.HandleAsync(
            new UpdateCalendarEventCommand(
                CalendarEventId,
                new ScheduledStart(new DateTime(2026, 3, 8, 2, 30, 0), "America/Vancouver"),
                Texts),
            CancellationToken.None);

        Assert.Equal(UpdateCalendarEventStatus.Invalid, result.Status);
        Assert.Equal(
            "Scheduled start time does not exist in the specified time zone.",
            result.ValidationError);
        VerifyNoUpdate();
    }

    [Fact]
    public async Task HandleAsync_MissingEvent_ReturnsNotFoundWithoutUpdating()
    {
        var handler = CreateHandler(calendarEvent: null);

        var result = await handler.HandleAsync(
            new UpdateCalendarEventCommand(CalendarEventId, ValidUpdatedStart(), Texts),
            CancellationToken.None);

        Assert.Equal(UpdateCalendarEventStatus.NotFound, result.Status);
        VerifyNoUpdate();
    }

    [Fact]
    public async Task HandleAsync_EventWithPlatformPublications_ReturnsConflictWithoutUpdating()
    {
        var handler = CreateHandler(
            CreateCalendarEventView(),
            canMutate: false);

        var result = await handler.HandleAsync(
            new UpdateCalendarEventCommand(CalendarEventId, ValidUpdatedStart(), Texts),
            CancellationToken.None);

        Assert.Equal(UpdateCalendarEventStatus.HasPlatformPublications, result.Status);
        VerifyNoUpdate();
    }

    [Fact]
    public async Task HandleAsync_RowVanishedBeforeWrite_ReturnsNotFound()
    {
        _modifier
            .Setup(candidate => candidate.UpdateAsync(
                CalendarEventId,
                It.IsAny<CalendarEvent>(),
                It.IsAny<DateTimeOffset>(),
                CancellationToken.None))
            .Returns(Task.FromResult(CalendarEventChangeResult.NotFound));
        var handler = CreateHandler(CreateCalendarEventView());

        var result = await handler.HandleAsync(
            new UpdateCalendarEventCommand(CalendarEventId, ValidUpdatedStart(), Texts),
            CancellationToken.None);

        Assert.Equal(UpdateCalendarEventStatus.NotFound, result.Status);
        _modifier.Verify(candidate => candidate.UpdateAsync(
            CalendarEventId,
            It.IsAny<CalendarEvent>(),
            It.IsAny<DateTimeOffset>(),
            CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_DuplicateScheduledStart_ReturnsDuplicateScheduledStartWithoutUpdating()
    {
        var scheduledStartUtc = new DateTimeOffset(2026, 7, 20, 8, 30, 0, TimeSpan.Zero);
        _modifier
            .Setup(candidate => candidate.UpdateAsync(
                CalendarEventId,
                It.IsAny<CalendarEvent>(),
                scheduledStartUtc,
                CancellationToken.None))
            .ThrowsAsync(new DuplicateScheduledStartException(scheduledStartUtc));
        var handler = CreateHandler(CreateCalendarEventView());

        var result = await handler.HandleAsync(
            new UpdateCalendarEventCommand(CalendarEventId, ValidUpdatedStart(), Texts),
            CancellationToken.None);

        Assert.Equal(UpdateCalendarEventStatus.DuplicateScheduledStart, result.Status);
        Assert.Equal(scheduledStartUtc, result.ScheduledStartUtc);
    }

    [Fact]
    public async Task HandleAsync_StaleEvent_ReturnsConflict()
    {
        _modifier
            .Setup(candidate => candidate.UpdateAsync(
                CalendarEventId,
                It.IsAny<CalendarEvent>(),
                It.IsAny<DateTimeOffset>(),
                CancellationToken.None))
            .Returns(Task.FromResult(CalendarEventChangeResult.Conflict));
        var handler = CreateHandler(CreateCalendarEventView());

        var result = await handler.HandleAsync(
            new UpdateCalendarEventCommand(CalendarEventId, ValidUpdatedStart(), Texts),
            CancellationToken.None);

        Assert.Equal(UpdateCalendarEventStatus.Conflict, result.Status);
    }

    [Fact]
    public async Task HandleAsync_NullCommand_Throws()
    {
        var handler = CreateHandler(CreateCalendarEventView());

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!, CancellationToken.None));
    }

    private UpdateCalendarEventHandler CreateHandler(
        CalendarEventView? calendarEvent,
        bool canMutate = true)
    {
        _calendarEvents
            .Setup(candidate => candidate.GetByIdAsync(
                CalendarEventId,
                CancellationToken.None))
            .ReturnsAsync(calendarEvent);
        _publications
            .Setup(candidate => candidate.HasAnyForEventAsync(
                CalendarEventId,
                CancellationToken.None))
            .ReturnsAsync(!canMutate);

        return _handler;
    }

    private static CalendarEventView CreateCalendarEventView() =>
        new(
            CalendarEventId,
            new ScheduledStart(StartUtc.UtcDateTime, "UTC"),
            StartUtc,
            EventTextSnapshot.Create(
                EventTextFields.Default,
                [
                    new EventTextValue("text1", "English title"),
                    new EventTextValue("text2", "English description")
                ]));

    private static ScheduledStart ValidUpdatedStart() =>
        new(new DateTime(2026, 07, 20, 09, 30, 00), "Europe/London");

    private void VerifyNoUpdate() =>
        _modifier.Verify(candidate => candidate.UpdateAsync(
            It.IsAny<string>(),
            It.IsAny<CalendarEvent>(),
            It.IsAny<DateTimeOffset>(),
            It.IsAny<CancellationToken>()), Times.Never());
}
