using Azure.Data.Tables;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Logging;
using YTSkedy.AzureFunctions.Auth;
using YTSkedy.Infrastructure.CalendarEvents;
using YTSkedy.Infrastructure.Platforms;
using YTSkedy.Infrastructure.Templates;
using YTSkedy.Infrastructure.YouTube;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Templates;
using YTSkedy.Scheduling.Application.YouTube;

var builder = FunctionsApplication.CreateBuilder(args);

// Safe-logging contract (T033). These are the IdentityModel defaults; pin
// them explicitly so a debug session that flips ShowPII = true (a common
// "why is my token rejected?" debugging shortcut) cannot quietly land
// raw token contents in framework logs or in WWW-Authenticate challenges.
IdentityModelEventSource.ShowPII = false;
IdentityModelEventSource.LogCompleteSecurityArtifact = false;

builder.ConfigureFunctionsWebApplication();

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
    .AddOptions<YouTubeOptions>()
    .Bind(builder.Configuration.GetSection(YouTubeOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<YouTubeBroadcastOptions>()
    .Bind(builder.Configuration.GetSection(YouTubeBroadcastOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

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
{
    var connectionString =
        builder.Configuration["AzureStorage:ConnectionString"] ??
        builder.Configuration["AzureWebJobsStorage"];

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "Azure Table Storage connection string is not configured.");
    }

    var tableName = builder.Configuration["AzureStorage:CalendarEventsTableName"];
    if (string.IsNullOrWhiteSpace(tableName))
    {
        tableName = "CalendarEvents";
    }

    return new TableClient(connectionString, tableName);
});

builder.Services.AddScoped<CreateCalendarEventHandler>();
builder.Services.AddScoped<ListEventsHandler>();
builder.Services.AddScoped<GetCalendarEventHandler>();
builder.Services.AddScoped<UpdateCalendarEventHandler>();
builder.Services.AddScoped<DeleteCalendarEventHandler>();
builder.Services.AddScoped<AzureCalendarEventRepository>();
builder.Services.AddScoped<ICalendarEventRepository>(
    serviceProvider => serviceProvider.GetRequiredService<AzureCalendarEventRepository>());
builder.Services.AddScoped<ICalendarEventReader>(
    serviceProvider => serviceProvider.GetRequiredService<AzureCalendarEventRepository>());

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<YouTubeBroadcastAdapter>();
builder.Services.AddSingleton<IYouTubePublisher>(
    serviceProvider => serviceProvider.GetRequiredService<YouTubeBroadcastAdapter>());
builder.Services.AddSingleton<IYouTubeDeleter>(
    serviceProvider => serviceProvider.GetRequiredService<YouTubeBroadcastAdapter>());

// Templates persist in their own table bound through a keyed TableClient so the
// calendar-event TableClient registration above is untouched.
builder.Services.AddKeyedSingleton<TableClient>("templates", (_, _) =>
{
    var connectionString =
        builder.Configuration["AzureStorage:ConnectionString"] ??
        builder.Configuration["AzureWebJobsStorage"];

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "Azure Table Storage connection string is not configured.");
    }

    var tableName = builder.Configuration["AzureStorage:TemplatesTableName"];
    if (string.IsNullOrWhiteSpace(tableName))
    {
        tableName = "Templates";
    }

    return new TableClient(connectionString, tableName);
});

builder.Services.AddScoped<CreateTemplateHandler>();
builder.Services.AddScoped<UpdateTemplateHandler>();
builder.Services.AddScoped<DeleteTemplateHandler>();
builder.Services.AddScoped<ListTemplatesHandler>();
builder.Services.AddScoped<ListTemplateTokensHandler>();
builder.Services.AddScoped(serviceProvider =>
    new AzureTemplateRepository(
        serviceProvider.GetRequiredKeyedService<TableClient>("templates"),
        serviceProvider.GetRequiredService<TimeProvider>()));
builder.Services.AddScoped<ITemplateRepository>(
    serviceProvider => serviceProvider.GetRequiredService<AzureTemplateRepository>());
builder.Services.AddScoped<ITemplateReader>(
    serviceProvider => serviceProvider.GetRequiredService<AzureTemplateRepository>());

// Platforms persist in their own table bound through a keyed TableClient so the
// calendar-event and templates TableClient registrations above are untouched.
builder.Services.AddKeyedSingleton<TableClient>("platforms", (_, _) =>
{
    var connectionString =
        builder.Configuration["AzureStorage:ConnectionString"] ??
        builder.Configuration["AzureWebJobsStorage"];

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "Azure Table Storage connection string is not configured.");
    }

    var tableName = builder.Configuration["AzureStorage:PlatformsTableName"];
    if (string.IsNullOrWhiteSpace(tableName))
    {
        tableName = "Platforms";
    }

    return new TableClient(connectionString, tableName);
});

builder.Services.AddScoped<ListPlatformsHandler>();
builder.Services.AddScoped<GetPlatformHandler>();
builder.Services.AddScoped<CreatePlatformHandler>();
builder.Services.AddScoped<UpdatePlatformHandler>();
builder.Services.AddScoped<DeletePlatformHandler>();
builder.Services.AddScoped(serviceProvider =>
    new AzurePlatformRepository(
        serviceProvider.GetRequiredKeyedService<TableClient>("platforms"),
        serviceProvider.GetRequiredService<TimeProvider>()));
builder.Services.AddScoped<IPlatformRepository>(
    serviceProvider => serviceProvider.GetRequiredService<AzurePlatformRepository>());
builder.Services.AddScoped<IPlatformReader>(
    serviceProvider => serviceProvider.GetRequiredService<AzurePlatformRepository>());

builder.Build().Run();
