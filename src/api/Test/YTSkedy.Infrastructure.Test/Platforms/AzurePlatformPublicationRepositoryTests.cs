using Azure;
using Azure.Core;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using YTSkedy.Infrastructure.Platforms;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Infrastructure.Test.Platforms;

public class AzurePlatformPublicationRepositoryTests
{
    private const string CalendarEventId = "f81d4fae7dec11d0a76500a0c91e6bf6";
    private const string PlatformId = "4fb4a32f3f344de1a7c3a9f4a2f94918";

    [Fact]
    public async Task StartAndMarkPublished_PreservesContentSnapshot()
    {
        var tableClient = new InMemoryTableClient();
        var repository = CreateRepository(tableClient);
        var attempt = new PlatformPublicationAttempt(
            CalendarEventId,
            PlatformId,
            "Main YouTube channel",
            PlatformType.YouTube,
            YouTubeSettings(),
            new ContentSnapshot("Rendered title", "Rendered description"));

        var start = await repository.StartPublishingAsync(attempt, CancellationToken.None);
        var publishing = await repository.GetAsync(CalendarEventId, PlatformId, CancellationToken.None);
        var publishedUtc = await repository.MarkPublishedAsync(
            CalendarEventId,
            PlatformId,
            "yt-broadcast-id",
            CancellationToken.None);
        var published = await repository.GetAsync(CalendarEventId, PlatformId, CancellationToken.None);

        Assert.Equal(StartPublicationResult.Started, start);
        Assert.NotNull(publishing);
        Assert.Equal(PublishStatus.Publishing, publishing.Status);
        Assert.Equal("Rendered title", publishing.ContentSnapshot!.Title);
        Assert.Equal("Rendered description", publishing.ContentSnapshot.Description);

        Assert.NotNull(publishedUtc);
        Assert.NotNull(published);
        Assert.Equal(PublishStatus.Published, published.Status);
        Assert.Equal("yt-broadcast-id", published.ExternalResourceId);
        Assert.Equal("Rendered title", published.ContentSnapshot!.Title);
        Assert.Equal("Rendered description", published.ContentSnapshot.Description);
    }

    [Fact]
    public void CanDeletePublished_PublishedRowWithMatchingExternalId_ReturnsDeleted()
    {
        var entity = PublishedEntity("yt-broadcast-id");

        var result = AzurePlatformPublicationRepository.CanDeletePublished(
            entity,
            "yt-broadcast-id");

        Assert.Equal(DeletePublishedResult.Deleted, result);
    }

    [Fact]
    public void CanDeletePublished_PublishingRow_ReturnsChanged()
    {
        var entity = PublishedEntity("yt-broadcast-id");
        entity.Status = PublishStatus.Publishing.ToString();

        var result = AzurePlatformPublicationRepository.CanDeletePublished(
            entity,
            "yt-broadcast-id");

        Assert.Equal(DeletePublishedResult.Changed, result);
    }

    [Fact]
    public void CanDeletePublished_OrphanedPublishedRow_ReturnsChanged()
    {
        var entity = PublishedEntity("yt-broadcast-id");
        entity.PlatformDeletedUtc = new DateTimeOffset(2026, 6, 23, 9, 0, 0, TimeSpan.Zero);

        var result = AzurePlatformPublicationRepository.CanDeletePublished(
            entity,
            "yt-broadcast-id");

        Assert.Equal(DeletePublishedResult.Changed, result);
    }

    [Fact]
    public void CanDeletePublished_ExternalResourceIdChanged_ReturnsChanged()
    {
        var entity = PublishedEntity("other-resource-id");

        var result = AzurePlatformPublicationRepository.CanDeletePublished(
            entity,
            "yt-broadcast-id");

        Assert.Equal(DeletePublishedResult.Changed, result);
    }

    private static AzurePlatformPublicationRepository CreateRepository(
        InMemoryTableClient tableClient) =>
        new(tableClient, new FixedTimeProvider(
            new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero)));

    private static PlatformPublicationEntity PublishedEntity(string externalResourceId) =>
        new()
        {
            PartitionKey = PlatformPublicationKey.PartitionKeyFor(CalendarEventId),
            RowKey = PlatformPublicationKey.RowKeyFor(PlatformId),
            CalendarEventId = CalendarEventId,
            PlatformId = PlatformId,
            PlatformName = "Main YouTube channel",
            PlatformType = PlatformType.YouTube.ToString(),
            Status = PublishStatus.Published.ToString(),
            ExternalResourceId = externalResourceId,
            PublishSettingsJson = "{}",
            PublishedUtc = new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero),
            CreatedUtc = new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero),
            UpdatedUtc = new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero)
        };

    private static YouTubeSettings YouTubeSettings() =>
        new(
            new YouTubeCredentials("client-id", "client-secret", "refresh-token"),
            "private",
            false);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class InMemoryTableClient : TableClient
    {
        private readonly Dictionary<(string PartitionKey, string RowKey), PlatformPublicationEntity>
            entities = [];

        public override Task<Response<TableItem>> CreateIfNotExistsAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Response.FromValue(
                TableModelFactory.TableItem("PlatformPublications"),
                StubResponse.Instance));

        public override Task<Response> AddEntityAsync<T>(
            T entity,
            CancellationToken cancellationToken = default)
        {
            var publication = ToPublicationEntity(entity);
            var key = (publication.PartitionKey, publication.RowKey);
            if (entities.ContainsKey(key))
            {
                throw new RequestFailedException(409, "Entity already exists.");
            }

            entities[key] = Clone(publication);

            return Task.FromResult<Response>(StubResponse.Instance);
        }

        public override Task<Response> UpdateEntityAsync<T>(
            T entity,
            ETag ifMatch,
            TableUpdateMode mode,
            CancellationToken cancellationToken = default)
        {
            var publication = ToPublicationEntity(entity);
            var key = (publication.PartitionKey, publication.RowKey);
            if (!entities.ContainsKey(key))
            {
                throw new RequestFailedException(404, "Entity not found.");
            }

            entities[key] = Clone(publication);

            return Task.FromResult<Response>(StubResponse.Instance);
        }

        public override Task<NullableResponse<T>> GetEntityIfExistsAsync<T>(
            string partitionKey,
            string rowKey,
            IEnumerable<string>? select = null,
            CancellationToken cancellationToken = default)
        {
            if (entities.TryGetValue((partitionKey, rowKey), out var entity))
            {
                return Task.FromResult<NullableResponse<T>>(
                    Response.FromValue((T)(object)Clone(entity), StubResponse.Instance));
            }

            return Task.FromResult<NullableResponse<T>>(new EmptyNullableResponse<T>());
        }

        private static PlatformPublicationEntity ToPublicationEntity<T>(T entity)
        {
            if (entity is not PlatformPublicationEntity publication)
            {
                throw new InvalidOperationException($"Unsupported entity type '{typeof(T).Name}'.");
            }

            return publication;
        }

        private static PlatformPublicationEntity Clone(PlatformPublicationEntity entity) =>
            new()
            {
                PartitionKey = entity.PartitionKey,
                RowKey = entity.RowKey,
                Timestamp = entity.Timestamp,
                ETag = entity.ETag,
                CalendarEventId = entity.CalendarEventId,
                PlatformId = entity.PlatformId,
                PlatformName = entity.PlatformName,
                PlatformType = entity.PlatformType,
                Status = entity.Status,
                ExternalResourceId = entity.ExternalResourceId,
                ContentSnapshotTitle = entity.ContentSnapshotTitle,
                ContentSnapshotDescription = entity.ContentSnapshotDescription,
                PublishSettingsJson = entity.PublishSettingsJson,
                PublishedUtc = entity.PublishedUtc,
                PlatformDeletedUtc = entity.PlatformDeletedUtc,
                CreatedUtc = entity.CreatedUtc,
                UpdatedUtc = entity.UpdatedUtc
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
