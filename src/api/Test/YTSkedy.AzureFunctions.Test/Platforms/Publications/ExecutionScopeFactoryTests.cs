using Microsoft.Extensions.Hosting;
using YTSkedy.AzureFunctions.Platforms.Publications;
using YTSkedy.Scheduling.Application.Platforms.Publications;

namespace YTSkedy.AzureFunctions.Test.Platforms.Publications;

public sealed class ExecutionScopeFactoryTests
{
    private readonly Mock<IHostApplicationLifetime> _lifetime = new();

    [Fact]
    public async Task OperationDeadline_CancelsAndClassifiesTimeout()
    {
        var factory = CreateFactory(operationTimeout: TimeSpan.FromMilliseconds(30));
        using var scope = factory.Create();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Task.Delay(Timeout.InfiniteTimeSpan, scope.OperationToken));

        Assert.Equal(PublishCancellationSource.OperationTimeout, scope.ClassifyCancellation());
    }

    [Fact]
    public async Task HostStopping_CancelsAndClassifiesShutdown()
    {
        using var hostStopping = new CancellationTokenSource();
        var factory = CreateFactory(hostStopping: hostStopping);
        using var scope = factory.Create();

        hostStopping.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Task.Delay(Timeout.InfiniteTimeSpan, scope.OperationToken));

        Assert.Equal(PublishCancellationSource.HostShutdown, scope.ClassifyCancellation());
    }

    [Fact]
    public void UncanceledScope_ClassifiesUnexpected()
    {
        using var scope = CreateFactory().Create();

        Assert.Equal(PublishCancellationSource.Unexpected, scope.ClassifyCancellation());
    }

    [Fact]
    public async Task Finalization_UsesIndependentTimeout()
    {
        using var scope = CreateFactory(
            finalizationTimeout: TimeSpan.FromMilliseconds(30)).Create();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            scope.RunFinalizationAsync(async token =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
                return true;
            }));
    }

    [Fact]
    public async Task Dispose_StopsOperationDeadlineTimer()
    {
        var scope = CreateFactory(operationTimeout: TimeSpan.FromMilliseconds(30)).Create();
        var token = scope.OperationToken;

        scope.Dispose();
        await Task.Delay(80);

        Assert.False(token.IsCancellationRequested);
    }

    private PublishExecutionScopeFactory CreateFactory(
        TimeSpan? operationTimeout = null,
        TimeSpan? finalizationTimeout = null,
        CancellationTokenSource? hostStopping = null)
    {
        _lifetime
            .SetupGet(candidate => candidate.ApplicationStopping)
            .Returns(hostStopping?.Token ?? CancellationToken.None);

        return new PublishExecutionScopeFactory(
            new PublicationExecutionSettings(
                operationTimeout ?? TimeSpan.FromSeconds(10),
                finalizationTimeout ?? TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(30)),
            _lifetime.Object,
            TimeProvider.System);
    }
}
