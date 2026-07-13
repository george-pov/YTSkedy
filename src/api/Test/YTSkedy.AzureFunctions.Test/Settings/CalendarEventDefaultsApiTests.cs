using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using YTSkedy.AzureFunctions.Settings;
using YTSkedy.Scheduling.Application.Settings;
using YTSkedy.Scheduling.Domain.CalendarEvents;
using YTSkedy.TestSupport;

namespace YTSkedy.AzureFunctions.Test.Settings;

public sealed class CalendarEventDefaultsApiTests
{
    [Fact]
    public async Task GetAsync_CurrentDefaults_ReturnsBothSettingsSections()
    {
        var fields = new EventTextFields(
            [new EventTextField("Episode", EventTextType.ShortText, 80)]);
        var startDefaults = new StartDefaults(
            DayOfWeek.Sunday,
            new TimeOnly(9, 5),
            "America/Vancouver");
        var api = CreateApi(fields, startDefaults, new FakeDefaultsModifier());

        var result = await api.GetAsync(
            new DefaultHttpContext().Request,
            CancellationToken.None);

        var response = Assert.IsType<CalendarEventDefaultsResponse>(
            Assert.IsType<OkObjectResult>(result).Value);
        var field = Assert.Single(response.EventTextFields.Fields);
        Assert.Equal("text1", field.FieldKey);
        Assert.Equal("Episode", field.Label);
        Assert.Equal("ShortText", field.Type);
        Assert.Equal(80, field.MaxLength);
        Assert.Equal("Sunday", response.StartDefaults.DayOfWeek);
        Assert.Equal("09:05", response.StartDefaults.LocalTime);
        Assert.Equal("America/Vancouver", response.StartDefaults.TimeZoneId);
    }

    [Fact]
    public async Task UpdateAsync_ValidRequest_SavesAndReturnsNormalizedDefaults()
    {
        var modifier = new FakeDefaultsModifier();
        var api = CreateApi(EventTextFields.Default, StartDefaults.Empty, modifier);
        var request = HttpRequestFactory.WithBody("""
            {
              "eventTextFields": {
                "fields": [
                  {
                    "fieldKey": "text7",
                    "label": "Episode",
                    "type": "ShortText",
                    "maxLength": 80
                  },
                  {
                    "fieldKey": "text9",
                    "label": "Notes",
                    "type": "LongText",
                    "maxLength": 1000
                  }
                ]
              },
              "startDefaults": {
                "dayOfWeek": "Sunday",
                "localTime": "09:05",
                "timeZoneId": "America/Vancouver"
              }
            }
            """);

        var result = await api.UpdateAsync(request, CancellationToken.None);

        var response = Assert.IsType<CalendarEventDefaultsResponse>(
            Assert.IsType<OkObjectResult>(result).Value);
        Assert.NotNull(modifier.Saved);
        Assert.Equal(
            ["text1", "text2"],
            modifier.Saved!.EventTextFields.Fields.Select(field => field.FieldKey));
        Assert.Equal(
            ["text1", "text2"],
            response.EventTextFields.Fields.Select(field => field.FieldKey));
        Assert.Equal(
            ["Episode", "Notes"],
            response.EventTextFields.Fields.Select(field => field.Label));
        Assert.Equal(DayOfWeek.Sunday, modifier.Saved.StartDefaults.DayOfWeek);
        Assert.Equal("09:05", response.StartDefaults.LocalTime);
    }

    [Fact]
    public async Task UpdateAsync_AllNullStartDefaults_ClearsDefaults()
    {
        var modifier = new FakeDefaultsModifier();
        var api = CreateApi(EventTextFields.Default, StartDefaults.Empty, modifier);

        var result = await api.UpdateAsync(
            HttpRequestFactory.WithBody("""
                {
                  "eventTextFields": {
                    "fields": [
                      {
                        "fieldKey": "text1",
                        "label": "Title",
                        "type": "ShortText",
                        "maxLength": 50
                      }
                    ]
                  },
                  "startDefaults": {
                    "dayOfWeek": null,
                    "localTime": null,
                    "timeZoneId": null
                  }
                }
                """),
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StartDefaults.Empty, modifier.Saved!.StartDefaults);
    }

    [Theory]
    [InlineData("""
        {
          "eventTextFields": { "fields": [] },
          "startDefaults": {}
        }
        """)]
    [InlineData("""
        {
          "eventTextFields": {
            "fields": [
              { "fieldKey": "text1", "label": "", "type": "ShortText", "maxLength": 50 }
            ]
          },
          "startDefaults": {}
        }
        """)]
    [InlineData("""
        {
          "eventTextFields": {
            "fields": [
              { "fieldKey": "text1", "label": "Title", "type": "Unknown", "maxLength": 50 }
            ]
          },
          "startDefaults": {}
        }
        """)]
    [InlineData("""
        {
          "eventTextFields": {
            "fields": [
              { "fieldKey": "text1", "label": "Title", "type": "ShortText", "maxLength": 0 }
            ]
          },
          "startDefaults": {}
        }
        """)]
    public async Task UpdateAsync_InvalidFieldList_ReturnsBadRequest(string json)
    {
        var modifier = new FakeDefaultsModifier();

        var result = await CreateApi(EventTextFields.Default, StartDefaults.Empty, modifier)
            .UpdateAsync(HttpRequestFactory.WithBody(json), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Null(modifier.Saved);
    }

    [Theory]
    [InlineData("sunday", null, null)]
    [InlineData(null, "9:05", null)]
    [InlineData(null, null, "Unknown/Zone")]
    public async Task UpdateAsync_InvalidStartDefaults_ReturnsBadRequest(
        string? dayOfWeek,
        string? localTime,
        string? timeZoneId)
    {
        var modifier = new FakeDefaultsModifier();
        var request = new UpdateCalendarEventDefaultsRequest(
            new UpdateEventTextFieldsRequest(
                [new UpdateEventTextFieldRequest("text1", "Title", "ShortText", 50)]),
            new UpdateStartDefaultsRequest(dayOfWeek, localTime, timeZoneId));

        var built = CalendarEventDefaultsApi.TryBuildUpdateCommand(
            request,
            out _,
            out var error);

        Assert.False(built);
        Assert.IsType<BadRequestObjectResult>(error);
        Assert.Null(modifier.Saved);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("not-json")]
    public async Task UpdateAsync_MissingSectionOrInvalidJson_ReturnsBadRequest(string json)
    {
        var modifier = new FakeDefaultsModifier();

        var result = await CreateApi(EventTextFields.Default, StartDefaults.Empty, modifier)
            .UpdateAsync(HttpRequestFactory.WithBody(json), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Null(modifier.Saved);
    }

    private static CalendarEventDefaultsApi CreateApi(
        EventTextFields fields,
        StartDefaults startDefaults,
        FakeDefaultsModifier modifier) =>
        new(
            new GetCalendarEventDefaultsHandler(
                new FakeFieldsReader(fields),
                new FakeStartDefaultsReader(startDefaults)),
            new UpdateCalendarEventDefaultsHandler(modifier));

    private sealed class FakeFieldsReader(EventTextFields fields) : IEventTextFieldsReader
    {
        public Task<EventTextFields> GetAsync(CancellationToken cancellationToken) =>
            Task.FromResult(fields);
    }

    private sealed class FakeStartDefaultsReader(StartDefaults defaults) : IStartDefaultsReader
    {
        public Task<StartDefaults> GetAsync(CancellationToken cancellationToken) =>
            Task.FromResult(defaults);
    }

    private sealed class FakeDefaultsModifier : ICalendarEventDefaultsModifier
    {
        public CalendarEventDefaults? Saved { get; private set; }

        public Task SaveAsync(
            CalendarEventDefaults defaults,
            CancellationToken cancellationToken)
        {
            Saved = defaults;
            return Task.CompletedTask;
        }
    }
}
