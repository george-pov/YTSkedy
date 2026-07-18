using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using YTSkedy.Infrastructure.CalendarEvents;
using YTSkedy.Infrastructure.Platforms;
using YTSkedy.Infrastructure.Settings;
using YTSkedy.Infrastructure.Templates;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.CalendarEvents.Thumbnails;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Platforms.Publications;
using YTSkedy.Scheduling.Application.Settings;
using YTSkedy.Scheduling.Application.Templates;

namespace YTSkedy.AzureFunctions.Configuration;

internal static class AzureStorageRegistration
{
    internal const string CalendarEventsClientKey = "calendarEvents";
    internal const string TemplatesClientKey = "templates";
    internal const string ApplicationSettingsClientKey = "applicationSettings";
    internal const string PlatformsClientKey = "platforms";
    internal const string PlatformPublicationsClientKey = "platformPublications";

    internal static IServiceCollection AddYTSkedyStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<AzureStorageOptions>()
            .Bind(configuration.GetSection(AzureStorageOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<
            IValidateOptions<AzureStorageOptions>,
            AzureStorageOptionsValidator>();

        services.AddSingleton(serviceProvider =>
            AzureStorageClientFactory.CreateTableServiceClient(
                serviceProvider.GetRequiredService<IOptions<AzureStorageOptions>>().Value));
        services.AddSingleton(serviceProvider =>
            AzureStorageClientFactory.CreateBlobServiceClient(
                serviceProvider.GetRequiredService<IOptions<AzureStorageOptions>>().Value));

        AddTableClient(
            services,
            CalendarEventsClientKey,
            options => options.CalendarEventsTableName);
        AddTableClient(
            services,
            TemplatesClientKey,
            options => options.TemplatesTableName);
        AddTableClient(
            services,
            ApplicationSettingsClientKey,
            options => options.ApplicationSettingsTableName);
        AddTableClient(
            services,
            PlatformsClientKey,
            options => options.PlatformsTableName);
        AddTableClient(
            services,
            PlatformPublicationsClientKey,
            options => options.PlatformPublicationsTableName);

        services.AddSingleton(serviceProvider =>
        {
            var options = serviceProvider
                .GetRequiredService<IOptions<AzureStorageOptions>>()
                .Value;
            return serviceProvider
                .GetRequiredService<BlobServiceClient>()
                .GetBlobContainerClient(options.ThumbnailsContainerName);
        });

        services.AddScoped(serviceProvider =>
            new AzureCalendarEventRepository(
                serviceProvider.GetRequiredKeyedService<TableClient>(
                    CalendarEventsClientKey),
                serviceProvider.GetRequiredService<TimeProvider>()));
        services.AddScoped<ICalendarEventModifier>(
            serviceProvider =>
                serviceProvider.GetRequiredService<AzureCalendarEventRepository>());
        services.AddScoped<ICalendarEventReader>(
            serviceProvider =>
                serviceProvider.GetRequiredService<AzureCalendarEventRepository>());
        services.AddScoped<IPublicationIndexWriter>(
            serviceProvider =>
                serviceProvider.GetRequiredService<AzureCalendarEventRepository>());
        services.AddScoped<ICalendarEventThumbnailModifier>(
            serviceProvider =>
                serviceProvider.GetRequiredService<AzureCalendarEventRepository>());
        services.AddScoped<ICalendarEventThumbnailReader>(
            serviceProvider =>
                serviceProvider.GetRequiredService<AzureCalendarEventRepository>());

        services.AddScoped<IThumbnailStore, AzureThumbnailStore>();

        services.AddScoped(serviceProvider =>
            new AzureTemplateRepository(
                serviceProvider.GetRequiredKeyedService<TableClient>(TemplatesClientKey),
                serviceProvider.GetRequiredService<TimeProvider>()));
        services.AddScoped<ITemplateModifier>(
            serviceProvider =>
                serviceProvider.GetRequiredService<AzureTemplateRepository>());
        services.AddScoped<ITemplateReader>(
            serviceProvider =>
                serviceProvider.GetRequiredService<AzureTemplateRepository>());

        services.AddScoped(serviceProvider =>
            new AzureCalendarEventDefaultsRepository(
                serviceProvider.GetRequiredKeyedService<TableClient>(
                    ApplicationSettingsClientKey)));
        services.AddScoped<IEventTextFieldsReader>(
            serviceProvider =>
                serviceProvider.GetRequiredService<AzureCalendarEventDefaultsRepository>());
        services.AddScoped<IStartDefaultsReader>(
            serviceProvider =>
                serviceProvider.GetRequiredService<AzureCalendarEventDefaultsRepository>());
        services.AddScoped<ICalendarEventDefaultsModifier>(
            serviceProvider =>
                serviceProvider.GetRequiredService<AzureCalendarEventDefaultsRepository>());

        services.AddScoped(serviceProvider =>
            new AzurePlatformRepository(
                serviceProvider.GetRequiredKeyedService<TableClient>(PlatformsClientKey),
                serviceProvider.GetRequiredService<TimeProvider>()));
        services.AddScoped<IPlatformModifier>(
            serviceProvider =>
                serviceProvider.GetRequiredService<AzurePlatformRepository>());
        services.AddScoped<IPlatformReader>(
            serviceProvider =>
                serviceProvider.GetRequiredService<AzurePlatformRepository>());

        services.AddScoped(serviceProvider =>
            new AzurePlatformPublicationRepository(
                serviceProvider.GetRequiredKeyedService<TableClient>(
                    PlatformPublicationsClientKey),
                serviceProvider.GetRequiredService<TimeProvider>()));
        services.AddScoped<IPublicationAttemptWriter>(
            serviceProvider =>
                serviceProvider.GetRequiredService<AzurePlatformPublicationRepository>());
        services.AddScoped<IPublicationThumbnailWriter>(
            serviceProvider =>
                serviceProvider.GetRequiredService<AzurePlatformPublicationRepository>());
        services.AddScoped<IPublicationCleanupWriter>(
            serviceProvider =>
                serviceProvider.GetRequiredService<AzurePlatformPublicationRepository>());
        services.AddScoped<IPublicationHistoryWriter>(
            serviceProvider =>
                serviceProvider.GetRequiredService<AzurePlatformPublicationRepository>());
        services.AddScoped<IPlatformPublicationReader>(
            serviceProvider =>
                serviceProvider.GetRequiredService<AzurePlatformPublicationRepository>());

        services.AddHostedService<AzureStorageInitializer>();

        return services;
    }

    private static void AddTableClient(
        IServiceCollection services,
        string key,
        Func<AzureStorageOptions, string> getTableName) =>
        services.AddKeyedSingleton<TableClient>(key, (serviceProvider, _) =>
        {
            var options = serviceProvider
                .GetRequiredService<IOptions<AzureStorageOptions>>()
                .Value;
            return serviceProvider
                .GetRequiredService<TableServiceClient>()
                .GetTableClient(getTableName(options));
        });
}
