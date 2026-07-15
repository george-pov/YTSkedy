namespace YTSkedy.Scheduling.Application.Platforms.Publications;

public sealed record PublicationExecutionSettings(
    TimeSpan OperationTimeout,
    TimeSpan FinalizationTimeout,
    TimeSpan StaleAfter);
