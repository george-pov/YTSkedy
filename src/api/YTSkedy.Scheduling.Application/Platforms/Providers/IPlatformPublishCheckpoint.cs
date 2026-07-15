namespace YTSkedy.Scheduling.Application.Platforms.Providers;

public interface IPlatformPublishCheckpoint
{
    Task SaveExternalResourceIdAsync(
        string externalResourceId,
        CancellationToken cancellationToken);
}
