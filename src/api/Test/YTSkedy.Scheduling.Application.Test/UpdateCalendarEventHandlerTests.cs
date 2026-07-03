using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.Platforms;
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
        var modifier = new FakeCalendarEventModifier(updateResult: true);
        var handler = CreateHandler(CreateCalendarEventView(), modifier);
        var updatedStart = ValidUpdatedStart();

        var result = await handler.HandleAsync(
            new UpdateCalendarEventCommand(CalendarEventId, updatedStart, Texts),
            CancellationToken.None);

        Assert.Equal(UpdateCalendarEventStatus.Updated, result.Status);
        Assert.Equal(1, modifier.UpdateCallCount);
        Assert.Equal(CalendarEventId, modifier.UpdatedCalendarEventId);
        Assert.NotNull(modifier.UpdatedCalendarEvent);
        Assert.Equal(updatedStart, modifier.UpdatedCalendarEvent!.Start);
        Assert.Equal(
            ["Updated title", "Updated description"],
            modifier.UpdatedCalendarEvent.Text.Values.Select(value => value.Value));
        Assert.Equal(
            new DateTimeOffset(2026, 07, 20, 08, 30, 00, TimeSpan.Zero),
            modifier.UpdatedScheduledStartUtc);
    }

    [Fact]
    public async Task HandleAsync_ExistingEvent_InvalidText_ReturnsInvalidWithoutUpdating()
    {
        var modifier = new FakeCalendarEventModifier(updateResult: true);
        var handler = CreateHandler(CreateCalendarEventView(), modifier);

        var result = await handler.HandleAsync(
            new UpdateCalendarEventCommand(
                CalendarEventId,
                ValidUpdatedStart(),
                [new EventTextValue("text1", "Updated title")]),
            CancellationToken.None);

        Assert.Equal(UpdateCalendarEventStatus.Invalid, result.Status);
        Assert.False(string.IsNullOrWhiteSpace(result.ValidationError));
        Assert.Equal(0, modifier.UpdateCallCount);
    }

    [Fact]
    public async Task HandleAsync_InvalidScheduledStart_ReturnsInvalid()
    {
        var modifier = new FakeCalendarEventModifier(updateResult: true);
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
        Assert.Equal(0, modifier.UpdateCallCount);
    }

    [Fact]
    public async Task HandleAsync_MissingEvent_ReturnsNotFoundWithoutUpdating()
    {
        var modifier = new FakeCalendarEventModifier(updateResult: true);
        var handler = CreateHandler(calendarEvent: null, modifier);

        var result = await handler.HandleAsync(
            new UpdateCalendarEventCommand(CalendarEventId, ValidUpdatedStart(), Texts),
            CancellationToken.None);

        Assert.Equal(UpdateCalendarEventStatus.NotFound, result.Status);
        Assert.Equal(0, modifier.UpdateCallCount);
    }

    [Fact]
    public async Task HandleAsync_EventWithPlatformPublications_ReturnsConflictWithoutUpdating()
    {
        var modifier = new FakeCalendarEventModifier(updateResult: true);
        var handler = CreateHandler(CreateCalendarEventView(), modifier, [Publication()]);

        var result = await handler.HandleAsync(
            new UpdateCalendarEventCommand(CalendarEventId, ValidUpdatedStart(), Texts),
            CancellationToken.None);

        Assert.Equal(UpdateCalendarEventStatus.HasPlatformPublications, result.Status);
        Assert.Equal(0, modifier.UpdateCallCount);
    }

    [Fact]
    public async Task HandleAsync_RowVanishedBeforeWrite_ReturnsNotFound()
    {
        var modifier = new FakeCalendarEventModifier(updateResult: false);
        var handler = CreateHandler(CreateCalendarEventView(), modifier);

        var result = await handler.HandleAsync(
            new UpdateCalendarEventCommand(CalendarEventId, ValidUpdatedStart(), Texts),
            CancellationToken.None);

        Assert.Equal(UpdateCalendarEventStatus.NotFound, result.Status);
        Assert.Equal(1, modifier.UpdateCallCount);
    }

    [Fact]
    public async Task HandleAsync_DuplicateScheduledStart_ReturnsDuplicateScheduledStartWithoutUpdating()
    {
        var scheduledStartUtc = new DateTimeOffset(2026, 7, 20, 8, 30, 0, TimeSpan.Zero);
        var modifier = new FakeCalendarEventModifier(updateResult: true)
        {
            DuplicateScheduledStartUtc = scheduledStartUtc
        };
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
            new FakeCalendarEventModifier(updateResult: true));

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!, CancellationToken.None));
    }

    private static UpdateCalendarEventHandler CreateHandler(
        CalendarEventView? calendarEvent,
        FakeCalendarEventModifier modifier,
        IReadOnlyList<PlatformPublication>? publications = null) =>
        new(
            new FakeCalendarEventReader(calendarEvent),
            new FakePlatformPublicationReader(publications ?? []),
            modifier);

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

    private static PlatformPublication Publication() =>
        new(
            CalendarEventId,
            "platform-1",
            "Main YouTube channel",
            PlatformType.YouTube,
            PublishStatus.Published,
            "external-1",
            StartUtc,
            null,
            StartUtc);

    private sealed class FakeCalendarEventReader(CalendarEventView? calendarEvent) : ICalendarEventReader
    {
        public Task<IReadOnlyList<CalendarEventView>> ListAsync(
            CalendarEventMonthCriteria? criteria,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<CalendarEventView?> GetByIdAsync(
            string calendarEventId,
            CancellationToken cancellationToken) =>
            Task.FromResult(calendarEvent);
    }

    private sealed class FakePlatformPublicationReader(
        IReadOnlyList<PlatformPublication> publications) : IPlatformPublicationReader
    {
        public Task<IReadOnlyList<PlatformPublication>> ListByEventAsync(
            string calendarEventId,
            CancellationToken cancellationToken) =>
            Task.FromResult(publications);

        public Task<PlatformPublication?> GetAsync(
            string calendarEventId,
            string platformId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<PlatformPublication>> ListPublishingByPlatformAsync(
            string platformId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeCalendarEventModifier(bool updateResult) : ICalendarEventModifier
    {
        public int UpdateCallCount { get; private set; }

        public string? UpdatedCalendarEventId { get; private set; }

        public CalendarEvent? UpdatedCalendarEvent { get; private set; }

        public DateTimeOffset? UpdatedScheduledStartUtc { get; private set; }

        public DateTimeOffset? DuplicateScheduledStartUtc { get; init; }

        public Task<string> CreateAsync(
            CalendarEvent calendarEvent,
            DateTimeOffset scheduledStartUtc,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> UpdateAsync(
            string calendarEventId,
            CalendarEvent calendarEvent,
            DateTimeOffset scheduledStartUtc,
            CancellationToken cancellationToken)
        {
            if (DuplicateScheduledStartUtc is { } duplicateScheduledStartUtc)
            {
                throw new DuplicateScheduledStartException(duplicateScheduledStartUtc);
            }

            UpdateCallCount++;
            UpdatedCalendarEventId = calendarEventId;
            UpdatedCalendarEvent = calendarEvent;
            UpdatedScheduledStartUtc = scheduledStartUtc;

            return Task.FromResult(updateResult);
        }

        public Task DeleteAsync(
            string calendarEventId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
