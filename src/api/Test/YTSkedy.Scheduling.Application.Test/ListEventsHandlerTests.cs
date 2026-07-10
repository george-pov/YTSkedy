using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Test;

public class ListEventsHandlerTests
{
    private const string FirstId = "11111111111111111111111111111111";
    private const string SecondId = "22222222222222222222222222222222";
    private const string ThirdId = "33333333333333333333333333333333";
    private const string FourthId = "44444444444444444444444444444444";
    private const string FifthId = "55555555555555555555555555555555";

    [Fact]
    public async Task HandleAsync_DefaultSort_OrdersByScheduledStartDescending()
    {
        var reader = new FakeCalendarEventReader(
        [
            CreateView(FirstId, ScheduledStartUtc(2026, 1, 1)),
            CreateView(ThirdId, ScheduledStartUtc(2026, 1, 3)),
            CreateView(SecondId, ScheduledStartUtc(2026, 1, 2))
        ]);
        var handler = CreateHandler(reader);

        var result = await handler.HandleAsync(Query(), CancellationToken.None);

        Assert.Equal([ThirdId, SecondId, FirstId], Ids(result));
    }

    [Fact]
    public async Task HandleAsync_ScheduledStartAscending_OrdersByScheduledStartAscending()
    {
        var reader = new FakeCalendarEventReader(
        [
            CreateView(ThirdId, ScheduledStartUtc(2026, 1, 3)),
            CreateView(FirstId, ScheduledStartUtc(2026, 1, 1)),
            CreateView(SecondId, ScheduledStartUtc(2026, 1, 2))
        ]);
        var handler = CreateHandler(reader);

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
        var reader = new FakeCalendarEventReader(
        [
            CreateView(FirstId, ScheduledStartUtc(2026, 1, 1), timeZoneId: "Europe/London"),
            CreateView(SecondId, ScheduledStartUtc(2026, 1, 2), timeZoneId: "America/Vancouver"),
            CreateView(ThirdId, ScheduledStartUtc(2026, 1, 3), timeZoneId: "Asia/Tokyo")
        ]);
        var handler = CreateHandler(reader);

        var result = await handler.HandleAsync(
            Query(sort: CalendarEventSortField.TimeZone, direction: SortDirection.Ascending),
            CancellationToken.None);

        Assert.Equal([SecondId, ThirdId, FirstId], Ids(result));
    }

    [Fact]
    public async Task HandleAsync_TitleAscending_SortsByDisplayTitleOrdinal()
    {
        var reader = new FakeCalendarEventReader(
        [
            CreateView(FirstId, ScheduledStartUtc(2026, 1, 1), title: "Charlie stream"),
            CreateView(SecondId, ScheduledStartUtc(2026, 1, 2), title: "Alpha stream"),
            CreateView(ThirdId, ScheduledStartUtc(2026, 1, 3), title: "Bravo stream")
        ]);
        var handler = CreateHandler(reader);

        var result = await handler.HandleAsync(
            Query(sort: CalendarEventSortField.Title, direction: SortDirection.Ascending),
            CancellationToken.None);

        Assert.Equal([SecondId, ThirdId, FirstId], Ids(result));
    }

    [Fact]
    public async Task HandleAsync_TitleAscending_SortsByDisplayTitleFallback()
    {
        var reader = new FakeCalendarEventReader(
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
        var handler = CreateHandler(reader);

        var result = await handler.HandleAsync(
            Query(sort: CalendarEventSortField.Title, direction: SortDirection.Ascending),
            CancellationToken.None);

        Assert.Equal([SecondId, ThirdId, FirstId], Ids(result));
    }

    [Fact]
    public async Task HandleAsync_FirstPage_ReturnsFirstSlice()
    {
        var handler = CreateHandler(new FakeCalendarEventReader(FiveAscendingItems()));

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
        var handler = CreateHandler(new FakeCalendarEventReader(FiveAscendingItems()));

        var result = await handler.HandleAsync(
            Query(page: 1, pageSize: 2, direction: SortDirection.Descending),
            CancellationToken.None);

        Assert.Equal([ThirdId, SecondId], Ids(result));
        Assert.Equal(5, result.TotalCount);
    }

    [Fact]
    public async Task HandleAsync_LastPartialPage_ReturnsRemainder()
    {
        var handler = CreateHandler(new FakeCalendarEventReader(FiveAscendingItems()));

        var result = await handler.HandleAsync(
            Query(page: 2, pageSize: 2, direction: SortDirection.Descending),
            CancellationToken.None);

        Assert.Equal([FirstId], Ids(result));
        Assert.Equal(5, result.TotalCount);
    }

    [Fact]
    public async Task HandleAsync_PagePastEnd_ReturnsEmptyItemsWithTotalCount()
    {
        var handler = CreateHandler(new FakeCalendarEventReader(FiveAscendingItems()));

        var result = await handler.HandleAsync(Query(page: 3, pageSize: 2), CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(3, result.Page);
    }

    [Fact]
    public async Task HandleAsync_NoCandidates_ReturnsEmptyPage()
    {
        var handler = CreateHandler(new FakeCalendarEventReader([]));

        var result = await handler.HandleAsync(Query(), CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task HandleAsync_EchoesSortAndDirection()
    {
        var handler = CreateHandler(new FakeCalendarEventReader([]));

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
        var handler = CreateHandler(reader);

        await handler.HandleAsync(Query(year: 2026, month: 6), CancellationToken.None);

        Assert.True(reader.ListCalled);
        Assert.Equal(new CalendarEventMonthCriteria(2026, 6), reader.Criteria);
    }

    [Fact]
    public async Task HandleAsync_NoYearOrMonth_PassesNullCriteria()
    {
        var reader = new FakeCalendarEventReader([]);
        var handler = CreateHandler(reader);

        await handler.HandleAsync(Query(), CancellationToken.None);

        Assert.True(reader.ListCalled);
        Assert.Null(reader.Criteria);
    }

    [Fact]
    public async Task HandleAsync_OnlyYear_PassesNullCriteria()
    {
        var reader = new FakeCalendarEventReader([]);
        var handler = CreateHandler(reader);

        await handler.HandleAsync(Query(year: 2026), CancellationToken.None);

        Assert.Null(reader.Criteria);
    }

    [Fact]
    public async Task HandleAsync_ForwardsCancellationToken()
    {
        var reader = new FakeCalendarEventReader([]);
        var platformReader = new FakePlatformReader();
        var handler = CreateHandler(reader, platformReader);
        using var cancellationTokenSource = new CancellationTokenSource();

        await handler.HandleAsync(Query(), cancellationTokenSource.Token);

        Assert.Equal(cancellationTokenSource.Token, reader.CancellationToken);
        Assert.Equal(
            cancellationTokenSource.Token,
            platformReader.ListIdsCancellationToken);
    }

    [Fact]
    public async Task HandleAsync_ListRecords_ComposesPublicationStatusFromActivePlatformIds()
    {
        var reader = new FakeCalendarEventReader(
            listRecords:
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
        var platformReader = new FakePlatformReader(
            platformIds: Set("platform-a", "platform-b"));
        var handler = CreateHandler(reader, platformReader);

        var result = await handler.HandleAsync(Query(), CancellationToken.None);

        Assert.Equal(PublishingStatus.NotPublished, StatusFor(result, FirstId));
        Assert.Equal(PublishingStatus.PartiallyPublished, StatusFor(result, SecondId));
        Assert.Equal(PublishingStatus.FullyPublished, StatusFor(result, ThirdId));
        Assert.Equal(1, platformReader.ListIdsCallCount);
    }

    [Fact]
    public async Task HandleAsync_NullQuery_Throws()
    {
        var handler = CreateHandler(new FakeCalendarEventReader([]));

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

    private static ListEventsHandler CreateHandler(
        FakeCalendarEventReader calendarEvents,
        FakePlatformReader? platforms = null) =>
        new(calendarEvents, platforms ?? new FakePlatformReader());

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
