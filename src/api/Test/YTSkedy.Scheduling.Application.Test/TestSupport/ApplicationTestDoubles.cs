using Microsoft.Extensions.Logging;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.CalendarEvents.Thumbnails;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Platforms.Publications;
using YTSkedy.Scheduling.Application.Platforms.WordPressCategory;
using YTSkedy.Scheduling.Application.Settings;
using YTSkedy.Scheduling.Application.Templates;
using YTSkedy.Scheduling.Domain.CalendarEvents;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Scheduling.Application.Test;

internal sealed class FakeEventTextFieldsReader(EventTextFields eventTextFields) :
    IEventTextFieldsReader
{
    public bool WasCalled { get; private set; }

    public Task<EventTextFields> GetAsync(CancellationToken cancellationToken)
    {
        WasCalled = true;

        return Task.FromResult(eventTextFields);
    }
}

internal sealed class FakeStartDefaultsStore(StartDefaults current) :
    IStartDefaultsReader
{
    public int GetCallCount { get; private set; }

    public CancellationToken CancellationToken { get; private set; }

    public Task<StartDefaults> GetAsync(CancellationToken cancellationToken)
    {
        GetCallCount++;
        CancellationToken = cancellationToken;
        return Task.FromResult(current);
    }
}

internal sealed class FakeCalendarEventReader : ICalendarEventReader
{
    private readonly IReadOnlyList<CalendarEventListRecord> items;
    private readonly CalendarEventView? getResult;

    public FakeCalendarEventReader(
        IReadOnlyList<CalendarEventView>? items = null,
        CalendarEventView? getResult = null,
        IReadOnlyList<CalendarEventListRecord>? listRecords = null)
    {
        this.items = listRecords ??
            (items ?? (getResult is null ? [] : [getResult]))
                .Select(item => new CalendarEventListRecord(
                    item,
                    new HashSet<string>(StringComparer.Ordinal)))
                .ToArray();
        this.getResult = getResult;
    }

    public bool ListCalled { get; private set; }

    public int GetByIdCallCount { get; private set; }

    public CalendarEventMonthCriteria? Criteria { get; private set; }

    public CancellationToken CancellationToken { get; private set; }

    public string? CalendarEventId { get; private set; }

    public Task<IReadOnlyList<CalendarEventListRecord>> ListAsync(
        CalendarEventMonthCriteria? criteria,
        CancellationToken cancellationToken)
    {
        ListCalled = true;
        Criteria = criteria;
        CancellationToken = cancellationToken;

        return Task.FromResult(items);
    }

    public Task<CalendarEventView?> GetByIdAsync(
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        GetByIdCallCount++;
        CalendarEventId = calendarEventId;
        CancellationToken = cancellationToken;

        var result = getResult ??
            items.Select(item => item.Event).FirstOrDefault(candidate =>
                string.Equals(candidate.CalendarEventId, calendarEventId, StringComparison.Ordinal));

        return Task.FromResult(result);
    }
}

internal sealed class FakePlatformReader : IPlatformReader
{
    private readonly IReadOnlyList<PlatformView> platforms;
    private readonly PlatformView? getResult;
    private readonly IReadOnlySet<string> platformIds;

    public FakePlatformReader(
        IReadOnlyList<PlatformView>? platforms = null,
        PlatformView? getResult = null,
        IReadOnlySet<string>? platformIds = null)
    {
        this.platforms = platforms ?? (getResult is null ? [] : [getResult]);
        this.getResult = getResult;
        this.platformIds = platformIds ?? new HashSet<string>(
            this.platforms.Select(platform => platform.PlatformId),
            StringComparer.Ordinal);
    }

    public bool ListCalled { get; private set; }

    public int ListIdsCallCount { get; private set; }

    public PlatformType? RequestedType { get; private set; }

    public string? PlatformId { get; private set; }

    public CancellationToken CancellationToken { get; private set; }

    public CancellationToken ListIdsCancellationToken { get; private set; }

    public Task<IReadOnlyList<PlatformView>> ListAsync(
        PlatformType? type,
        CancellationToken cancellationToken)
    {
        ListCalled = true;
        RequestedType = type;
        CancellationToken = cancellationToken;

        IReadOnlyList<PlatformView> result = type is null
            ? platforms
            : platforms.Where(platform => platform.Type == type).ToArray();

        return Task.FromResult(result);
    }

    public Task<IReadOnlySet<string>> ListIdsAsync(
        CancellationToken cancellationToken)
    {
        ListIdsCallCount++;
        ListIdsCancellationToken = cancellationToken;

        return Task.FromResult(platformIds);
    }

    public Task<PlatformView?> GetAsync(
        string platformId,
        CancellationToken cancellationToken)
    {
        PlatformId = platformId;
        CancellationToken = cancellationToken;

        var result = getResult ??
            platforms.FirstOrDefault(candidate =>
                string.Equals(candidate.PlatformId, platformId, StringComparison.Ordinal));

        return Task.FromResult(result);
    }
}

internal sealed class FakeCategoryReader : ICategoryReader
{
    public CategoryPage Result { get; init; } = new([], 1, 25, 0, 0);

    public Exception? Exception { get; init; }

    public int CallCount { get; private set; }

    public WordPressSettings? Settings { get; private set; }

    public CategoryQuery? Query { get; private set; }

    public CancellationToken CancellationToken { get; private set; }

    public Task<CategoryPage> ListAsync(
        WordPressSettings settings,
        CategoryQuery query,
        CancellationToken cancellationToken)
    {
        CallCount++;
        Settings = settings;
        Query = query;
        CancellationToken = cancellationToken;

        return Exception is null
            ? Task.FromResult(Result)
            : Task.FromException<CategoryPage>(Exception);
    }
}

internal sealed class FakeCalendarEventPublicationIndexWriter : IPublicationIndexWriter
{
    public bool AddResult { get; init; } = true;

    public bool RemoveResult { get; init; } = true;

    public Exception? AddException { get; init; }

    public Exception? RemoveException { get; init; }

    public List<(string CalendarEventId, string PlatformId)> AddCalls { get; } = [];

    public List<(string CalendarEventId, string PlatformId)> RemoveCalls { get; } = [];

    public Task<bool> AddPublishedPlatformAsync(
        string calendarEventId,
        string platformId,
        CancellationToken cancellationToken)
    {
        AddCalls.Add((calendarEventId, platformId));

        return AddException is null
            ? Task.FromResult(AddResult)
            : Task.FromException<bool>(AddException);
    }

    public Task<bool> RemovePublishedPlatformAsync(
        string calendarEventId,
        string platformId,
        CancellationToken cancellationToken)
    {
        RemoveCalls.Add((calendarEventId, platformId));

        return RemoveException is null
            ? Task.FromResult(RemoveResult)
            : Task.FromException<bool>(RemoveException);
    }
}

internal sealed class CapturingLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, string Message)> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter) =>
        Entries.Add((logLevel, formatter(state, exception)));
}

internal sealed class FakePlatformPublicationReader : IPlatformPublicationReader
{
    private readonly IReadOnlyList<PlatformPublication> rows;

    public FakePlatformPublicationReader(IReadOnlyList<PlatformPublication>? rows = null)
    {
        this.rows = rows ?? [];
    }

    public string? CalendarEventId { get; private set; }

    public string? PlatformId { get; private set; }

    public string? PublishingPlatformId { get; private set; }

    public CancellationToken CancellationToken { get; private set; }

    public Task<IReadOnlyList<PlatformPublication>> ListByEventAsync(
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        CalendarEventId = calendarEventId;
        CancellationToken = cancellationToken;

        var result = rows
            .Where(row => string.Equals(
                row.CalendarEventId,
                calendarEventId,
                StringComparison.Ordinal))
            .ToArray();

        return Task.FromResult<IReadOnlyList<PlatformPublication>>(result);
    }

    public Task<bool> HasAnyForEventAsync(
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        CalendarEventId = calendarEventId;
        CancellationToken = cancellationToken;

        var result = rows.Any(row => string.Equals(
            row.CalendarEventId,
            calendarEventId,
            StringComparison.Ordinal));

        return Task.FromResult(result);
    }

    public Task<PlatformPublication?> GetAsync(
        string calendarEventId,
        string platformId,
        CancellationToken cancellationToken)
    {
        CalendarEventId = calendarEventId;
        PlatformId = platformId;
        CancellationToken = cancellationToken;

        var result = rows.FirstOrDefault(row =>
            string.Equals(row.CalendarEventId, calendarEventId, StringComparison.Ordinal) &&
            string.Equals(row.PlatformId, platformId, StringComparison.Ordinal));

        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<PlatformPublication>> ListPublishingByPlatformAsync(
        string platformId,
        CancellationToken cancellationToken)
    {
        PublishingPlatformId = platformId;
        CancellationToken = cancellationToken;

        var result = rows
            .Where(row =>
                string.Equals(row.PlatformId, platformId, StringComparison.Ordinal) &&
                row.Status == PublishStatus.Publishing)
            .ToArray();

        return Task.FromResult<IReadOnlyList<PlatformPublication>>(result);
    }
}

internal sealed class FakeTemplateReader : ITemplateReader
{
    private readonly IReadOnlyList<TemplateView> templates;

    public FakeTemplateReader(params TemplateView[] templates)
        : this((IReadOnlyList<TemplateView>)templates)
    {
    }

    public FakeTemplateReader(IReadOnlyList<TemplateView> templates)
    {
        this.templates = templates;
    }

    public List<(TemplateType Type, string Id)> GetCalls { get; } = [];

    public int ListCallCount { get; private set; }

    public TemplateType? RequestedType { get; private set; }

    public Task<TemplateView?> GetAsync(
        TemplateType type,
        string templateId,
        CancellationToken cancellationToken)
    {
        GetCalls.Add((type, templateId));

        var template = templates.FirstOrDefault(candidate =>
            candidate.Type == type &&
            string.Equals(candidate.Id, templateId, StringComparison.Ordinal));

        return Task.FromResult(template);
    }

    public Task<IReadOnlyList<TemplateView>> ListAsync(
        TemplateType? type,
        CancellationToken cancellationToken)
    {
        ListCallCount++;
        RequestedType = type;

        var result = type is null
            ? templates
            : templates.Where(template => template.Type == type).ToArray();

        return Task.FromResult<IReadOnlyList<TemplateView>>(result);
    }
}

internal static class ApplicationTestAdapters
{
    public static FakeTemplateReader DefaultTemplateReader() =>
        new(ApplicationTestData.RequiredTemplates());
}

internal sealed class FakeThumbnailReader(Thumbnail? thumbnail) : ICalendarEventThumbnailReader
{
    public int GetCallCount { get; private set; }

    public string? CalendarEventId { get; private set; }

    public Task<Thumbnail?> GetThumbnailAsync(
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        GetCallCount++;
        CalendarEventId = calendarEventId;

        return Task.FromResult(thumbnail);
    }
}

internal sealed class FakeThumbnailStore(ThumbnailContent? content = null) : IThumbnailStore
{
    public int SaveCallCount { get; private set; }

    public int GetCallCount { get; private set; }

    public int DeleteCallCount { get; private set; }

    public string? SavedBlobName { get; private set; }

    public byte[]? SavedContent { get; private set; }

    public string? SavedContentType { get; private set; }

    public string? ReadBlobName { get; private set; }

    public string? DeletedBlobName { get; private set; }

    public bool ThrowOnDelete { get; init; }

    public Task SaveAsync(
        string blobName,
        byte[] content,
        string contentType,
        CancellationToken cancellationToken)
    {
        SaveCallCount++;
        SavedBlobName = blobName;
        SavedContent = content;
        SavedContentType = contentType;

        return Task.CompletedTask;
    }

    public Task<ThumbnailContent?> GetAsync(
        string blobName,
        CancellationToken cancellationToken)
    {
        GetCallCount++;
        ReadBlobName = blobName;

        return Task.FromResult(content);
    }

    public Task DeleteAsync(
        string blobName,
        CancellationToken cancellationToken)
    {
        DeleteCallCount++;
        DeletedBlobName = blobName;

        if (ThrowOnDelete)
        {
            throw new InvalidOperationException("Blob delete failed.");
        }

        return Task.CompletedTask;
    }
}
