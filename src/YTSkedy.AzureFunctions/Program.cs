using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using YTSkedy.Infrastructure.CalendarEvents;
using YTSkedy.Scheduling.Application.CalendarEvents;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

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

builder.Services.AddScoped<CreateEventHandler>();
builder.Services.AddScoped<ICalendarEventRepository, AzureCalendarEventRepository>();

builder.Build().Run();
