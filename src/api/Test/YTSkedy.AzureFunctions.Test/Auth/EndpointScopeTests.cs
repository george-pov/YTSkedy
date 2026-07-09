using System.Reflection;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Identity.Web.Resource;
using YTSkedy.AzureFunctions.CalendarEvents;
using YTSkedy.AzureFunctions.Platforms;
using YTSkedy.AzureFunctions.Settings;
using YTSkedy.AzureFunctions.Templates;

namespace YTSkedy.AzureFunctions.Test.Auth;

public sealed class EndpointScopeTests
{
    private static readonly EndpointScopeCase[] ExpectedFunctionScopes =
    [
        new(typeof(CalendarEventsApi), nameof(CalendarEventsApi.CreateCalendarEventAsync), "CreateCalendarEvent", "CalendarEvents.Write"),
        new(typeof(CalendarEventsApi), nameof(CalendarEventsApi.ListAsync), "ListCalendarEvents", "CalendarEvents.Read"),
        new(typeof(CalendarEventsApi), nameof(CalendarEventsApi.GetCalendarEventAsync), "GetCalendarEvent", "CalendarEvents.Read"),
        new(typeof(CalendarEventsApi), nameof(CalendarEventsApi.UpdateCalendarEventAsync), "UpdateCalendarEvent", "CalendarEvents.Write"),
        new(typeof(CalendarEventsApi), nameof(CalendarEventsApi.DeleteCalendarEventAsync), "DeleteCalendarEvent", "CalendarEvents.Write"),
        new(typeof(CalendarEventThumbnailsApi), nameof(CalendarEventThumbnailsApi.UploadAsync), "UploadCalendarEventThumbnail", "CalendarEvents.Write"),
        new(typeof(CalendarEventThumbnailsApi), nameof(CalendarEventThumbnailsApi.GetAsync), "GetCalendarEventThumbnail", "CalendarEvents.Read"),
        new(typeof(CalendarEventThumbnailsApi), nameof(CalendarEventThumbnailsApi.DeleteAsync), "DeleteCalendarEventThumbnail", "CalendarEvents.Write"),
        new(typeof(DeletePlatformPublicationApi), nameof(DeletePlatformPublicationApi.DeleteAsync), "DeletePlatformPublication", "CalendarEvents.Write"),
        new(typeof(GetPublishingContentApi), nameof(GetPublishingContentApi.GetAsync), "GetPublishingContent", "CalendarEvents.Read"),
        new(typeof(PublishEventPlatformApi), nameof(PublishEventPlatformApi.PublishAsync), "PublishEventPlatform", "CalendarEvents.Write"),
        new(typeof(PlatformsApi), nameof(PlatformsApi.ListPlatformsAsync), "ListPlatforms", "CalendarEvents.Read"),
        new(typeof(PlatformsApi), nameof(PlatformsApi.GetPlatformAsync), "GetPlatform", "CalendarEvents.Read"),
        new(typeof(PlatformsApi), nameof(PlatformsApi.CreatePlatformAsync), "CreatePlatform", "CalendarEvents.Write"),
        new(typeof(PlatformsApi), nameof(PlatformsApi.UpdatePlatformAsync), "UpdatePlatform", "CalendarEvents.Write"),
        new(typeof(PlatformsApi), nameof(PlatformsApi.DeletePlatformAsync), "DeletePlatform", "CalendarEvents.Write"),
        new(typeof(TemplatesApi), nameof(TemplatesApi.ListTemplatesAsync), "ListTemplates", "CalendarEvents.Read"),
        new(typeof(TemplatesApi), nameof(TemplatesApi.CreateTemplateAsync), "CreateTemplate", "CalendarEvents.Write"),
        new(typeof(TemplatesApi), nameof(TemplatesApi.UpdateTemplateAsync), "UpdateTemplate", "CalendarEvents.Write"),
        new(typeof(TemplatesApi), nameof(TemplatesApi.DeleteTemplateAsync), "DeleteTemplate", "CalendarEvents.Write"),
        new(typeof(TemplatesApi), nameof(TemplatesApi.ListTemplateTokensAsync), "ListTemplateTokens", "CalendarEvents.Read"),
        new(typeof(EventTextFieldsApi), nameof(EventTextFieldsApi.Get), "GetEventTextFields", "CalendarEvents.Read"),
        new(typeof(EventTextFieldsApi), nameof(EventTextFieldsApi.Update), "UpdateEventTextFields", "CalendarEvents.Write")
    ];

    public static TheoryData<Type, string, string, string> FunctionScopes =>
        CreateTheoryData();

    private static TheoryData<Type, string, string, string> CreateTheoryData()
    {
        var data = new TheoryData<Type, string, string, string>();
        foreach (var endpoint in ExpectedFunctionScopes)
        {
            data.Add(
                endpoint.EndpointType,
                endpoint.MethodName,
                endpoint.FunctionName,
                endpoint.RequiredScope);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(FunctionScopes))]
    public void FunctionEndpoint_HasExpectedRequiredScope(
        Type endpointType,
        string methodName,
        string functionName,
        string expectedScope)
    {
        var method = EndpointMethod(endpointType, methodName);

        var function = Assert.Single(method.GetCustomAttributes<FunctionAttribute>());
        var scope = Assert.Single(method.GetCustomAttributes<RequiredScopeAttribute>());

        Assert.Equal(functionName, function.Name);
        Assert.Equal([expectedScope], scope.AcceptedScope ?? []);
    }

    [Fact]
    public void AllFunctionEndpoints_AreCoveredByScopeContract()
    {
        var expected = ExpectedFunctionScopes
            .Select(endpoint => (endpoint.EndpointType, endpoint.MethodName))
            .ToHashSet();

        var actual = typeof(CalendarEventsApi).Assembly
            .GetTypes()
            .Where(type => type.Namespace?.StartsWith(
                "YTSkedy.AzureFunctions",
                StringComparison.Ordinal) is true)
            .SelectMany(type => type.GetMethods(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic)
                .Where(method => method.GetCustomAttribute<FunctionAttribute>() is not null)
                .Select(method => (EndpointType: type, MethodName: method.Name)))
            .ToHashSet();

        Assert.Equal(expected.OrderBy(Key), actual.OrderBy(Key));
    }

    private static MethodInfo EndpointMethod(Type endpointType, string methodName) =>
        endpointType.GetMethod(methodName)
        ?? throw new InvalidOperationException(
            $"Endpoint method {endpointType.FullName}.{methodName} was not found.");

    private static string Key((Type EndpointType, string MethodName) endpoint) =>
        $"{endpoint.EndpointType.FullName}.{endpoint.MethodName}";

    private sealed record EndpointScopeCase(
        Type EndpointType,
        string MethodName,
        string FunctionName,
        string RequiredScope);
}
