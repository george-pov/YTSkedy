using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using YTSkedy.AzureFunctions.CalendarEvents;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.CalendarEvents.Starts;
using YTSkedy.Scheduling.Application.Settings;
using YTSkedy.Scheduling.Domain.CalendarEvents;
using YTSkedy.TestSupport;

namespace YTSkedy.AzureFunctions.Test.CalendarEvents;

public sealed class DefaultStartApiTests
{
    [Fact]
    public async Task GetAsync_ValidFallback_ReturnsFormattedSuggestion()
    {
        var request = new DefaultHttpContext().Request;
        request.QueryString = new QueryString("?fallbackTimeZoneId=America%2FVancouver");

        var result = await CreateApi(
                new StartDefaults(DayOfWeek.Sunday, new TimeOnly(10, 0), null))
            .GetAsync(request, CancellationToken.None);

        var response = Assert.IsType<DefaultStartResponse>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal("2026-07-12", response.LocalDate);
        Assert.Equal("10:00", response.LocalTime);
        Assert.Equal("America/Vancouver", response.TimeZoneId);
    }

    [Theory]
    [InlineData("?fallbackTimeZoneId=")]
    [InlineData("?fallbackTimeZoneId=Unknown%2FZone")]
    [InlineData("?fallbackTimeZoneId=UTC&fallbackTimeZoneId=UTC")]
    public async Task GetAsync_InvalidFallback_ReturnsBadRequest(string query)
    {
        var request = new DefaultHttpContext().Request;
        request.QueryString = new QueryString(query);

        var result = await CreateApi(StartDefaults.Empty).GetAsync(
            request,
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    private static DefaultStartApi CreateApi(StartDefaults defaults)
    {
        var startDefaults = new Mock<IStartDefaultsReader>();
        startDefaults
            .Setup(reader => reader.GetAsync(CancellationToken.None))
            .ReturnsAsync(defaults);
        var calendarEvents = new Mock<ICalendarEventReader>();
        calendarEvents
            .Setup(reader => reader.ListAsync(
                It.IsAny<CalendarEventMonthCriteria?>(),
                CancellationToken.None))
            .ReturnsAsync([]);

        return new DefaultStartApi(
            new GetDefaultStartHandler(
                startDefaults.Object,
                calendarEvents.Object,
                new FixedTimeProvider(DateTimeOffset.Parse("2026-07-12T16:00:00+00:00"))));
    }
}
