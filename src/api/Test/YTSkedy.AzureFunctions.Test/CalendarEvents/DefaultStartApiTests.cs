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
    private readonly Mock<IStartDefaultsReader> _startDefaults = new();
    private readonly Mock<ICalendarEventReader> _calendarEvents = new();
    private readonly DefaultStartApi _api;

    public DefaultStartApiTests()
    {
        _api = new DefaultStartApi(
            new GetDefaultStartHandler(
                _startDefaults.Object,
                _calendarEvents.Object,
                new FixedTimeProvider(DateTimeOffset.Parse("2026-07-12T16:00:00+00:00"))));
    }

    [Fact]
    public async Task GetAsync_ValidFallback_ReturnsFormattedSuggestion()
    {
        SetReaderResults(new StartDefaults(DayOfWeek.Sunday, new TimeOnly(10, 0), null));
        var request = new DefaultHttpContext().Request;
        request.QueryString = new QueryString("?fallbackTimeZoneId=America%2FVancouver");

        var result = await _api.GetAsync(request, CancellationToken.None);

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
        SetReaderResults(StartDefaults.Empty);
        var request = new DefaultHttpContext().Request;
        request.QueryString = new QueryString(query);

        var result = await _api.GetAsync(
            request,
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    private void SetReaderResults(StartDefaults defaults)
    {
        _startDefaults
            .Setup(reader => reader.GetAsync(CancellationToken.None))
            .ReturnsAsync(defaults);
        _calendarEvents
            .Setup(reader => reader.ListAsync(
                It.IsAny<CalendarEventMonthCriteria?>(),
                CancellationToken.None))
            .ReturnsAsync([]);
    }
}
