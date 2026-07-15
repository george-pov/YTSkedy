using Microsoft.Extensions.Hosting;
using YTSkedy.Scheduling.Application.Platforms.Publications;

namespace YTSkedy.AzureFunctions.Platforms.Publications;

public sealed class PublishExecutionScopeFactory(
    PublicationExecutionSettings settings,
    IHostApplicationLifetime hostLifetime,
    TimeProvider timeProvider) : IPublishExecutionScopeFactory
{
    public IPublishExecutionScope Create() =>
        new PublishExecutionScope(settings, hostLifetime.ApplicationStopping, timeProvider);

    private sealed class PublishExecutionScope : IPublishExecutionScope
    {
        private readonly PublicationExecutionSettings settings;
        private readonly CancellationToken hostStopping;
        private readonly TimeProvider timeProvider;
        private readonly CancellationTokenSource operationTimeout;
        private readonly CancellationTokenSource operation;

        public PublishExecutionScope(
            PublicationExecutionSettings settings,
            CancellationToken hostStopping,
            TimeProvider timeProvider)
        {
            this.settings = settings;
            this.hostStopping = hostStopping;
            this.timeProvider = timeProvider;
            operationTimeout = new CancellationTokenSource(settings.OperationTimeout, timeProvider);
            operation = CancellationTokenSource.CreateLinkedTokenSource(
                operationTimeout.Token,
                hostStopping);
        }

        public CancellationToken OperationToken => operation.Token;

        public PublishCancellationSource ClassifyCancellation()
        {
            if (hostStopping.IsCancellationRequested)
            {
                return PublishCancellationSource.HostShutdown;
            }

            return operationTimeout.IsCancellationRequested
                ? PublishCancellationSource.OperationTimeout
                : PublishCancellationSource.Unexpected;
        }

        public async Task<TResult> RunFinalizationAsync<TResult>(
            Func<CancellationToken, Task<TResult>> action)
        {
            ArgumentNullException.ThrowIfNull(action);

            using var finalization = new CancellationTokenSource(
                settings.FinalizationTimeout,
                timeProvider);
            return await action(finalization.Token);
        }

        public void Dispose()
        {
            operation.Dispose();
            operationTimeout.Dispose();
        }
    }
}
