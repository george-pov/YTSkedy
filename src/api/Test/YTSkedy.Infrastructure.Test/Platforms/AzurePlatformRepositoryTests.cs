using Azure;
using Azure.Core;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using YTSkedy.Infrastructure.Platforms;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Infrastructure.Test.Platforms;

public sealed class AzurePlatformRepositoryTests
{
    [Fact]
    public async Task CreateAsync_NullReferenceKey_CreatesAndReadsNull()
    {
        var tableClient = new InMemoryTableClient();
        var repository = CreateRepository(tableClient);

        var result = await repository.CreateAsync(
            YouTubePlatform("Main YouTube channel", referenceKey: null),
            CancellationToken.None);

        Assert.Equal(CreatePlatformStatus.Created, result.Status);

        var view = await repository.GetAsync(result.PlatformId!, CancellationToken.None);

        Assert.NotNull(view);
        Assert.Null(view.ReferenceKey);
    }

    [Fact]
    public async Task CreateAsync_WithReferenceKey_ReadAndListPreserveDisplayCasing()
    {
        var tableClient = new InMemoryTableClient();
        var repository = CreateRepository(tableClient);

        var result = await repository.CreateAsync(
            YouTubePlatform("Main YouTube channel", "youTube1"),
            CancellationToken.None);

        var read = await repository.GetAsync(result.PlatformId!, CancellationToken.None);
        var listed = await repository.ListAsync(null, CancellationToken.None);

        Assert.NotNull(read);
        Assert.Equal("youTube1", read.ReferenceKey);
        Assert.Contains(listed, view => view.PlatformId == result.PlatformId &&
            view.ReferenceKey == "youTube1");
    }

    [Fact]
    public async Task CreateAsync_DuplicateReferenceKeyDifferentCasing_ReturnsReferenceKeyAlreadyExists()
    {
        var tableClient = new InMemoryTableClient();
        var repository = CreateRepository(tableClient);
        await repository.CreateAsync(
            YouTubePlatform("Main YouTube channel", "youTube1"),
            CancellationToken.None);

        var result = await repository.CreateAsync(
            YouTubePlatform("Backup YouTube channel", "youtube1"),
            CancellationToken.None);

        Assert.Equal(CreatePlatformStatus.ReferenceKeyAlreadyExists, result.Status);
        Assert.Null(result.PlatformId);
    }

    [Fact]
    public async Task UpdateAsync_NullToReferenceKey_PreservesDisplayCasing()
    {
        var tableClient = new InMemoryTableClient();
        var repository = CreateRepository(tableClient);
        var create = await repository.CreateAsync(
            YouTubePlatform("Main YouTube channel", referenceKey: null),
            CancellationToken.None);

        var result = await repository.UpdateAsync(
            create.PlatformId!,
            "Main YouTube channel",
            "WP-1",
            YouTubeSettings(),
            CancellationToken.None);
        var read = await repository.GetAsync(create.PlatformId!, CancellationToken.None);

        Assert.Equal(UpdatePlatformResult.Updated, result);
        Assert.NotNull(read);
        Assert.Equal("WP-1", read.ReferenceKey);
    }

    [Fact]
    public async Task UpdateAsync_CasingOnlyReferenceKeyChange_PreservesDisplayCasing()
    {
        var tableClient = new InMemoryTableClient();
        var repository = CreateRepository(tableClient);
        var create = await repository.CreateAsync(
            YouTubePlatform("Main YouTube channel", "WP-1"),
            CancellationToken.None);

        var result = await repository.UpdateAsync(
            create.PlatformId!,
            "Main YouTube channel",
            "wp-1",
            YouTubeSettings(),
            CancellationToken.None);
        var read = await repository.GetAsync(create.PlatformId!, CancellationToken.None);

        Assert.Equal(UpdatePlatformResult.Updated, result);
        Assert.NotNull(read);
        Assert.Equal("wp-1", read.ReferenceKey);
    }

    [Fact]
    public async Task UpdateAsync_OtherPlatformReferenceKeyDifferentCasing_ReturnsReferenceKeyAlreadyExists()
    {
        var tableClient = new InMemoryTableClient();
        var repository = CreateRepository(tableClient);
        await repository.CreateAsync(
            YouTubePlatform("Main YouTube channel", "WP-1"),
            CancellationToken.None);
        var other = await repository.CreateAsync(
            YouTubePlatform("Backup YouTube channel", referenceKey: null),
            CancellationToken.None);

        var result = await repository.UpdateAsync(
            other.PlatformId!,
            "Backup YouTube channel",
            "wp-1",
            YouTubeSettings(),
            CancellationToken.None);

        Assert.Equal(UpdatePlatformResult.ReferenceKeyAlreadyExists, result);
    }

    [Fact]
    public async Task DeleteAsync_ThenCreateWithSameReferenceKey_Creates()
    {
        var tableClient = new InMemoryTableClient();
        var repository = CreateRepository(tableClient);
        var create = await repository.CreateAsync(
            YouTubePlatform("Main YouTube channel", "youTube1"),
            CancellationToken.None);

        var delete = await repository.DeleteAsync(create.PlatformId!, CancellationToken.None);
        var recreate = await repository.CreateAsync(
            YouTubePlatform("Replacement YouTube channel", "youtube1"),
            CancellationToken.None);

        Assert.Equal(DeletePlatformResult.Deleted, delete);
        Assert.Equal(CreatePlatformStatus.Created, recreate.Status);
    }

    private static AzurePlatformRepository CreateRepository(InMemoryTableClient tableClient) =>
        new(tableClient, new FixedTimeProvider(
            new DateTimeOffset(2026, 06, 27, 12, 00, 00, TimeSpan.Zero)));

    private static Platform YouTubePlatform(string name, string? referenceKey) =>
        new(name, PlatformType.YouTube, YouTubeSettings(), referenceKey);

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
        private readonly Dictionary<(string PartitionKey, string RowKey), PlatformEntity> entities = [];

        public override Task<Response<TableItem>> CreateIfNotExistsAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Response.FromValue(
                TableModelFactory.TableItem("Platforms"),
                StubResponse.Instance));

        public override Task<Response> AddEntityAsync<T>(
            T entity,
            CancellationToken cancellationToken = default)
        {
            var platform = ToPlatformEntity(entity);
            entities[(platform.PartitionKey, platform.RowKey)] = Clone(platform);

            return Task.FromResult<Response>(StubResponse.Instance);
        }

        public override Task<Response> UpdateEntityAsync<T>(
            T entity,
            ETag ifMatch,
            TableUpdateMode mode,
            CancellationToken cancellationToken = default)
        {
            var platform = ToPlatformEntity(entity);
            var key = (platform.PartitionKey, platform.RowKey);
            if (!entities.ContainsKey(key))
            {
                throw new RequestFailedException(404, "Entity not found.");
            }

            entities[key] = Clone(platform);

            return Task.FromResult<Response>(StubResponse.Instance);
        }

        public override Task<Response> DeleteEntityAsync(
            string partitionKey,
            string rowKey,
            ETag ifMatch = default,
            CancellationToken cancellationToken = default)
        {
            if (!entities.Remove((partitionKey, rowKey)))
            {
                throw new RequestFailedException(404, "Entity not found.");
            }

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

        public override AsyncPageable<T> QueryAsync<T>(
            string? filter = null,
            int? maxPerPage = null,
            IEnumerable<string>? select = null,
            CancellationToken cancellationToken = default)
        {
            var values = entities.Values
                .Select(Clone)
                .Cast<T>()
                .ToArray();
            var page = Page<T>.FromValues(values, continuationToken: null, StubResponse.Instance);

            return AsyncPageable<T>.FromPages([page]);
        }

        private static PlatformEntity ToPlatformEntity<T>(T entity)
        {
            if (entity is not PlatformEntity platform)
            {
                throw new InvalidOperationException($"Unsupported entity type '{typeof(T).Name}'.");
            }

            return platform;
        }

        private static PlatformEntity Clone(PlatformEntity entity) =>
            new()
            {
                PartitionKey = entity.PartitionKey,
                RowKey = entity.RowKey,
                Timestamp = entity.Timestamp,
                ETag = entity.ETag,
                PlatformId = entity.PlatformId,
                Name = entity.Name,
                ReferenceKey = entity.ReferenceKey,
                Type = entity.Type,
                PublishSettingsJson = entity.PublishSettingsJson,
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
