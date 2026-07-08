using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using YTSkedy.Scheduling.Application.Platforms;

namespace YTSkedy.Infrastructure.WordPress;

public static class WordPressServiceExtensions
{
    private const string HttpClientName = "YTSkedy.WordPress";

    public static IServiceCollection AddWordPressPlatformAdapters(
        this IServiceCollection services)
    {
        services.AddHttpClient(HttpClientName);

        services.AddSingleton(serviceProvider =>
            new WordPressEndpointResolver(
                CreateHttpClient(serviceProvider),
                serviceProvider.GetRequiredService<ILogger<WordPressEndpointResolver>>()));

        services.AddSingleton(serviceProvider =>
            new WordPressPublisher(
                CreateHttpClient(serviceProvider),
                serviceProvider.GetRequiredService<WordPressEndpointResolver>(),
                serviceProvider.GetRequiredService<ILogger<WordPressPublisher>>()));
        services.AddSingleton<IPlatformPublisher>(
            serviceProvider => serviceProvider.GetRequiredService<WordPressPublisher>());

        services.AddSingleton(serviceProvider =>
            new WordPressPublicationDeleter(
                CreateHttpClient(serviceProvider),
                serviceProvider.GetRequiredService<WordPressEndpointResolver>(),
                serviceProvider.GetRequiredService<ILogger<WordPressPublicationDeleter>>()));
        services.AddSingleton<IPlatformPublicationDeleter>(
            serviceProvider => serviceProvider.GetRequiredService<WordPressPublicationDeleter>());

        return services;
    }

    private static HttpClient CreateHttpClient(IServiceProvider serviceProvider) =>
        serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName);
}
