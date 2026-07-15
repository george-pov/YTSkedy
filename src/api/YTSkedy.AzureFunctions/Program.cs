using Azure.Data.Tables;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Logging;
using YTSkedy.AzureFunctions.Auth;
using YTSkedy.AzureFunctions.Configuration;
using YTSkedy.AzureFunctions.Http;
using YTSkedy.AzureFunctions.Platforms.Publications;
using YTSkedy.Infrastructure.CalendarEvents;
using YTSkedy.Infrastructure.Platforms;
using YTSkedy.Infrastructure.Settings;
using YTSkedy.Infrastructure.Templates;
using YTSkedy.Infrastructure.WordPress;
using YTSkedy.Infrastructure.YouTube;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.CalendarEvents.Starts;
using YTSkedy.Scheduling.Application.CalendarEvents.Thumbnails;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Platforms.Content;
using YTSkedy.Scheduling.Application.Platforms.Providers;
using YTSkedy.Scheduling.Application.Platforms.PublicationThumbnails;
using YTSkedy.Scheduling.Application.Platforms.Publications;
using YTSkedy.Scheduling.Application.Platforms.WordPressCategory;
using YTSkedy.Scheduling.Application.Settings;
using YTSkedy.Scheduling.Application.Templates;

var builder = FunctionsApplication.CreateBuilder(args);

// Safe-logging contract (T033). These are the IdentityModel defaults; pin
// them explicitly so a debug session that flips ShowPII = true (a common
// "why is my token rejected?" debugging shortcut) cannot quietly land
// raw token contents in framework logs or in WWW-Authenticate challenges.
IdentityModelEventSource.ShowPII = false;
IdentityModelEventSource.LogCompleteSecurityArtifact = false;

builder.ConfigureFunctionsWebApplication();

builder.UseMiddleware<RequestCancellationMiddleware>();
builder.UseMiddleware<BearerTokenMiddleware>();
builder.UseMiddleware<AuthorizationMiddleware>();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services
    .AddOptions<AuthOptions>()
    .Bind(builder.Configuration.GetSection(AuthOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<PublicationExecutionOptions>()
    .Bind(builder.Configuration.GetSection(PublicationExecutionOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton(serviceProvider =>
    serviceProvider
        .GetRequiredService<IOptions<PublicationExecutionOptions>>()
        .Value
        .ToSettings());
builder.Services.AddSingleton<IPublishExecutionScopeFactory, PublishExecutionScopeFactory>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(
        builder.Configuration,
        configSectionName: AuthOptions.SectionName);

builder.Services.AddAuthorization();

// Strip token validation internals from any challenge the handler might
// emit (T030). Defense in depth: the worker middleware uses AuthenticateAsync
// and does not invoke ChallengeAsync today, but suppressing error details
// here means future code paths cannot leak "token expired at...",
// "audience mismatch", etc. via the `WWW-Authenticate` header.
builder.Services.PostConfigure<JwtBearerOptions>(
    JwtBearerDefaults.AuthenticationScheme,
    options =>
    {
        options.IncludeErrorDetails = false;
    });

// Entra External ID quirk: the authority host uses the tenant short subdomain
// (e.g. `<tenant>.ciamlogin.com`) while the `iss` claim uses the tenant-GUID
// subdomain. When Auth:Issuer is set, pin it on TokenValidationParameters so
// Microsoft.Identity.Web's derived issuer does not reject valid tokens.
var configuredIssuer = builder.Configuration[$"{AuthOptions.SectionName}:Issuer"];
if (!string.IsNullOrWhiteSpace(configuredIssuer))
{
    builder.Services.PostConfigure<JwtBearerOptions>(
        JwtBearerDefaults.AuthenticationScheme,
        options =>
        {
            options.TokenValidationParameters.ValidIssuer = configuredIssuer;
        });
}

builder.Services.AddSingleton(_ =>
    AzureStorageClientFactory.CreateTableClient(
        builder.Configuration,
        "AzureStorage:CalendarEventsTableName",
        "CalendarEvents"));

builder.Services.AddScoped<CreateCalendarEventHandler>();
builder.Services.AddScoped<ListEventsHandler>();
builder.Services.AddScoped<GetCalendarEventDetailsHandler>();
builder.Services.AddScoped<CalendarEventPublicationLock>();
builder.Services.AddScoped<UpdateCalendarEventHandler>();
builder.Services.AddScoped<DeleteCalendarEventHandler>();
builder.Services.AddScoped<GetDefaultStartHandler>();
builder.Services.AddScoped<UploadThumbnailHandler>();
builder.Services.AddScoped<GetThumbnailHandler>();
builder.Services.AddScoped<DeleteThumbnailHandler>();
builder.Services.AddScoped<AzureCalendarEventRepository>();
builder.Services.AddScoped<ICalendarEventModifier>(
    serviceProvider => serviceProvider.GetRequiredService<AzureCalendarEventRepository>());
builder.Services.AddScoped<ICalendarEventReader>(
    serviceProvider => serviceProvider.GetRequiredService<AzureCalendarEventRepository>());
builder.Services.AddScoped<IPublicationIndexWriter>(
    serviceProvider => serviceProvider.GetRequiredService<AzureCalendarEventRepository>());
builder.Services.AddScoped<ICalendarEventThumbnailModifier>(
    serviceProvider => serviceProvider.GetRequiredService<AzureCalendarEventRepository>());
builder.Services.AddScoped<ICalendarEventThumbnailReader>(
    serviceProvider => serviceProvider.GetRequiredService<AzureCalendarEventRepository>());

builder.Services.AddSingleton(_ =>
    AzureStorageClientFactory.CreateBlobContainerClient(
        builder.Configuration,
        AzureStorageConfiguration.GetThumbnailsContainerName(builder.Configuration)));

builder.Services.AddScoped<IThumbnailStore, AzureThumbnailStore>();

builder.Services.AddSingleton(TimeProvider.System);

// Templates persist in their own table bound through a keyed TableClient so the
// calendar-event TableClient registration above is untouched.
builder.Services.AddKeyedSingleton<TableClient>("templates", (_, _) =>
    AzureStorageClientFactory.CreateTableClient(
        builder.Configuration,
        "AzureStorage:TemplatesTableName",
        "Templates"));

builder.Services.AddScoped<CreateTemplateHandler>();
builder.Services.AddScoped<UpdateTemplateHandler>();
builder.Services.AddScoped<DeleteTemplateHandler>();
builder.Services.AddScoped<ListTemplatesHandler>();
builder.Services.AddScoped<ListTemplateTokensHandler>();
builder.Services.AddScoped(serviceProvider =>
    new AzureTemplateRepository(
        serviceProvider.GetRequiredKeyedService<TableClient>("templates"),
        serviceProvider.GetRequiredService<TimeProvider>()));
builder.Services.AddScoped<ITemplateModifier>(
    serviceProvider => serviceProvider.GetRequiredService<AzureTemplateRepository>());
builder.Services.AddScoped<ITemplateReader>(
    serviceProvider => serviceProvider.GetRequiredService<AzureTemplateRepository>());

// Application-owned settings persist in one generic settings table. Individual
// settings are rows inside that table, so adding event text fields does not add
// a dedicated EventTextFields table.
builder.Services.AddKeyedSingleton<TableClient>("applicationSettings", (_, _) =>
    AzureStorageClientFactory.CreateTableClient(
        builder.Configuration,
        "AzureStorage:ApplicationSettingsTableName",
        "ApplicationSettings"));

builder.Services.AddScoped<GetCalendarEventDefaultsHandler>();
builder.Services.AddScoped<UpdateCalendarEventDefaultsHandler>();
builder.Services.AddScoped(serviceProvider =>
    new AzureEventTextFieldsRepository(
        serviceProvider.GetRequiredKeyedService<TableClient>("applicationSettings")));
builder.Services.AddScoped<IEventTextFieldsReader>(
    serviceProvider => serviceProvider.GetRequiredService<AzureEventTextFieldsRepository>());
builder.Services.AddScoped(serviceProvider =>
    new AzureStartDefaultsRepository(
        serviceProvider.GetRequiredKeyedService<TableClient>("applicationSettings")));
builder.Services.AddScoped<IStartDefaultsReader>(
    serviceProvider => serviceProvider.GetRequiredService<AzureStartDefaultsRepository>());
builder.Services.AddScoped(serviceProvider =>
    new AzureCalendarEventDefaultsRepository(
        serviceProvider.GetRequiredKeyedService<TableClient>("applicationSettings")));
builder.Services.AddScoped<ICalendarEventDefaultsModifier>(
    serviceProvider => serviceProvider.GetRequiredService<AzureCalendarEventDefaultsRepository>());

// Platforms persist in their own table bound through a keyed TableClient so the
// calendar-event and templates TableClient registrations above are untouched.
builder.Services.AddKeyedSingleton<TableClient>("platforms", (_, _) =>
    AzureStorageClientFactory.CreateTableClient(
        builder.Configuration,
        "AzureStorage:PlatformsTableName",
        "Platforms"));

builder.Services.AddScoped<ListPlatformsHandler>();
builder.Services.AddScoped<GetPlatformHandler>();
builder.Services.AddScoped<CreatePlatformHandler>();
builder.Services.AddScoped<UpdatePlatformHandler>();
builder.Services.AddScoped<DeletePlatformHandler>();
builder.Services.AddScoped<CategoryListHandler>();
builder.Services.AddScoped<PublishingContentRenderer>();
builder.Services.AddScoped<GetPublishingContentHandler>();
builder.Services.AddScoped(serviceProvider =>
    new AzurePlatformRepository(
        serviceProvider.GetRequiredKeyedService<TableClient>("platforms"),
        serviceProvider.GetRequiredService<TimeProvider>()));
builder.Services.AddScoped<IPlatformModifier>(
    serviceProvider => serviceProvider.GetRequiredService<AzurePlatformRepository>());
builder.Services.AddScoped<IPlatformReader>(
    serviceProvider => serviceProvider.GetRequiredService<AzurePlatformRepository>());

// Platform publications persist in their own table bound through a keyed
// TableClient so the calendar-event, templates, and platforms TableClient
// registrations above are untouched.
builder.Services.AddKeyedSingleton<TableClient>("platformPublications", (_, _) =>
    AzureStorageClientFactory.CreateTableClient(
        builder.Configuration,
        "AzureStorage:PlatformPublicationsTableName",
        "PlatformPublications"));

builder.Services.AddScoped(serviceProvider =>
    new AzurePlatformPublicationRepository(
        serviceProvider.GetRequiredKeyedService<TableClient>("platformPublications"),
        serviceProvider.GetRequiredService<TimeProvider>()));
builder.Services.AddScoped<IPublicationAttemptWriter>(
    serviceProvider => serviceProvider.GetRequiredService<AzurePlatformPublicationRepository>());
builder.Services.AddScoped<IPublicationThumbnailWriter>(
    serviceProvider => serviceProvider.GetRequiredService<AzurePlatformPublicationRepository>());
builder.Services.AddScoped<IPublicationCleanupWriter>(
    serviceProvider => serviceProvider.GetRequiredService<AzurePlatformPublicationRepository>());
builder.Services.AddScoped<IPublicationHistoryWriter>(
    serviceProvider => serviceProvider.GetRequiredService<AzurePlatformPublicationRepository>());
builder.Services.AddScoped<IPlatformPublicationReader>(
    serviceProvider => serviceProvider.GetRequiredService<AzurePlatformPublicationRepository>());

// Platform provider adapters are selected by platform type and use settings
// stored on the platform row.
const string wordPressHttpClientName = "YTSkedy.WordPress";
builder.Services.AddHttpClient(
    wordPressHttpClientName,
    (serviceProvider, client) =>
    {
        client.Timeout = serviceProvider
            .GetRequiredService<PublicationExecutionSettings>()
            .OperationTimeout;
    });
builder.Services.AddSingleton(serviceProvider =>
    new WordPressEndpointResolver(
        serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(wordPressHttpClientName),
        serviceProvider.GetRequiredService<ILogger<WordPressEndpointResolver>>()));
builder.Services.AddSingleton<IYouTubePublishClientFactory, YouTubePublishClientFactory>();
builder.Services.AddSingleton<IPlatformPublisher, YouTubePublisher>();
builder.Services.AddSingleton<IPlatformPublicationDeleter, YouTubePublicationDeleter>();
builder.Services.AddSingleton(serviceProvider =>
    new WordPressPublisher(
        serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(wordPressHttpClientName),
        serviceProvider.GetRequiredService<WordPressEndpointResolver>(),
        serviceProvider.GetRequiredService<TimeProvider>(),
        serviceProvider.GetRequiredService<ILogger<WordPressPublisher>>()));
builder.Services.AddSingleton<IPlatformPublisher>(
    serviceProvider => serviceProvider.GetRequiredService<WordPressPublisher>());
builder.Services.AddSingleton(serviceProvider =>
    new WordPressCategoryReader(
        serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(wordPressHttpClientName),
        serviceProvider.GetRequiredService<WordPressEndpointResolver>(),
        serviceProvider.GetRequiredService<ILogger<WordPressCategoryReader>>()));
builder.Services.AddSingleton<ICategoryReader>(
    serviceProvider => serviceProvider.GetRequiredService<WordPressCategoryReader>());
builder.Services.AddSingleton(serviceProvider =>
    new WordPressPublicationDeleter(
        serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(wordPressHttpClientName),
        serviceProvider.GetRequiredService<WordPressEndpointResolver>(),
        serviceProvider.GetRequiredService<ILogger<WordPressPublicationDeleter>>()));
builder.Services.AddSingleton<IPlatformPublicationDeleter>(
    serviceProvider => serviceProvider.GetRequiredService<WordPressPublicationDeleter>());
builder.Services.AddSingleton<IPlatformTypeAdapterSelector<IPlatformPublisher>,
    PlatformTypeAdapterSelector<IPlatformPublisher>>();
builder.Services.AddSingleton<IThumbnailPublisher, YouTubeThumbnailPublisher>();
builder.Services.AddSingleton<IPlatformTypeAdapterSelector<IThumbnailPublisher>,
    PlatformTypeAdapterSelector<IThumbnailPublisher>>();
builder.Services.AddScoped<PublicationThumbnailApplier>();
builder.Services.AddScoped<PublicationIndexUpdater>();
builder.Services.AddScoped<PublishHandler>();
builder.Services.AddScoped<RecoverPublicationHandler>();

// Platform publication cleanup: providers are selected by platform type and use
// current settings stored on the platform row.
builder.Services.AddSingleton<IPlatformTypeAdapterSelector<IPlatformPublicationDeleter>,
    PlatformTypeAdapterSelector<IPlatformPublicationDeleter>>();
builder.Services.AddScoped<DeletePublicationHandler>();

builder.Build().Run();
