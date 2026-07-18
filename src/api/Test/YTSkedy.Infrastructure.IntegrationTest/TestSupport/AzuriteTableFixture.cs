using Azure;
using Azure.Data.Tables;

namespace YTSkedy.Infrastructure.IntegrationTest.TestSupport;

public sealed class AzuriteTableFixture : IAsyncLifetime
{
    private const string ConnectionString = "UseDevelopmentStorage=true";
    private static readonly TimeSpan ReadinessTimeout = TimeSpan.FromSeconds(30);
    private readonly HashSet<string> _createdTableNames = [];

    public TableServiceClient ServiceClient { get; } = new(ConnectionString);

    public async Task InitializeAsync()
    {
        if (!IsEnabled())
        {
            return;
        }

        using var timeout = new CancellationTokenSource(ReadinessTimeout);
        while (true)
        {
            try
            {
                await ServiceClient.GetPropertiesAsync(timeout.Token);
                return;
            }
            catch (RequestFailedException) when (!timeout.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250), timeout.Token);
            }
        }
    }

    public async Task DisposeAsync()
    {
        if (!IsEnabled())
        {
            return;
        }

        foreach (var tableName in _createdTableNames)
        {
            await ServiceClient.DeleteTableAsync(tableName);
        }
    }

    public async Task<TableClient> CreateTableAsync(string prefix)
    {
        var tableName = NewTableName(prefix);
        await ServiceClient.CreateTableAsync(tableName);
        _createdTableNames.Add(tableName);
        return ServiceClient.GetTableClient(tableName);
    }

    public TableClient MissingTable(string prefix) =>
        ServiceClient.GetTableClient(NewTableName(prefix));

    private static bool IsEnabled() =>
        string.Equals(
            Environment.GetEnvironmentVariable("YTSKEDY_RUN_AZURITE_TESTS"),
            "1",
            StringComparison.Ordinal);

    private static string NewTableName(string prefix)
    {
        var lettersOnly = new string(prefix.Where(char.IsLetterOrDigit).ToArray());
        var safePrefix = string.IsNullOrEmpty(lettersOnly) || !char.IsLetter(lettersOnly[0])
            ? $"T{lettersOnly}"
            : lettersOnly;
        return $"{safePrefix[..Math.Min(safePrefix.Length, 20)]}{Guid.NewGuid():N}";
    }
}
