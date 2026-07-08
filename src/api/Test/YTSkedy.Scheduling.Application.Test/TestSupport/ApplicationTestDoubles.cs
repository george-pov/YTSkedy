using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Settings;
using YTSkedy.Scheduling.Application.Templates;
using YTSkedy.Scheduling.Domain.CalendarEvents;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Scheduling.Application.Test;

internal sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

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

internal sealed class FakeCalendarEventReader : ICalendarEventReader
{
    private readonly IReadOnlyList<CalendarEventView> items;
    private readonly CalendarEventView? getResult;

    public FakeCalendarEventReader(
        IReadOnlyList<CalendarEventView>? items = null,
        CalendarEventView? getResult = null)
    {
        this.items = items ?? (getResult is null ? [] : [getResult]);
        this.getResult = getResult;
    }

    public bool ListCalled { get; private set; }

    public int GetByIdCallCount { get; private set; }

    public CalendarEventMonthCriteria? Criteria { get; private set; }

    public CancellationToken CancellationToken { get; private set; }

    public string? CalendarEventId { get; private set; }

    public Task<IReadOnlyList<CalendarEventView>> ListAsync(
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
            items.FirstOrDefault(candidate =>
                string.Equals(candidate.CalendarEventId, calendarEventId, StringComparison.Ordinal));

        return Task.FromResult(result);
    }
}

internal sealed class FakePlatformReader : IPlatformReader
{
    private readonly IReadOnlyList<PlatformView> platforms;
    private readonly PlatformView? getResult;

    public FakePlatformReader(
        IReadOnlyList<PlatformView>? platforms = null,
        PlatformView? getResult = null)
    {
        this.platforms = platforms ?? (getResult is null ? [] : [getResult]);
        this.getResult = getResult;
    }

    public bool ListCalled { get; private set; }

    public PlatformType? RequestedType { get; private set; }

    public string? PlatformId { get; private set; }

    public CancellationToken CancellationToken { get; private set; }

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
