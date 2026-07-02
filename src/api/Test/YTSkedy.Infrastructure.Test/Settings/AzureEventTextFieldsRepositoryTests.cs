using Azure;
using Azure.Core;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using YTSkedy.Infrastructure.Settings;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Infrastructure.Test.Settings;

public sealed class AzureEventTextFieldsRepositoryTests
{
    [Fact]
    public async Task GetAsync_MissingRow_ReturnsDefaultFields()
    {
        var repository = new AzureEventTextFieldsRepository(new InMemoryTableClient());

        var eventTextFields = await repository.GetAsync(CancellationToken.None);

        Assert.Equal(["text1", "text2"], eventTextFields.Fields.Select(field => field.FieldKey));
        Assert.Equal(
            [EventTextType.ShortText, EventTextType.LongText],
            eventTextFields.Fields.Select(field => field.Type));
    }

    [Fact]
    public async Task SaveAsync_Fields_UpsertsOneGenericApplicationSettingsRow()
    {
        var tableClient = new InMemoryTableClient();
        var repository = new AzureEventTextFieldsRepository(tableClient);
        var settings = new EventTextFields(
            [
                new EventTextField("Title", EventTextType.ShortText, 80),
                new EventTextField("Details", EventTextType.LongText, 3000)
            ]);

        await repository.SaveAsync(settings, CancellationToken.None);

        var entity = Assert.Single(tableClient.Entities.Values);
        Assert.True(tableClient.CreateIfNotExistsCalled);
        Assert.Equal(ApplicationSettingsKey.PartitionKey, entity.PartitionKey);
        Assert.Equal(ApplicationSettingsKey.EventTextFieldsRowKey, entity.RowKey);
        Assert.Contains("\"fieldKey\":\"text1\"", entity.ValueJson, StringComparison.Ordinal);
        Assert.Contains("\"fieldKey\":\"text2\"", entity.ValueJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SaveAsync_ThenGetAsync_ReturnsSavedNormalizedFields()
    {
        var tableClient = new InMemoryTableClient();
        var repository = new AzureEventTextFieldsRepository(tableClient);
        var settings = new EventTextFields(
            [new EventTextField("Episode", EventTextType.ShortText, 120)]);

        await repository.SaveAsync(settings, CancellationToken.None);
        var read = await repository.GetAsync(CancellationToken.None);

        var field = Assert.Single(read.Fields);
        Assert.Equal("text1", field.FieldKey);
        Assert.Equal("Episode", field.Label);
        Assert.Equal(EventTextType.ShortText, field.Type);
        Assert.Equal(120, field.MaxLength);
    }

    private sealed class InMemoryTableClient : TableClient
    {
        public Dictionary<(string PartitionKey, string RowKey), ApplicationSettingsEntity> Entities { get; } = [];

        public bool CreateIfNotExistsCalled { get; private set; }

        public override Task<Response<TableItem>> CreateIfNotExistsAsync(
            CancellationToken cancellationToken = default)
        {
            CreateIfNotExistsCalled = true;

            return Task.FromResult(Response.FromValue(
                TableModelFactory.TableItem("ApplicationSettings"),
                StubResponse.Instance));
        }

        public override Task<Response> UpsertEntityAsync<T>(
            T entity,
            TableUpdateMode mode = TableUpdateMode.Merge,
            CancellationToken cancellationToken = default)
        {
            var setting = ToApplicationSettingsEntity(entity);
            Entities[(setting.PartitionKey, setting.RowKey)] = Clone(setting);

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

        private static ApplicationSettingsEntity ToApplicationSettingsEntity<T>(T entity)
        {
            if (entity is not ApplicationSettingsEntity setting)
            {
                throw new InvalidOperationException($"Unsupported entity type '{typeof(T).Name}'.");
            }

            return setting;
        }

        private static ApplicationSettingsEntity Clone(ApplicationSettingsEntity entity) =>
            new()
            {
                PartitionKey = entity.PartitionKey,
                RowKey = entity.RowKey,
                Timestamp = entity.Timestamp,
                ETag = entity.ETag,
                ValueJson = entity.ValueJson
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
