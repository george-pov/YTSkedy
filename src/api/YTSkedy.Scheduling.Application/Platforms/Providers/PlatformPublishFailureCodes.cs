namespace YTSkedy.Scheduling.Application.Platforms.Providers;

public static class PlatformPublishFailureCodes
{
    public const string ProviderFailure = "provider_failure";
    public const string ProviderTimeout = "provider_timeout";
    public const string ProviderCanceled = "provider_canceled";
    public const string ProviderValidationFailed = "provider_validation_failed";
    public const string ThumbnailLoadFailed = "thumbnail_load_failed";
    public const string WordPressRateLimited = "wordpress_rate_limited";
    public const string WordPressAuthenticationFailed = "wordpress_authentication_failed";
    public const string WordPressPermissionDenied = "wordpress_permission_denied";
    public const string WordPressRequestRejected = "wordpress_request_rejected";
    public const string WordPressProviderError = "wordpress_provider_error";
    public const string WordPressInvalidResponse = "wordpress_invalid_response";
    public const string WordPressDiscoveryFailed = "wordpress_discovery_failed";
}
