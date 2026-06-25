namespace YTSkedy.Scheduling.Domain.Platforms;

/// <summary>
/// Settings used when publishing through a <see cref="Platform"/>. Concrete
/// settings are provider-specific (for example <see cref="YouTubeSettings"/>)
/// and the matching type is chosen by the platform's <see cref="PlatformType"/>.
/// Some provider settings can carry secrets for internal persistence. HTTP
/// responses and platform-publication snapshots must use redacted or sanitized
/// projections instead of returning or copying secret-bearing settings.
/// </summary>
public abstract record PublishSettings;
