using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Test;

public class ListEventsHandlerTests
{
    private const string FirstId = "11111111111111111111111111111111";
    private const string SecondId = "22222222222222222222222222222222";
    private const string ThirdId = "33333333333333333333333333333333";
    private const string FourthId = "44444444444444444444444444444444";
    private const string FifthId = "55555555555555555555555555555555";
    private readonly Mock<ICalendarEventReader> _calendarEvents = new();
    private readonly Mock<IPlatformReader> _platforms = new();
    private readonly ListEventsHandler _handler;

    public ListEventsHandlerTests()
    {
        _handler = new ListEventsHandler(_calendarEvents.Object, _platforms.Object);
    }

    [Fact]
    public async Task HandleAsync_DefaultSort_OrdersByScheduledStartDescending()
    {
        CalendarEventReader(
        [
            CreateView(FirstId, ScheduledStartUtc(2026, 1, 1)),
            CreateView(ThirdId, ScheduledStartUtc(2026, 1, 3)),
            CreateView(SecondId, ScheduledStartUtc(2026, 1, 2))
        ]);
        PlatformReader(Set());
        var handler = CreateHandler();

        var result = await handler.HandleAsync(Query(), CancellationToken.None);

        Assert.Equal([ThirdId, SecondId, FirstId], Ids(result));
    }

    [Fact]
    public async Task HandleAsync_ScheduledStartAscending_OrdersByScheduledStartAscending()
    {
        CalendarEventReader(
        [
            CreateView(ThirdId, ScheduledStartUtc(2026, 1, 3)),
            CreateView(FirstId, ScheduledStartUtc(2026, 1, 1)),
            CreateView(SecondId, ScheduledStartUtc(2026, 1, 2))
        ]);
        PlatformReader(Set());
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            Query(
                sort: CalendarEventSortField.ScheduledStart,
                direction: SortDirection.Ascending),
            CancellationToken.None);

        Assert.Equal([FirstId, SecondId, ThirdId], Ids(result));
    }

    [Fact]
    public async Task HandleAsync_TimeZoneAscending_SortsByTimeZoneOrdinal()
    {
        CalendarEventReader(
        [
            CreateView(FirstId, ScheduledStartUtc(2026, 1, 1), timeZoneId: "Europe/London"),
            CreateView(SecondId, ScheduledStartUtc(2026, 1, 2), timeZoneId: "America/Vancouver"),
            CreateView(ThirdId, ScheduledStartUtc(2026, 1, 3), timeZoneId: "Asia/Tokyo")
        ]);
        PlatformReader(Set());
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            Query(sort: CalendarEventSortField.TimeZone, direction: SortDirection.Ascending),
            CancellationToken.None);

        Assert.Equal([SecondId, ThirdId, FirstId], Ids(result));
    }

    [Fact]
    public async Task HandleAsync_TitleAscending_SortsByDisplayTitleOrdinal()
    {
        CalendarEventReader(
        [
            CreateView(FirstId, ScheduledStartUtc(2026, 1, 1), title: "Charlie stream"),
            CreateView(SecondId, ScheduledStartUtc(2026, 1, 2), title: "Alpha stream"),
            CreateView(ThirdId, ScheduledStartUtc(2026, 1, 3), title: "Bravo stream")
        ]);
        PlatformReader(Set());
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            Query(sort: CalendarEventSortField.Title, direction: SortDirection.Ascending),
            CancellationToken.None);

        Assert.Equal([SecondId, ThirdId, FirstId], Ids(result));
    }

    [Fact]
    public async Task HandleAsync_TitleAscending_SortsByDisplayTitleFallback()
    {
        CalendarEventReader(
        [
            CreateView(
                FirstId,
                ScheduledStartUtc(2026, 1, 1),
                title: "Charlie stream",
                includeShortText: false),
            CreateView(
                SecondId,
                ScheduledStartUtc(2026, 1, 2),
                title: "Alpha stream",
                includeShortText: false),
            CreateView(
                ThirdId,
                ScheduledStartUtc(2026, 1, 3),
                title: "Bravo stream",
                includeShortText: false)
        ]);
        PlatformReader(Set());
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            Query(sort: CalendarEventSortField.Title, direction: SortDirection.Ascending),
            CancellationToken.None);

        Assert.Equal([SecondId, ThirdId, FirstId], Ids(result));
    }

    [Fact]
    public async Task HandleAsync_PublicationStatusAscending_SortsBeforePaging()
    {
        var reader = CalendarEventRecordReader(
            [
                Record(
                    CreateView(ThirdId, ScheduledStartUtc(2026, 1, 3)),
                    ["platform-a", "platform-b"]),
                Record(
                    CreateView(FirstId, ScheduledStartUtc(2026, 1, 1)),
                    []),
                Record(
                    CreateView(SecondId, ScheduledStartUtc(2026, 1, 2)),
                    ["platform-a"])
            ]);
        var platformReader = PlatformReader(Set("platform-a", "platform-b"));
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            Query(
                pageSize: 2,
                sort: CalendarEventSortField.PublicationStatus,
                direction: SortDirection.Ascending),
            CancellationToken.None);

        Assert.Equal([FirstId, SecondId], Ids(result));
        Assert.Equal(
            [PublishingStatus.NotPublished, PublishingStatus.PartiallyPublished],
            result.Items.Select(item => item.PublicationStatus));
    }

    [Fact]
    public async Task HandleAsync_FirstPage_ReturnsFirstSlice()
    {
        CalendarEventReader(FiveAscendingItems());
        PlatformReader(Set());
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            Query(page: 0, pageSize: 2, direction: SortDirection.Descending),
            CancellationToken.None);

        Assert.Equal([FifthId, FourthId], Ids(result));
        Assert.Equal(0, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(5, result.TotalCount);
    }

    [Fact]
    public async Task HandleAsync_MiddlePage_ReturnsMiddleSlice()
    {
        CalendarEventReader(FiveAscendingItems());
        PlatformReader(Set());
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            Query(page: 1, pageSize: 2, direction: SortDirection.Descending),
            CancellationToken.None);

        Assert.Equal([ThirdId, SecondId], Ids(result));
        Assert.Equal(5, result.TotalCount);
    }

    [Fact]
    public async Task HandleAsync_LastPartialPage_ReturnsRemainder()
    {
        CalendarEventReader(FiveAscendingItems());
        PlatformReader(Set());
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            Query(page: 2, pageSize: 2, direction: SortDirection.Descending),
            CancellationToken.None);

        Assert.Equal([FirstId], Ids(result));
        Assert.Equal(5, result.TotalCount);
    }

    [Fact]
    public async Task HandleAsync_PagePastEnd_ReturnsEmptyItemsWithTotalCount()
    {
        CalendarEventReader(FiveAscendingItems());
        PlatformReader(Set());
        var handler = CreateHandler();

        var result = await handler.HandleAsync(Query(page: 3, pageSize: 2), CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(3, result.Page);
    }

    [Fact]
    public async Task HandleAsync_NoCandidates_ReturnsEmptyPage()
    {
        CalendarEventReader([]);
        PlatformReader(Set());
        var handler = CreateHandler();

        var result = await handler.HandleAsync(Query(), CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task HandleAsync_EchoesSortAndDirection()
    {
        CalendarEventReader([]);
        PlatformReader(Set());
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            Query(sort: CalendarEventSortField.TimeZone, direction: SortDirection.Ascending),
            CancellationToken.None);

        Assert.Equal(CalendarEventSortField.TimeZone, result.Sort);
        Assert.Equal(SortDirection.Ascending, result.Direction);
    }

    [Fact]
    public async Task HandleAsync_YearAndMonthProvided_PassesMonthCriteria()
    {
        var reader = CalendarEventReader([]);
        PlatformReader(Set());
        var handler = CreateHandler();

        await handler.HandleAsync(Query(year: 2026, month: 6), CancellationToken.None);

        reader.Verify(candidate => candidate.ListAsync(
            new CalendarEventMonthCriteria(2026, 6),
            CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_NoYearOrMonth_PassesNullCriteria()
    {
        var reader = CalendarEventReader([]);
        PlatformReader(Set());
        var handler = CreateHandler();

        await handler.HandleAsync(Query(), CancellationToken.None);

        reader.Verify(candidate => candidate.ListAsync(null, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_OnlyYear_PassesNullCriteria()
    {
        var reader = CalendarEventReader([]);
        PlatformReader(Set());
        var handler = CreateHandler();

        await handler.HandleAsync(Query(year: 2026), CancellationToken.None);

        reader.Verify(candidate => candidate.ListAsync(null, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_ForwardsCancellationToken()
    {
        var reader = CalendarEventReader([]);
        var platformReader = PlatformReader(Set());
        var handler = CreateHandler();
        using var cancellationTokenSource = new CancellationTokenSource();

        await handler.HandleAsync(Query(), cancellationTokenSource.Token);

        reader.Verify(candidate => candidate.ListAsync(
            null,
            cancellationTokenSource.Token));
        platformReader.Verify(candidate => candidate.ListIdsAsync(
            cancellationTokenSource.Token));
    }

    [Fact]
    public async Task HandleAsync_ListRecords_ComposesPublicationStatusFromActivePlatformIds()
    {
        var reader = CalendarEventRecordReader(
            [
                Record(
                    CreateView(FirstId, ScheduledStartUtc(2026, 1, 1)),
                    []),
                Record(
                    CreateView(SecondId, ScheduledStartUtc(2026, 1, 2)),
                    ["platform-a"]),
                Record(
                    CreateView(ThirdId, ScheduledStartUtc(2026, 1, 3)),
                    ["historical-platform", "platform-a", "platform-b"])
            ]);
        var platformReader = PlatformReader(Set("platform-a", "platform-b"));
        var handler = CreateHandler();

        var result = await handler.HandleAsync(Query(), CancellationToken.None);

        Assert.Equal(PublishingStatus.NotPublished, StatusFor(result, FirstId));
        Assert.Equal(PublishingStatus.PartiallyPublished, StatusFor(result, SecondId));
        Assert.Equal(PublishingStatus.FullyPublished, StatusFor(result, ThirdId));
        platformReader.Verify(candidate => candidate.ListIdsAsync(CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_NullQuery_Throws()
    {
        CalendarEventReader([]);
        PlatformReader(Set());
        var handler = CreateHandler();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!, CancellationToken.None));
    }

    private static CalendarEventListQuery Query(
        int page = 0,
        int pageSize = 10,
        CalendarEventSortField sort = CalendarEventSortField.ScheduledStart,
        SortDirection direction = SortDirection.Descending,
        int? year = null,
        int? month = null) =>
        new(page, pageSize, sort, direction, year, month);

    private ListEventsHandler CreateHandler() => _handler;

    private Mock<ICalendarEventReader> CalendarEventReader(
        IReadOnlyList<CalendarEventView> events) =>
        CalendarEventRecordReader(events.Select(calendarEvent => Record(calendarEvent, [])).ToArray());

    private Mock<ICalendarEventReader> CalendarEventRecordReader(
        IReadOnlyList<CalendarEventListRecord> records)
    {
        _calendarEvents
            .Setup(candidate => candidate.ListAsync(
                It.IsAny<CalendarEventMonthCriteria?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(records);
        return _calendarEvents;
    }

    private Mock<IPlatformReader> PlatformReader(IReadOnlySet<string> platformIds)
    {
        _platforms
            .Setup(candidate => candidate.ListIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(platformIds);
        return _platforms;
    }

    private static CalendarEventListRecord Record(
        CalendarEventView calendarEvent,
        IEnumerable<string> publishedPlatformIds) =>
        new(calendarEvent, new HashSet<string>(publishedPlatformIds, StringComparer.Ordinal));

    private static IReadOnlySet<string> Set(params string[] ids) =>
        new HashSet<string>(ids, StringComparer.Ordinal);

    private static IReadOnlyList<CalendarEventView> FiveAscendingItems() =>
    [
        CreateView(FirstId, ScheduledStartUtc(2026, 1, 1)),
        CreateView(SecondId, ScheduledStartUtc(2026, 1, 2)),
        CreateView(ThirdId, ScheduledStartUtc(2026, 1, 3)),
        CreateView(FourthId, ScheduledStartUtc(2026, 1, 4)),
        CreateView(FifthId, ScheduledStartUtc(2026, 1, 5))
    ];

    private static CalendarEventView CreateView(
        string calendarEventId,
        DateTimeOffset scheduledStartUtc,
        string timeZoneId = "America/Vancouver",
        string? title = null,
        bool includeShortText = true) =>
        new(
            calendarEventId,
            new ScheduledStart(scheduledStartUtc.UtcDateTime, timeZoneId),
            scheduledStartUtc,
            includeShortText
                ? EventTextSnapshot.Create(
                    EventTextFields.Default,
                    [
                        new EventTextValue("text1", title ?? $"Title {calendarEventId}"),
                        new EventTextValue("text2", "Description")
                    ])
                : EventTextSnapshot.Create(
                    new EventTextFields(
                        [new EventTextField("Body", EventTextType.LongText, 2500)]),
                    [new EventTextValue("text1", title ?? $"Title {calendarEventId}")]));

    private static DateTimeOffset ScheduledStartUtc(int year, int month, int day) =>
        new(year, month, day, 0, 0, 0, TimeSpan.Zero);

    private static string[] Ids(CalendarEventListPage page) =>
        page.Items.Select(item => item.Event.CalendarEventId).ToArray();

    private static PublishingStatus StatusFor(
        CalendarEventListPage page,
        string calendarEventId) =>
        page.Items.Single(item => string.Equals(
            item.Event.CalendarEventId,
            calendarEventId,
            StringComparison.Ordinal)).PublicationStatus;

}
