namespace YTSkedy.Infrastructure.IntegrationTest.TestSupport;

internal sealed class AzuriteFactAttribute : FactAttribute
{
    public AzuriteFactAttribute()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("YTSKEDY_RUN_AZURITE_TESTS"),
                "1",
                StringComparison.Ordinal))
        {
            Skip = "Set YTSKEDY_RUN_AZURITE_TESTS=1 to run Azurite contracts.";
        }
    }
}
