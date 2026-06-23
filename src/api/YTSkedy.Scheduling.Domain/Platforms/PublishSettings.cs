namespace YTSkedy.Scheduling.Domain.Platforms;

/// <summary>
/// Non-secret settings used when publishing through a <see cref="Platform"/>.
/// Concrete settings are provider-specific (for example
/// <see cref="YouTubeSettings"/>) and the matching type is chosen by the
/// platform's <see cref="PlatformType"/>. Settings never carry secrets, OAuth
/// tokens, refresh tokens, client secrets, API keys, or raw authorization
/// headers; credential material is resolved outside application storage.
/// </summary>
public abstract record PublishSettings;
