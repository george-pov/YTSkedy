using System.ComponentModel.DataAnnotations;
using YTSkedy.AzureFunctions.Configuration;

namespace YTSkedy.AzureFunctions.Test.Configuration;

public sealed class PublicationExecutionOptionsTests
{
    [Fact]
    public void Defaults_AreSafeAndMapToSettings()
    {
        var options = new PublicationExecutionOptions();

        Assert.Empty(Validate(options));
        var settings = options.ToSettings();
        Assert.Equal(TimeSpan.FromSeconds(120), settings.OperationTimeout);
        Assert.Equal(TimeSpan.FromSeconds(15), settings.FinalizationTimeout);
        Assert.Equal(TimeSpan.FromSeconds(300), settings.StaleAfter);
    }

    [Theory]
    [InlineData(0, 15, 300)]
    [InlineData(120, 0, 300)]
    [InlineData(120, 15, 0)]
    [InlineData(120, 15, 135)]
    [InlineData(120, 15, 134)]
    public void InvalidValues_FailValidation(
        int operationSeconds,
        int finalizationSeconds,
        int staleSeconds)
    {
        var options = new PublicationExecutionOptions
        {
            OperationTimeoutSeconds = operationSeconds,
            FinalizationTimeoutSeconds = finalizationSeconds,
            StaleAfterSeconds = staleSeconds
        };

        Assert.NotEmpty(Validate(options));
    }

    private static IReadOnlyList<ValidationResult> Validate(object value)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(value, new ValidationContext(value), results, true);
        return results;
    }
}
