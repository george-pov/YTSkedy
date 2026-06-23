namespace YTSkedy.Scheduling.Domain.Platforms;

/// <summary>
/// Provider family a <see cref="Platform"/> publishes through. The value is
/// immutable once a platform is created because it determines the publish
/// settings schema and the provider adapter. YouTube is the first implemented
/// provider; WordPress exists as a value before WordPress publishing is built.
/// </summary>
public enum PlatformType
{
    YouTube = 0,
    WordPress = 1
}
