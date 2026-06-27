using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms;

public sealed record CreatePlatformCommand(
    string Name,
    PlatformType Type,
    PublishSettings PublishSettings,
    string? ReferenceKey = null);
