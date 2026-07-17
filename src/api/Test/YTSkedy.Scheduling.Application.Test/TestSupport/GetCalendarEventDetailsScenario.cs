using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.CalendarEvents.Thumbnails;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Platforms.Publications;
using YTSkedy.Scheduling.Domain.CalendarEvents;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.Scheduling.TestSupport;
using YTSkedy.TestSupport;

namespace YTSkedy.Scheduling.Application.Test;

internal sealed class GetCalendarEventDetailsScenario
{
    public const string CalendarEventId = SchedulingSampleIds.CalendarEventId;

    public static readonly DateTimeOffset DefaultNow =
        new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    public CalendarEventView? CalendarEvent { get; set; } = ApplicationTestData.CalendarEvent(
        calendarEventId: CalendarEventId,
        start: new ScheduledStart(new DateTime(2026, 6, 15, 10, 0, 0), "America/Vancouver"),
        scheduledStartUtc: new DateTimeOffset(2026, 6, 15, 17, 0, 0, TimeSpan.Zero),
        text: EventTextSnapshot.Create(
            EventTextFields.Default,
            [
                new EventTextValue("text1", "English stream 1"),
                new EventTextValue("text2", "Event details")
            ]));

    public IReadOnlyList<PlatformView> Platforms { get; set; } = [];

    public IReadOnlyList<PlatformPublication> Publications { get; set; } = [];

    public Thumbnail? Thumbnail { get; set; }

    public DateTimeOffset Now { get; set; } = DefaultNow;

    public Mock<ICalendarEventReader> CalendarEventReader { get; } = new();

    public Task<CalendarEventDetailsView?> HandleAsync(
        string calendarEventId = CalendarEventId,
        CancellationToken cancellationToken = default) =>
        CreateHandler().HandleAsync(calendarEventId, cancellationToken);

    public GetCalendarEventDetailsHandler CreateHandler()
    {
        CalendarEventReader
            .Setup(candidate => candidate.GetByIdAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CalendarEvent);
        var platforms = new Mock<IPlatformReader>();
        platforms
            .Setup(candidate => candidate.ListAsync(
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Platforms);
        var publications = new Mock<IPlatformPublicationReader>();
        publications
            .Setup(candidate => candidate.ListByEventAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Publications);
        var thumbnails = new Mock<ICalendarEventThumbnailReader>();
        thumbnails
            .Setup(candidate => candidate.GetThumbnailAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Thumbnail);

        return new GetCalendarEventDetailsHandler(
            CalendarEventReader.Object,
            platforms.Object,
            publications.Object,
            new FixedTimeProvider(Now),
            thumbnails.Object,
            new PublicationExecutionSettings(
                TimeSpan.FromMinutes(2),
                TimeSpan.FromSeconds(15),
                TimeSpan.FromMinutes(5)));
    }
}
