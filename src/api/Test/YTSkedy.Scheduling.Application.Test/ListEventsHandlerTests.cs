using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Test;

public class ListEventsHandlerTests
{
    [Fact]
    public async Task HandleAsync_DefaultSort_OrdersByScheduledStartDescending()
    {
        var reader = new FakeCalendarEventReader(
        [
            CreateListItem("20260101T000000Z"),
            CreateListItem("20260103T000000Z"),
            CreateListItem("20260102T000000Z")
        ]);
        var handler = new ListEventsHandler(reader);

        var result = await handler.HandleAsync(Query(), CancellationToken.None);

        Assert.Equal(
            ["20260103T000000Z", "20260102T000000Z", "20260101T000000Z"],
            Ids(result));
    }

    [Fact]
    public async Task HandleAsync_ScheduledStartAscending_OrdersByCalendarEventIdAscending()
    {
        var reader = new FakeCalendarEventReader(
        [
            CreateListItem("20260103T000000Z"),
            CreateListItem("20260101T000000Z"),
            CreateListItem("20260102T000000Z")
        ]);
        var handler = new ListEventsHandler(reader);

        var result = await handler.HandleAsync(
            Query(
                sort: CalendarEventSortField.ScheduledStart,
                direction: SortDirection.Ascending),
            CancellationToken.None);

        Assert.Equal(
            ["20260101T000000Z", "20260102T000000Z", "20260103T000000Z"],
            Ids(result));
    }

    [Fact]
    public async Task HandleAsync_StatusAscending_SortsByStatusThenCalendarEventIdAscending()
    {
        var reader = new FakeCalendarEventReader(
        [
            CreateListItem("20260101T000000Z", CalendarEventStatus.Published),
            CreateListItem("20260102T000000Z", CalendarEventStatus.Draft),
            CreateListItem("20260103T000000Z", CalendarEventStatus.Published),
            CreateListItem("20260104T000000Z", CalendarEventStatus.Draft)
        ]);
        var handler = new ListEventsHandler(reader);

        var result = await handler.HandleAsync(
            Query(sort: CalendarEventSortField.Status, direction: SortDirection.Ascending),
            CancellationToken.None);

        // Ordinal status order ascending: Draft before Published. Within each
        // status the calendar event id ascending is the deterministic tiebreak.
        Assert.Equal(
            [
                "20260102T000000Z",
                "20260104T000000Z",
                "20260101T000000Z",
                "20260103T000000Z"
            ],
            Ids(result));
    }

    [Fact]
    public async Task HandleAsync_StatusDescending_KeepsCalendarEventIdAscendingTiebreak()
    {
        var reader = new FakeCalendarEventReader(
        [
            CreateListItem("20260101T000000Z", CalendarEventStatus.Published),
            CreateListItem("20260102T000000Z", CalendarEventStatus.Draft),
            CreateListItem("20260103T000000Z", CalendarEventStatus.Published),
            CreateListItem("20260104T000000Z", CalendarEventStatus.Draft)
        ]);
        var handler = new ListEventsHandler(reader);

        var result = await handler.HandleAsync(
            Query(sort: CalendarEventSortField.Status, direction: SortDirection.Descending),
            CancellationToken.None);

        // Status descending: Published before Draft. The secondary key stays
        // calendar event id ascending even when the primary direction is
        // descending, so paging is stable.
        Assert.Equal(
            [
                "20260101T000000Z",
                "20260103T000000Z",
                "20260102T000000Z",
                "20260104T000000Z"
            ],
            Ids(result));
    }

    [Fact]
    public async Task HandleAsync_TimeZoneAscending_SortsByTimeZoneOrdinal()
    {
        var reader = new FakeCalendarEventReader(
        [
            CreateListItem("20260101T000000Z", timeZoneId: "Europe/London"),
            CreateListItem("20260102T000000Z", timeZoneId: "America/Vancouver"),
            CreateListItem("20260103T000000Z", timeZoneId: "Asia/Tokyo")
        ]);
        var handler = new ListEventsHandler(reader);

        var result = await handler.HandleAsync(
            Query(sort: CalendarEventSortField.TimeZone, direction: SortDirection.Ascending),
            CancellationToken.None);

        Assert.Equal(
            ["20260102T000000Z", "20260103T000000Z", "20260101T000000Z"],
            Ids(result));
    }

    [Fact]
    public async Task HandleAsync_TitleAscending_SortsByEnglishTitleOrdinal()
    {
        var reader = new FakeCalendarEventReader(
        [
            CreateListItem("20260101T000000Z", englishTitle: "Charlie stream"),
            CreateListItem("20260102T000000Z", englishTitle: "Alpha stream"),
            CreateListItem("20260103T000000Z", englishTitle: "Bravo stream")
        ]);
        var handler = new ListEventsHandler(reader);

        var result = await handler.HandleAsync(
            Query(sort: CalendarEventSortField.Title, direction: SortDirection.Ascending),
            CancellationToken.None);

        // English title order ascending: Alpha, Bravo, Charlie. This differs
        // from the calendar event id order, so it proves the English title is
        // the sort key rather than the id.
        Assert.Equal(
            ["20260102T000000Z", "20260103T000000Z", "20260101T000000Z"],
            Ids(result));
    }

    [Fact]
    public async Task HandleAsync_FirstPage_ReturnsFirstSlice()
    {
        var handler = new ListEventsHandler(new FakeCalendarEventReader(FiveAscendingItems()));

        var result = await handler.HandleAsync(
            Query(page: 0, pageSize: 2, direction: SortDirection.Descending),
            CancellationToken.None);

        Assert.Equal(["20260105T000000Z", "20260104T000000Z"], Ids(result));
        Assert.Equal(0, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(5, result.TotalCount);
    }

    [Fact]
    public async Task HandleAsync_MiddlePage_ReturnsMiddleSlice()
    {
        var handler = new ListEventsHandler(new FakeCalendarEventReader(FiveAscendingItems()));

        var result = await handler.HandleAsync(
            Query(page: 1, pageSize: 2, direction: SortDirection.Descending),
            CancellationToken.None);

        Assert.Equal(["20260103T000000Z", "20260102T000000Z"], Ids(result));
        Assert.Equal(5, result.TotalCount);
    }

    [Fact]
    public async Task HandleAsync_LastPartialPage_ReturnsRemainder()
    {
        var handler = new ListEventsHandler(new FakeCalendarEventReader(FiveAscendingItems()));

        var result = await handler.HandleAsync(
            Query(page: 2, pageSize: 2, direction: SortDirection.Descending),
            CancellationToken.None);

        Assert.Equal(["20260101T000000Z"], Ids(result));
        Assert.Equal(5, result.TotalCount);
    }

    [Fact]
    public async Task HandleAsync_PagePastEnd_ReturnsEmptyItemsWithTotalCount()
    {
        var handler = new ListEventsHandler(new FakeCalendarEventReader(FiveAscendingItems()));

        var result = await handler.HandleAsync(
            Query(page: 3, pageSize: 2),
            CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(3, result.Page);
    }

    [Fact]
    public async Task HandleAsync_NoCandidates_ReturnsEmptyPage()
    {
        var handler = new ListEventsHandler(new FakeCalendarEventReader([]));

        var result = await handler.HandleAsync(Query(), CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task HandleAsync_EchoesSortAndDirection()
    {
        var handler = new ListEventsHandler(new FakeCalendarEventReader([]));

        var result = await handler.HandleAsync(
            Query(sort: CalendarEventSortField.TimeZone, direction: SortDirection.Ascending),
            CancellationToken.None);

        Assert.Equal(CalendarEventSortField.TimeZone, result.Sort);
        Assert.Equal(SortDirection.Ascending, result.Direction);
    }

    [Fact]
    public async Task HandleAsync_YearAndMonthProvided_PassesMonthCriteria()
    {
        var reader = new FakeCalendarEventReader([]);
        var handler = new ListEventsHandler(reader);

        await handler.HandleAsync(Query(year: 2026, month: 6), CancellationToken.None);

        Assert.True(reader.ListCalled);
        Assert.Equal(new CalendarEventMonthCriteria(2026, 6), reader.Criteria);
    }

    [Fact]
    public async Task HandleAsync_NoYearOrMonth_PassesNullCriteria()
    {
        var reader = new FakeCalendarEventReader([]);
        var handler = new ListEventsHandler(reader);

        await handler.HandleAsync(Query(), CancellationToken.None);

        Assert.True(reader.ListCalled);
        Assert.Null(reader.Criteria);
    }

    [Fact]
    public async Task HandleAsync_OnlyYear_PassesNullCriteria()
    {
        var reader = new FakeCalendarEventReader([]);
        var handler = new ListEventsHandler(reader);

        await handler.HandleAsync(Query(year: 2026), CancellationToken.None);

        Assert.Null(reader.Criteria);
    }

    [Fact]
    public async Task HandleAsync_ForwardsCancellationToken()
    {
        var reader = new FakeCalendarEventReader([]);
        var handler = new ListEventsHandler(reader);
        using var cancellationTokenSource = new CancellationTokenSource();

        await handler.HandleAsync(Query(), cancellationTokenSource.Token);

        Assert.Equal(cancellationTokenSource.Token, reader.CancellationToken);
    }

    [Fact]
    public async Task HandleAsync_NullQuery_Throws()
    {
        var handler = new ListEventsHandler(new FakeCalendarEventReader([]));

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

    private static IReadOnlyList<CalendarEventListItem> FiveAscendingItems() =>
    [
        CreateListItem("20260101T000000Z"),
        CreateListItem("20260102T000000Z"),
        CreateListItem("20260103T000000Z"),
        CreateListItem("20260104T000000Z"),
        CreateListItem("20260105T000000Z")
    ];

    private static CalendarEventListItem CreateListItem(
        string calendarEventId,
        CalendarEventStatus status = CalendarEventStatus.Draft,
        string timeZoneId = "America/Vancouver",
        string? englishTitle = null) =>
        new(
            calendarEventId,
            new ScheduledStart(new DateTime(2026, 1, 1, 0, 0, 0), timeZoneId),
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            [new LocalizedDescription("en", englishTitle ?? $"Title {calendarEventId}", null)],
            status);

    private static string[] Ids(CalendarEventListPage page) =>
        page.Items.Select(item => item.CalendarEventId).ToArray();

    private sealed class FakeCalendarEventReader(
        IReadOnlyList<CalendarEventListItem> items) : ICalendarEventReader
    {
        public bool ListCalled { get; private set; }

        public CalendarEventMonthCriteria? Criteria { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public Task<IReadOnlyList<CalendarEventListItem>> ListAsync(
            CalendarEventMonthCriteria? criteria,
            CancellationToken cancellationToken)
        {
            ListCalled = true;
            Criteria = criteria;
            CancellationToken = cancellationToken;

            return Task.FromResult(items);
        }

        public Task<CalendarEventDetail?> GetByIdAsync(
            string calendarEventId,
            CancellationToken cancellationToken) =>
            Task.FromResult<CalendarEventDetail?>(null);

        public Task<CalendarEventListItem?> GetListItemByIdAsync(
            string calendarEventId,
            CancellationToken cancellationToken) =>
            Task.FromResult<CalendarEventListItem?>(null);
    }
}
