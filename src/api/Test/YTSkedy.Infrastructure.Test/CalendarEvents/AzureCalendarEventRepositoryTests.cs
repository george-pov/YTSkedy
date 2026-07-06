using System.Globalization;
using System.Text.Json;
using Azure;
using Azure.Core;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using YTSkedy.Infrastructure.CalendarEvents;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Infrastructure.Test.CalendarEvents;

public sealed class AzureCalendarEventRepositoryTests
{
    [Fact]
    public async Task CreateAsync_CalendarEvent_StoresStableKeysAndTextJson()
    {
        var tableClient = new InMemoryTableClient();
        var repository = CreateRepository(tableClient);
        var calendarEvent = new CalendarEvent(
            new ScheduledStart(new DateTime(2026, 6, 15, 10, 0, 0), "America/Vancouver"),
            Text("Original title", "Original description"));
        var scheduledStartUtc = new DateTimeOffset(2026, 6, 15, 17, 0, 0, TimeSpan.Zero);

        var calendarEventId = await repository.CreateAsync(
            calendarEvent,
            scheduledStartUtc,
            CancellationToken.None);

        var entity = Assert.Single(tableClient.Entities.Values);
        Assert.Equal(32, calendarEventId.Length);
        Assert.Equal(calendarEventId, entity.CalendarEventId);
        Assert.Equal("calendar-events", entity.PartitionKey);
        Assert.Equal($"event-{calendarEventId}", entity.RowKey);
        Assert.Equal(scheduledStartUtc, entity.ScheduledStartUtc);
        Assert.Equal("2026-06-15T10:00:00", entity.LocalDateTime);
        Assert.Equal("America/Vancouver", entity.TimeZoneId);
        Assert.Equal(["text1", "text2"], FieldKeys(entity.TextJson));
        Assert.Equal("Original title", ValueFor(entity.TextJson, "text1"));
        Assert.Equal("Original description", ValueFor(entity.TextJson, "text2"));
        Assert.Null(entity.ThumbnailJson);
    }

    [Fact]
    public async Task CreateAsync_DuplicateScheduledStart_ThrowsDuplicateScheduledStartException()
    {
        var tableClient = new InMemoryTableClient();
        var repository = CreateRepository(tableClient);
        var scheduledStartUtc = new DateTimeOffset(2026, 6, 15, 17, 0, 0, TimeSpan.Zero);
        await repository.CreateAsync(
            Event("Original title", "2026-06-15T10:00:00"),
            scheduledStartUtc,
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<DuplicateScheduledStartException>(
            () => repository.CreateAsync(
                Event("Duplicate title", "2026-06-15T11:00:00"),
                scheduledStartUtc,
                CancellationToken.None));

        Assert.Equal(scheduledStartUtc, exception.ScheduledStartUtc);
    }

    [Fact]
    public async Task GetByIdAsync_OpaqueId_ReturnsCalendarEvent()
    {
        var tableClient = new InMemoryTableClient();
        var repository = CreateRepository(tableClient);
        var scheduledStartUtc = new DateTimeOffset(2026, 6, 15, 17, 0, 0, TimeSpan.Zero);
        var calendarEventId = await repository.CreateAsync(
            Event("Original title", "2026-06-15T10:00:00"),
            scheduledStartUtc,
            CancellationToken.None);

        var result = await repository.GetByIdAsync(calendarEventId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(calendarEventId, result!.CalendarEventId);
        Assert.Equal(scheduledStartUtc, result.ScheduledStartUtc);
        Assert.Equal("Original title", result.Text.ValueFor("text1"));
    }

    [Fact]
    public async Task GetByIdAsync_LegacyScheduledStartId_ReturnsNull()
    {
        var repository = CreateRepository(new InMemoryTableClient());

        var result = await repository.GetByIdAsync(
            "start-20260606T170000Z-6f9619ff8b864fb5bdfd4f5c2f2f16a1",
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_OpaqueId_RemovesEventRow()
    {
        var tableClient = new InMemoryTableClient();
        var repository = CreateRepository(tableClient);
        var calendarEventId = await repository.CreateAsync(
            Event("Original title", "2026-06-15T10:00:00"),
            new DateTimeOffset(2026, 6, 15, 17, 0, 0, TimeSpan.Zero),
            CancellationToken.None);

        await repository.DeleteAsync(calendarEventId, CancellationToken.None);

        Assert.Empty(tableClient.Entities);
    }

    [Fact]
    public async Task ListAsync_MonthCriteria_FiltersSinglePartitionByLocalMonth()
    {
        var tableClient = new InMemoryTableClient();
        var repository = CreateRepository(tableClient);
        var tokyoJuneId = await repository.CreateAsync(
            Event("Tokyo June", "2026-06-01T00:30:00", "Asia/Tokyo"),
            new DateTimeOffset(2026, 5, 31, 15, 30, 0, TimeSpan.Zero),
            CancellationToken.None);
        var vancouverJuneId = await repository.CreateAsync(
            Event("Vancouver June", "2026-06-15T10:00:00"),
            new DateTimeOffset(2026, 6, 15, 17, 0, 0, TimeSpan.Zero),
            CancellationToken.None);
        var lateJuneId = await repository.CreateAsync(
            Event("Late June", "2026-06-30T23:30:00"),
            new DateTimeOffset(2026, 7, 1, 6, 30, 0, TimeSpan.Zero),
            CancellationToken.None);
        await repository.CreateAsync(
            Event("May local", "2026-05-31T23:30:00"),
            new DateTimeOffset(2026, 6, 1, 6, 30, 0, TimeSpan.Zero),
            CancellationToken.None);

        var result = await repository.ListAsync(
            new CalendarEventMonthCriteria(2026, 6),
            CancellationToken.None);

        Assert.Equal(
            [tokyoJuneId, vancouverJuneId, lateJuneId],
            result.Select(calendarEvent => calendarEvent.CalendarEventId));
    }

    [Fact]
    public async Task ListAsync_NoCriteria_ReturnsAllEvents()
    {
        var tableClient = new InMemoryTableClient();
        var repository = CreateRepository(tableClient);
        var firstId = await repository.CreateAsync(
            Event("First", "2026-06-15T10:00:00"),
            new DateTimeOffset(2026, 6, 15, 17, 0, 0, TimeSpan.Zero),
            CancellationToken.None);
        var secondId = await repository.CreateAsync(
            Event("Second", "2026-07-15T10:00:00"),
            new DateTimeOffset(2026, 7, 15, 17, 0, 0, TimeSpan.Zero),
            CancellationToken.None);

        var result = await repository.ListAsync(null, CancellationToken.None);

        Assert.Equal(
            [firstId, secondId],
            result.Select(calendarEvent => calendarEvent.CalendarEventId));
    }

    [Fact]
    public async Task UpdateAsync_ExistingEvent_ReplacesScheduledStartAndTextJson()
    {
        var tableClient = new InMemoryTableClient();
        var repository = CreateRepository(tableClient);
        var calendarEventId = await repository.CreateAsync(
            Event("Original title", "2026-06-15T10:00:00"),
            new DateTimeOffset(2026, 6, 15, 17, 0, 0, TimeSpan.Zero),
            CancellationToken.None);
        var updatedEvent = Event(
            "Updated title",
            "2026-07-20T09:30:00",
            "Europe/London");
        var updatedScheduledStartUtc = new DateTimeOffset(
            2026,
            7,
            20,
            8,
            30,
            0,
            TimeSpan.Zero);

        var result = await repository.UpdateAsync(
            calendarEventId,
            updatedEvent,
            updatedScheduledStartUtc,
            CancellationToken.None);

        var entity = Assert.Single(tableClient.Entities.Values);
        Assert.True(result);
        Assert.Equal("calendar-events", entity.PartitionKey);
        Assert.Equal($"event-{calendarEventId}", entity.RowKey);
        Assert.Equal(updatedScheduledStartUtc, entity.ScheduledStartUtc);
        Assert.Equal("2026-07-20T09:30:00", entity.LocalDateTime);
        Assert.Equal("Europe/London", entity.TimeZoneId);
        Assert.Equal("Updated title", ValueFor(entity.TextJson, "text1"));
        Assert.Equal("Updated description", ValueFor(entity.TextJson, "text2"));
    }

    [Fact]
    public async Task UpdateAsync_MissingEvent_ReturnsFalse()
    {
        var repository = CreateRepository(new InMemoryTableClient());

        var result = await repository.UpdateAsync(
            "6f9619ff8b864fb5bdfd4f5c2f2f16a1",
            Event("Updated title", "2026-06-15T10:00:00"),
            new DateTimeOffset(2026, 6, 15, 17, 0, 0, TimeSpan.Zero),
            CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task UpdateAsync_DuplicateScheduledStart_ThrowsDuplicateScheduledStartException()
    {
        var tableClient = new InMemoryTableClient();
        var repository = CreateRepository(tableClient);
        var scheduledStartUtc = new DateTimeOffset(2026, 6, 15, 17, 0, 0, TimeSpan.Zero);
        await repository.CreateAsync(
            Event("First", "2026-06-15T10:00:00"),
            scheduledStartUtc,
            CancellationToken.None);
        var otherEventId = await repository.CreateAsync(
            Event("Second", "2026-07-15T10:00:00"),
            new DateTimeOffset(2026, 7, 15, 17, 0, 0, TimeSpan.Zero),
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<DuplicateScheduledStartException>(
            () => repository.UpdateAsync(
                otherEventId,
                Event("Second updated", "2026-06-15T10:00:00"),
                scheduledStartUtc,
                CancellationToken.None));

        Assert.Equal(scheduledStartUtc, exception.ScheduledStartUtc);
    }

    [Fact]
    public async Task SaveThumbnailAsync_ExistingEvent_StoresThumbnailJson()
    {
        var tableClient = new InMemoryTableClient();
        var repository = CreateRepository(tableClient);
        var calendarEventId = await repository.CreateAsync(
            Event("Original title", "2026-06-15T10:00:00"),
            new DateTimeOffset(2026, 6, 15, 17, 0, 0, TimeSpan.Zero),
            CancellationToken.None);
        var thumbnail = Thumbnail(calendarEventId);

        var result = await repository.SaveThumbnailAsync(
            calendarEventId,
            thumbnail,
            CancellationToken.None);

        var entity = Assert.Single(tableClient.Entities.Values);
        Assert.True(result);
        Assert.NotNull(entity.ThumbnailJson);
        Assert.Equal(thumbnail, await repository.GetThumbnailAsync(
            calendarEventId,
            CancellationToken.None));
        Assert.Equal("Original title", ValueFor(entity.TextJson, "text1"));
    }

    [Fact]
    public async Task SaveThumbnailAsync_MissingEvent_ReturnsFalse()
    {
        var repository = CreateRepository(new InMemoryTableClient());

        var result = await repository.SaveThumbnailAsync(
            "6f9619ff8b864fb5bdfd4f5c2f2f16a1",
            Thumbnail("6f9619ff8b864fb5bdfd4f5c2f2f16a1"),
            CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task GetThumbnailAsync_NoThumbnail_ReturnsNull()
    {
        var tableClient = new InMemoryTableClient();
        var repository = CreateRepository(tableClient);
        var calendarEventId = await repository.CreateAsync(
            Event("Original title", "2026-06-15T10:00:00"),
            new DateTimeOffset(2026, 6, 15, 17, 0, 0, TimeSpan.Zero),
            CancellationToken.None);

        var result = await repository.GetThumbnailAsync(
            calendarEventId,
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteThumbnailAsync_ExistingEvent_ClearsThumbnailJson()
    {
        var tableClient = new InMemoryTableClient();
        var repository = CreateRepository(tableClient);
        var calendarEventId = await repository.CreateAsync(
            Event("Original title", "2026-06-15T10:00:00"),
            new DateTimeOffset(2026, 6, 15, 17, 0, 0, TimeSpan.Zero),
            CancellationToken.None);
        await repository.SaveThumbnailAsync(
            calendarEventId,
            Thumbnail(calendarEventId),
            CancellationToken.None);

        var result = await repository.DeleteThumbnailAsync(
            calendarEventId,
            CancellationToken.None);

        var entity = Assert.Single(tableClient.Entities.Values);
        Assert.True(result);
        Assert.Null(entity.ThumbnailJson);
        Assert.Null(await repository.GetThumbnailAsync(
            calendarEventId,
            CancellationToken.None));
    }

    [Fact]
    public async Task DeleteThumbnailAsync_MissingEvent_ReturnsFalse()
    {
        var repository = CreateRepository(new InMemoryTableClient());

        var result = await repository.DeleteThumbnailAsync(
            "6f9619ff8b864fb5bdfd4f5c2f2f16a1",
            CancellationToken.None);

        Assert.False(result);
    }

    private static AzureCalendarEventRepository CreateRepository(InMemoryTableClient tableClient) =>
        new(tableClient, new FixedTimeProvider(
            new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero)));

    private static CalendarEvent Event(
        string title,
        string localDateTime,
        string timeZoneId = "America/Vancouver") =>
        new(
            new ScheduledStart(
                DateTime.Parse(
                    localDateTime,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None),
                timeZoneId),
            Text(title, "Updated description"));

    private static EventTextSnapshot Text(string title, string description) =>
        EventTextSnapshot.Create(
            EventTextFields.Default,
            [
                new EventTextValue("text1", title),
                new EventTextValue("text2", description)
            ]);

    private static Thumbnail Thumbnail(string calendarEventId) =>
        new(
            "stream.png",
            "image/png",
            123,
            1280,
            720,
            new DateTimeOffset(2026, 6, 20, 12, 0, 0, TimeSpan.Zero),
            $"calendar-events/{calendarEventId}/thumbnail");

    private static string[] FieldKeys(string textJson)
    {
        using var document = JsonDocument.Parse(textJson);

        return document.RootElement
            .GetProperty("fields")
            .EnumerateArray()
            .Select(field => field.GetProperty("fieldKey").GetString() ?? string.Empty)
            .ToArray();
    }

    private static string ValueFor(string textJson, string fieldKey)
    {
        using var document = JsonDocument.Parse(textJson);

        var value = document.RootElement
            .GetProperty("values")
            .EnumerateArray()
            .Single(value => string.Equals(
                value.GetProperty("fieldKey").GetString(),
                fieldKey,
                StringComparison.Ordinal));

        return value.GetProperty("value").GetString() ?? string.Empty;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class InMemoryTableClient : TableClient
    {
        public Dictionary<(string PartitionKey, string RowKey), CalendarEventEntity> Entities { get; } = [];

        public override Task<Response<TableItem>> CreateIfNotExistsAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Response.FromValue(
                TableModelFactory.TableItem("CalendarEvents"),
                StubResponse.Instance));

        public override Task<Response> AddEntityAsync<T>(
            T entity,
            CancellationToken cancellationToken = default)
        {
            var calendarEvent = ToCalendarEventEntity(entity);
            var key = (calendarEvent.PartitionKey, calendarEvent.RowKey);
            if (Entities.ContainsKey(key))
            {
                throw new RequestFailedException(409, "Entity already exists.");
            }

            Entities[key] = Clone(calendarEvent);

            return Task.FromResult<Response>(StubResponse.Instance);
        }

        public override Task<Response> UpdateEntityAsync<T>(
            T entity,
            ETag ifMatch,
            TableUpdateMode mode,
            CancellationToken cancellationToken = default)
        {
            var calendarEvent = ToCalendarEventEntity(entity);
            var key = (calendarEvent.PartitionKey, calendarEvent.RowKey);
            if (!Entities.ContainsKey(key))
            {
                throw new RequestFailedException(404, "Entity not found.");
            }

            Entities[key] = Clone(calendarEvent);

            return Task.FromResult<Response>(StubResponse.Instance);
        }

        public override Task<Response> DeleteEntityAsync(
            string partitionKey,
            string rowKey,
            ETag ifMatch = default,
            CancellationToken cancellationToken = default)
        {
            Entities.Remove((partitionKey, rowKey));

            return Task.FromResult<Response>(StubResponse.Instance);
        }

        public override Task<NullableResponse<T>> GetEntityIfExistsAsync<T>(
            string partitionKey,
            string rowKey,
            IEnumerable<string>? select = null,
            CancellationToken cancellationToken = default)
        {
            if (Entities.TryGetValue((partitionKey, rowKey), out var entity))
            {
                return Task.FromResult<NullableResponse<T>>(
                    Response.FromValue((T)(object)Clone(entity), StubResponse.Instance));
            }

            return Task.FromResult<NullableResponse<T>>(new EmptyNullableResponse<T>());
        }

        public override AsyncPageable<T> QueryAsync<T>(
            string? filter = null,
            int? maxPerPage = null,
            IEnumerable<string>? select = null,
            CancellationToken cancellationToken = default)
        {
            var values = filter is null || string.Equals(
                filter,
                CalendarEventStorageKey.PartitionFilter(),
                StringComparison.Ordinal)
                    ? Entities.Values.Select(entity => (T)(object)Clone(entity)).ToArray()
                    : [];

            return AsyncPageable<T>.FromPages(
                [Page<T>.FromValues(values, continuationToken: null, StubResponse.Instance)]);
        }

        private static CalendarEventEntity ToCalendarEventEntity<T>(T entity)
        {
            if (entity is not CalendarEventEntity calendarEvent)
            {
                throw new InvalidOperationException($"Unsupported entity type '{typeof(T).Name}'.");
            }

            return calendarEvent;
        }

        private static CalendarEventEntity Clone(CalendarEventEntity entity) =>
            new()
            {
                PartitionKey = entity.PartitionKey,
                RowKey = entity.RowKey,
                Timestamp = entity.Timestamp,
                ETag = entity.ETag,
                CalendarEventId = entity.CalendarEventId,
                ScheduledStartUtc = entity.ScheduledStartUtc,
                LocalDateTime = entity.LocalDateTime,
                TimeZoneId = entity.TimeZoneId,
                TextJson = entity.TextJson,
                ThumbnailJson = entity.ThumbnailJson,
                CreatedUtc = entity.CreatedUtc
            };
    }

    private sealed class EmptyNullableResponse<T> : NullableResponse<T>
    {
        public override bool HasValue => false;

        public override T Value => throw new InvalidOperationException("No value is available.");

        public override Response GetRawResponse() => StubResponse.Instance;
    }

    private sealed class StubResponse : Response
    {
        public static readonly StubResponse Instance = new();

        private StubResponse()
        {
        }

        public override int Status => 200;

        public override string ReasonPhrase => "OK";

        public override Stream? ContentStream { get; set; }

        public override string ClientRequestId { get; set; } = string.Empty;

        protected override bool ContainsHeader(string name) => false;

        protected override IEnumerable<HttpHeader> EnumerateHeaders() => [];

        protected override bool TryGetHeader(string name, out string value)
        {
            value = string.Empty;

            return false;
        }

        protected override bool TryGetHeaderValues(string name, out IEnumerable<string> values)
        {
            values = [];

            return false;
        }

        public override void Dispose()
        {
        }
    }
}
