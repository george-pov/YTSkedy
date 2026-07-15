namespace YTSkedy.Scheduling.Application.Platforms.Publications;

public interface IPublishExecutionScope : IDisposable
{
    CancellationToken OperationToken { get; }

    PublishCancellationSource ClassifyCancellation();

    Task<TResult> RunFinalizationAsync<TResult>(
        Func<CancellationToken, Task<TResult>> action);
}
