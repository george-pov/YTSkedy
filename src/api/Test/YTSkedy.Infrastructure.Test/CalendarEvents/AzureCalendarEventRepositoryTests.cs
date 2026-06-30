using Azure;
using Azure.Core;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using YTSkedy.Infrastructure.CalendarEvents;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Infrastructure.Test.CalendarEvents;

public sealed class AzureCalendarEventRepositoryTests
{
    [Fact]
    public async Task CreateAsync_CalendarEvent_StoresTextJson()
    {
        var tableClient = new InMemoryTableClient();
        var repository = CreateRepository(tableClient);
        var calendarEvent = new CalendarEvent(
            new ScheduledStart(new DateTime(2026, 6, 15, 17, 0, 0), "UTC"),
            Text("Original title", "Original description"));

        var calendarEventId = await repository.CreateAsync(calendarEvent, CancellationToken.None);

        var entity = Assert.Single(tableClient.Entities.Values);
        Assert.Equal(calendarEventId, entity.CalendarEventId);
        Assert.Equal("2026-06-15T17:00:00", entity.LocalDateTime);
        Assert.Contains("\"fieldKey\":\"text1\"", entity.TextJson, StringComparison.Ordinal);
        Assert.Contains("\"value\":\"Original title\"", entity.TextJson, StringComparison.Ordinal);
        Assert.Contains("\"fieldKey\":\"text2\"", entity.TextJson, StringComparison.Ordinal);
        Assert.Contains("\"value\":\"Original description\"", entity.TextJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UpdateTextAsync_ExistingEvent_ReplacesTextJson()
    {
        var tableClient = new InMemoryTableClient();
        var repository = CreateRepository(tableClient);
        var calendarEvent = new CalendarEvent(
            new ScheduledStart(new DateTime(2026, 6, 15, 17, 0, 0), "UTC"),
            Text("Original title", "Original description"));
        var calendarEventId = await repository.CreateAsync(calendarEvent, CancellationToken.None);

        var result = await repository.UpdateTextAsync(
            calendarEventId,
            Text("Updated title", "Updated description"),
            CancellationToken.None);

        var entity = Assert.Single(tableClient.Entities.Values);
        Assert.True(result);
        Assert.Contains("\"value\":\"Updated title\"", entity.TextJson, StringComparison.Ordinal);
        Assert.Contains("\"value\":\"Updated description\"", entity.TextJson, StringComparison.Ordinal);
        Assert.Equal("2026-06-15T17:00:00", entity.LocalDateTime);
    }

    [Fact]
    public async Task UpdateTextAsync_MissingEvent_ReturnsFalse()
    {
        var repository = CreateRepository(new InMemoryTableClient());

        var result = await repository.UpdateTextAsync(
            "20260615T170000Z-missing",
            Text("Updated title", "Updated description"),
            CancellationToken.None);

        Assert.False(result);
    }

    private static AzureCalendarEventRepository CreateRepository(InMemoryTableClient tableClient) =>
        new(tableClient, new FixedTimeProvider(
            new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero)));

    private static EventTextSnapshot Text(string title, string description) =>
        EventTextSnapshot.Create(
            EventTextFields.Default,
            [
                new EventTextValue("text1", title),
                new EventTextValue("text2", description)
            ]);

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
