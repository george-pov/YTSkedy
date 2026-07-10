using System.Globalization;
using System.Text.Json;
using YTSkedy.Infrastructure.CalendarEvents;
using YTSkedy.Infrastructure.Test.TestSupport;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Domain.CalendarEvents;
using YTSkedy.TestSupport;

namespace YTSkedy.Infrastructure.Test.CalendarEvents;

public sealed class AzureCalendarEventRepositoryTests
{
    [Fact]
    public async Task CreateAsync_CalendarEvent_StoresStableKeysAndTextJson()
    {
        var tableClient = new CalendarEventTableClient();
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
        Assert.Equal("[]", entity.PublishedPlatformIdsJson);
        Assert.Null(entity.ThumbnailJson);
    }

    [Fact]
    public async Task CreateAsync_DuplicateScheduledStart_ThrowsDuplicateScheduledStartException()
    {
        var tableClient = new CalendarEventTableClient();
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
        var tableClient = new CalendarEventTableClient();
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
        var repository = CreateRepository(new CalendarEventTableClient());

        var result = await repository.GetByIdAsync(
            "start-20260606T170000Z-6f9619ff8b864fb5bdfd4f5c2f2f16a1",
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_OpaqueId_RemovesEventRow()
    {
        var tableClient = new CalendarEventTableClient();
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
        var tableClient = new CalendarEventTableClient();
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
            result.Select(record => record.Event.CalendarEventId));
    }

    [Fact]
    public async Task ListAsync_NoCriteria_ReturnsAllEvents()
    {
        var tableClient = new CalendarEventTableClient();
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
            result.Select(record => record.Event.CalendarEventId));
    }

    [Fact]
    public async Task UpdateAsync_ExistingEvent_ReplacesScheduledStartAndTextJson()
    {
        var tableClient = new CalendarEventTableClient();
        var repository = CreateRepository(tableClient);
        var calendarEventId = await repository.CreateAsync(
            Event("Original title", "2026-06-15T10:00:00"),
            new DateTimeOffset(2026, 6, 15, 17, 0, 0, TimeSpan.Zero),
            CancellationToken.None);
        await repository.AddPublishedPlatformAsync(
            calendarEventId,
            "platform-a",
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
        Assert.Equal(["platform-a"], PublishedPlatformIds(entity));
    }

    [Fact]
    public async Task UpdateAsync_MissingEvent_ReturnsFalse()
    {
        var repository = CreateRepository(new CalendarEventTableClient());

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
        var tableClient = new CalendarEventTableClient();
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
        var tableClient = new CalendarEventTableClient();
        var repository = CreateRepository(tableClient);
        var calendarEventId = await repository.CreateAsync(
            Event("Original title", "2026-06-15T10:00:00"),
            new DateTimeOffset(2026, 6, 15, 17, 0, 0, TimeSpan.Zero),
            CancellationToken.None);
        await repository.AddPublishedPlatformAsync(
            calendarEventId,
            "platform-a",
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
        Assert.Equal(["platform-a"], PublishedPlatformIds(entity));
    }

    [Fact]
    public async Task SaveThumbnailAsync_MissingEvent_ReturnsFalse()
    {
        var repository = CreateRepository(new CalendarEventTableClient());

        var result = await repository.SaveThumbnailAsync(
            "6f9619ff8b864fb5bdfd4f5c2f2f16a1",
            Thumbnail("6f9619ff8b864fb5bdfd4f5c2f2f16a1"),
            CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task GetThumbnailAsync_NoThumbnail_ReturnsNull()
    {
        var tableClient = new CalendarEventTableClient();
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
        var tableClient = new CalendarEventTableClient();
        var repository = CreateRepository(tableClient);
        var calendarEventId = await repository.CreateAsync(
            Event("Original title", "2026-06-15T10:00:00"),
            new DateTimeOffset(2026, 6, 15, 17, 0, 0, TimeSpan.Zero),
            CancellationToken.None);
        await repository.SaveThumbnailAsync(
            calendarEventId,
            Thumbnail(calendarEventId),
            CancellationToken.None);
        await repository.AddPublishedPlatformAsync(
            calendarEventId,
            "platform-a",
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
        Assert.Equal(["platform-a"], PublishedPlatformIds(entity));
    }

    [Fact]
    public async Task DeleteThumbnailAsync_MissingEvent_ReturnsFalse()
    {
        var repository = CreateRepository(new CalendarEventTableClient());

        var result = await repository.DeleteThumbnailAsync(
            "6f9619ff8b864fb5bdfd4f5c2f2f16a1",
            CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task AddPublishedPlatformAsync_NewIds_StoresDistinctOrdinallySortedIds()
    {
        var tableClient = new CalendarEventTableClient();
        var repository = CreateRepository(tableClient);
        var calendarEventId = await repository.CreateAsync(
            Event("Original title", "2026-06-15T10:00:00"),
            new DateTimeOffset(2026, 6, 15, 17, 0, 0, TimeSpan.Zero),
            CancellationToken.None);

        var firstResult = await repository.AddPublishedPlatformAsync(
            calendarEventId,
            "platform-b",
            CancellationToken.None);
        var secondResult = await repository.AddPublishedPlatformAsync(
            calendarEventId,
            "platform-a",
            CancellationToken.None);
        var idempotentResult = await repository.AddPublishedPlatformAsync(
            calendarEventId,
            "platform-a",
            CancellationToken.None);

        var entity = Assert.Single(tableClient.Entities.Values);
        Assert.True(firstResult);
        Assert.True(secondResult);
        Assert.True(idempotentResult);
        Assert.Equal("[\"platform-a\",\"platform-b\"]", entity.PublishedPlatformIdsJson);
        Assert.Equal(2, tableClient.UpdateCallCount);
        Assert.Equal(Azure.Data.Tables.TableUpdateMode.Merge, tableClient.LastUpdateMode);
        Assert.Equal("Original title", ValueFor(entity.TextJson, "text1"));
    }

    [Fact]
    public async Task RemovePublishedPlatformAsync_ExistingAndMissingIds_IsIdempotent()
    {
        var tableClient = new CalendarEventTableClient();
        var repository = CreateRepository(tableClient);
        var calendarEventId = await repository.CreateAsync(
            Event("Original title", "2026-06-15T10:00:00"),
            new DateTimeOffset(2026, 6, 15, 17, 0, 0, TimeSpan.Zero),
            CancellationToken.None);
        await repository.AddPublishedPlatformAsync(
            calendarEventId,
            "platform-a",
            CancellationToken.None);
        var updatesBeforeRemove = tableClient.UpdateCallCount;

        var missingResult = await repository.RemovePublishedPlatformAsync(
            calendarEventId,
            "platform-b",
            CancellationToken.None);
        var existingResult = await repository.RemovePublishedPlatformAsync(
            calendarEventId,
            "platform-a",
            CancellationToken.None);

        var entity = Assert.Single(tableClient.Entities.Values);
        Assert.True(missingResult);
        Assert.True(existingResult);
        Assert.Equal("[]", entity.PublishedPlatformIdsJson);
        Assert.Equal(updatesBeforeRemove + 1, tableClient.UpdateCallCount);
    }

    [Fact]
    public async Task AddPublishedPlatformAsync_MissingEvent_ReturnsFalse()
    {
        var repository = CreateRepository(new CalendarEventTableClient());

        var result = await repository.AddPublishedPlatformAsync(
            "6f9619ff8b864fb5bdfd4f5c2f2f16a1",
            "platform-a",
            CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task RemovePublishedPlatformAsync_MissingEvent_ReturnsFalse()
    {
        var repository = CreateRepository(new CalendarEventTableClient());

        var result = await repository.RemovePublishedPlatformAsync(
            "6f9619ff8b864fb5bdfd4f5c2f2f16a1",
            "platform-a",
            CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task AddPublishedPlatformAsync_PreconditionFailure_RereadsAndPreservesConcurrentIds()
    {
        var tableClient = new CalendarEventTableClient();
        var repository = CreateRepository(tableClient);
        var calendarEventId = await repository.CreateAsync(
            Event("Original title", "2026-06-15T10:00:00"),
            new DateTimeOffset(2026, 6, 15, 17, 0, 0, TimeSpan.Zero),
            CancellationToken.None);
        var storedEntity = Assert.Single(tableClient.Entities.Values);
        tableClient.FailNextUpdateWithPreconditionFailed(
            storedEntity,
            concurrentEntity => concurrentEntity.PublishedPlatformIdsJson =
                "[\"platform-b\"]");

        var result = await repository.AddPublishedPlatformAsync(
            calendarEventId,
            "platform-a",
            CancellationToken.None);

        Assert.True(result);
        Assert.Equal(2, tableClient.UpdateCallCount);
        Assert.Equal(
            ["platform-a", "platform-b"],
            PublishedPlatformIds(Assert.Single(tableClient.Entities.Values)));
    }

    [Fact]
    public async Task AddPublishedPlatformAsync_ThreePreconditionFailures_ReturnsFalse()
    {
        var tableClient = new CalendarEventTableClient();
        var repository = CreateRepository(tableClient);
        var calendarEventId = await repository.CreateAsync(
            Event("Original title", "2026-06-15T10:00:00"),
            new DateTimeOffset(2026, 6, 15, 17, 0, 0, TimeSpan.Zero),
            CancellationToken.None);
        var storedEntity = Assert.Single(tableClient.Entities.Values);
        tableClient.FailNextUpdateWithPreconditionFailed(storedEntity);
        tableClient.FailNextUpdateWithPreconditionFailed(storedEntity);
        tableClient.FailNextUpdateWithPreconditionFailed(storedEntity);

        var result = await repository.AddPublishedPlatformAsync(
            calendarEventId,
            "platform-a",
            CancellationToken.None);

        Assert.False(result);
        Assert.Equal(3, tableClient.UpdateCallCount);
        Assert.Empty(PublishedPlatformIds(Assert.Single(tableClient.Entities.Values)));
    }

    [Theory]
    [InlineData(null, "platform-a")]
    [InlineData("", "platform-a")]
    [InlineData("event-a", null)]
    [InlineData("event-a", " ")]
    public async Task AddPublishedPlatformAsync_BlankId_ThrowsArgumentException(
        string? calendarEventId,
        string? platformId)
    {
        var repository = CreateRepository(new CalendarEventTableClient());

        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            repository.AddPublishedPlatformAsync(
                calendarEventId!,
                platformId!,
                CancellationToken.None));
    }

    private static AzureCalendarEventRepository CreateRepository(CalendarEventTableClient tableClient) =>
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

    private static string[] PublishedPlatformIds(CalendarEventEntity entity) =>
        PublishedPlatformIdsJson.Deserialize(
                entity.PublishedPlatformIdsJson,
                entity.CalendarEventId)
            .Order(StringComparer.Ordinal)
            .ToArray();

}
