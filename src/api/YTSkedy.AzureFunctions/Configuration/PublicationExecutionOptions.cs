using System.ComponentModel.DataAnnotations;
using YTSkedy.Scheduling.Application.Platforms.Publications;

namespace YTSkedy.AzureFunctions.Configuration;

public sealed class PublicationExecutionOptions : IValidatableObject
{
    public const string SectionName = "PublicationExecution";

    [Range(1, int.MaxValue)]
    public int OperationTimeoutSeconds { get; init; } = 120;

    [Range(1, int.MaxValue)]
    public int FinalizationTimeoutSeconds { get; init; } = 15;

    [Range(1, int.MaxValue)]
    public int StaleAfterSeconds { get; init; } = 300;

    public PublicationExecutionSettings ToSettings() =>
        new(
            TimeSpan.FromSeconds(OperationTimeoutSeconds),
            TimeSpan.FromSeconds(FinalizationTimeoutSeconds),
            TimeSpan.FromSeconds(StaleAfterSeconds));

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StaleAfterSeconds <= (long)OperationTimeoutSeconds + FinalizationTimeoutSeconds)
        {
            yield return new ValidationResult(
                $"{nameof(StaleAfterSeconds)} must be greater than " +
                $"{nameof(OperationTimeoutSeconds)} plus {nameof(FinalizationTimeoutSeconds)}.",
                [nameof(StaleAfterSeconds)]);
        }
    }
}
