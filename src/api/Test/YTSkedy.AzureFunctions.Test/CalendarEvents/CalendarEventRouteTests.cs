using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Azure.Functions.Worker;
using YTSkedy.AzureFunctions.CalendarEvents;

namespace YTSkedy.AzureFunctions.Test.CalendarEvents;

public sealed class CalendarEventRouteTests
{
    private const string EventIdRoutePattern = "^[0-9a-f]{32}$";

    [Fact]
    public void GetCalendarEvent_Route_ConstrainsCalendarEventId()
    {
        var trigger = GetHttpTrigger<CalendarEventsApi>(
            nameof(CalendarEventsApi.GetCalendarEventAsync));

        Assert.Equal(
            "calendar-events/{calendarEventId:regex(^[0-9a-f]{{32}}$)}",
            trigger.Route);
    }

    [Fact]
    public void StartSuggestion_Route_RemainsStatic()
    {
        var trigger = GetHttpTrigger<DefaultStartApi>(nameof(DefaultStartApi.GetAsync));

        Assert.Equal("calendar-events/start-suggestion", trigger.Route);
    }

    [Fact]
    public void CalendarEventIdRoutePattern_MatchesIdsButNotStartSuggestion()
    {
        Assert.True(
            Regex.IsMatch(
                "0123456789abcdef0123456789abcdef",
                EventIdRoutePattern,
                RegexOptions.CultureInvariant));
        Assert.False(
            Regex.IsMatch(
                "start-suggestion",
                EventIdRoutePattern,
                RegexOptions.CultureInvariant));
    }

    private static HttpTriggerAttribute GetHttpTrigger<TEndpoint>(string methodName) =>
        Assert.Single(
            typeof(TEndpoint)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public)!
                .GetParameters()[0]
                .GetCustomAttributes<HttpTriggerAttribute>());
}
