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

    [Fact]
    public async Task HandleAsync_ExistingEvent_UpdatesStartAndTextAndReturnsUpdated()
    {
        CalendarEvent? updatedCalendarEvent = null;
        DateTimeOffset? updatedScheduledStartUtc = null;
        var modifier = new Mock<ICalendarEventModifier>();
        modifier
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
            .ReturnsAsync(true);
        var handler = CreateHandler(CreateCalendarEventView(), modifier);
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
        var modifier = new Mock<ICalendarEventModifier>();
        var handler = CreateHandler(CreateCalendarEventView(), modifier);

        var result = await handler.HandleAsync(
            new UpdateCalendarEventCommand(
                CalendarEventId,
                ValidUpdatedStart(),
                [new EventTextValue("text1", "Updated title")]),
            CancellationToken.None);

        Assert.Equal(UpdateCalendarEventStatus.Invalid, result.Status);
        Assert.False(string.IsNullOrWhiteSpace(result.ValidationError));
        VerifyNoUpdate(modifier);
    }

    [Fact]
    public async Task HandleAsync_InvalidScheduledStart_ReturnsInvalid()
    {
        var modifier = new Mock<ICalendarEventModifier>();
        var handler = CreateHandler(CreateCalendarEventView(), modifier);

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
        VerifyNoUpdate(modifier);
    }

    [Fact]
    public async Task HandleAsync_MissingEvent_ReturnsNotFoundWithoutUpdating()
    {
        var modifier = new Mock<ICalendarEventModifier>();
        var handler = CreateHandler(calendarEvent: null, modifier);

        var result = await handler.HandleAsync(
            new UpdateCalendarEventCommand(CalendarEventId, ValidUpdatedStart(), Texts),
            CancellationToken.None);

        Assert.Equal(UpdateCalendarEventStatus.NotFound, result.Status);
        VerifyNoUpdate(modifier);
    }

    [Fact]
    public async Task HandleAsync_EventWithPlatformPublications_ReturnsConflictWithoutUpdating()
    {
        var modifier = new Mock<ICalendarEventModifier>();
        var handler = CreateHandler(
            CreateCalendarEventView(),
            modifier,
            canMutate: false);

        var result = await handler.HandleAsync(
            new UpdateCalendarEventCommand(CalendarEventId, ValidUpdatedStart(), Texts),
            CancellationToken.None);

        Assert.Equal(UpdateCalendarEventStatus.HasPlatformPublications, result.Status);
        VerifyNoUpdate(modifier);
    }

    [Fact]
    public async Task HandleAsync_RowVanishedBeforeWrite_ReturnsNotFound()
    {
        var modifier = new Mock<ICalendarEventModifier>();
        modifier
            .Setup(candidate => candidate.UpdateAsync(
                CalendarEventId,
                It.IsAny<CalendarEvent>(),
                It.IsAny<DateTimeOffset>(),
                CancellationToken.None))
            .ReturnsAsync(false);
        var handler = CreateHandler(CreateCalendarEventView(), modifier);

        var result = await handler.HandleAsync(
            new UpdateCalendarEventCommand(CalendarEventId, ValidUpdatedStart(), Texts),
            CancellationToken.None);

        Assert.Equal(UpdateCalendarEventStatus.NotFound, result.Status);
        modifier.Verify(candidate => candidate.UpdateAsync(
            CalendarEventId,
            It.IsAny<CalendarEvent>(),
            It.IsAny<DateTimeOffset>(),
            CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_DuplicateScheduledStart_ReturnsDuplicateScheduledStartWithoutUpdating()
    {
        var scheduledStartUtc = new DateTimeOffset(2026, 7, 20, 8, 30, 0, TimeSpan.Zero);
        var modifier = new Mock<ICalendarEventModifier>();
        modifier
            .Setup(candidate => candidate.UpdateAsync(
                CalendarEventId,
                It.IsAny<CalendarEvent>(),
                scheduledStartUtc,
                CancellationToken.None))
            .ThrowsAsync(new DuplicateScheduledStartException(scheduledStartUtc));
        var handler = CreateHandler(CreateCalendarEventView(), modifier);

        var result = await handler.HandleAsync(
            new UpdateCalendarEventCommand(CalendarEventId, ValidUpdatedStart(), Texts),
            CancellationToken.None);

        Assert.Equal(UpdateCalendarEventStatus.DuplicateScheduledStart, result.Status);
        Assert.Equal(scheduledStartUtc, result.ScheduledStartUtc);
    }

    [Fact]
    public async Task HandleAsync_NullCommand_Throws()
    {
        var handler = CreateHandler(
            CreateCalendarEventView(),
            new Mock<ICalendarEventModifier>());

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!, CancellationToken.None));
    }

    private static UpdateCalendarEventHandler CreateHandler(
        CalendarEventView? calendarEvent,
        Mock<ICalendarEventModifier> modifier,
        bool canMutate = true)
    {
        var calendarEvents = new Mock<ICalendarEventReader>();
        calendarEvents
            .Setup(candidate => candidate.GetByIdAsync(
                CalendarEventId,
                CancellationToken.None))
            .ReturnsAsync(calendarEvent);
        var publications = new Mock<IPlatformPublicationReader>();
        publications
            .Setup(candidate => candidate.HasAnyForEventAsync(
                CalendarEventId,
                CancellationToken.None))
            .ReturnsAsync(!canMutate);

        return new UpdateCalendarEventHandler(
            calendarEvents.Object,
            new CalendarEventPublicationLock(publications.Object),
            modifier.Object);
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

    private static void VerifyNoUpdate(Mock<ICalendarEventModifier> modifier) =>
        modifier.Verify(candidate => candidate.UpdateAsync(
            It.IsAny<string>(),
            It.IsAny<CalendarEvent>(),
            It.IsAny<DateTimeOffset>(),
            It.IsAny<CancellationToken>()), Times.Never());
}
