using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using YTSkedy.AzureFunctions.Settings;
using YTSkedy.Scheduling.Application.Settings;
using YTSkedy.Scheduling.Domain.CalendarEvents;
using YTSkedy.TestSupport;

namespace YTSkedy.AzureFunctions.Test.Settings;

public sealed class StartDefaultsApiTests
{
    [Fact]
    public async Task GetAsync_EmptyDefaults_ReturnsNullableContract()
    {
        var result = await CreateApi(new FakeStartDefaultsStore(StartDefaults.Empty)).GetAsync(
            new DefaultHttpContext().Request,
            CancellationToken.None);

        var response = Assert.IsType<StartDefaultsResponse>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Null(response.DayOfWeek);
        Assert.Null(response.LocalTime);
        Assert.Null(response.TimeZoneId);
    }

    [Fact]
    public async Task UpdateAsync_FullRequest_SavesAndReturnsNormalizedStrings()
    {
        var store = new FakeStartDefaultsStore(StartDefaults.Empty);
        var result = await CreateApi(store).UpdateAsync(
            HttpRequestFactory.WithBody(
                """{"dayOfWeek":"Sunday","localTime":"09:05","timeZoneId":"America/Vancouver"}"""),
            CancellationToken.None);

        var response = Assert.IsType<StartDefaultsResponse>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal("Sunday", response.DayOfWeek);
        Assert.Equal("09:05", response.LocalTime);
        Assert.Equal("America/Vancouver", response.TimeZoneId);
        Assert.Equal(DayOfWeek.Sunday, store.Saved!.DayOfWeek);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"dayOfWeek\":null,\"localTime\":null,\"timeZoneId\":null}")]
    public async Task UpdateAsync_OmittedOrNullValues_ClearsDefaults(string json)
    {
        var store = new FakeStartDefaultsStore(
            new StartDefaults(DayOfWeek.Monday, new TimeOnly(10, 0), "UTC"));

        var result = await CreateApi(store).UpdateAsync(
            HttpRequestFactory.WithBody(json),
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StartDefaults.Empty, store.Saved);
    }

    [Theory]
    [InlineData("{\"dayOfWeek\":\"sunday\"}")]
    [InlineData("{\"localTime\":\"9:05\"}")]
    [InlineData("{\"timeZoneId\":\"Unknown/Zone\"}")]
    [InlineData("not-json")]
    public async Task UpdateAsync_InvalidRequest_ReturnsBadRequest(string json)
    {
        var result = await CreateApi(new FakeStartDefaultsStore(StartDefaults.Empty)).UpdateAsync(
            HttpRequestFactory.WithBody(json),
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    private static StartDefaultsApi CreateApi(FakeStartDefaultsStore store) =>
        new(new GetStartDefaultsHandler(store), new UpdateStartDefaultsHandler(store));

    private sealed class FakeStartDefaultsStore(StartDefaults current) :
        IStartDefaultsReader,
        IStartDefaultsModifier
    {
        public StartDefaults? Saved { get; private set; }

        public Task<StartDefaults> GetAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Saved ?? current);

        public Task SaveAsync(StartDefaults startDefaults, CancellationToken cancellationToken)
        {
            Saved = startDefaults;
            return Task.CompletedTask;
        }
    }
}
