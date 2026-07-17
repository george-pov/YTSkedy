using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using YTSkedy.AzureFunctions.Settings;
using YTSkedy.Scheduling.Application.Settings;
using YTSkedy.Scheduling.Domain.CalendarEvents;
using YTSkedy.TestSupport;

namespace YTSkedy.AzureFunctions.Test.Settings;

public sealed class CalendarEventDefaultsApiTests
{
    private readonly Mock<IEventTextFieldsReader> _fields = new();
    private readonly Mock<IStartDefaultsReader> _startDefaults = new();
    private readonly Mock<ICalendarEventDefaultsModifier> _modifier = new();
    private readonly CalendarEventDefaultsApi _api;

    public CalendarEventDefaultsApiTests()
    {
        _api = new CalendarEventDefaultsApi(
            new GetCalendarEventDefaultsHandler(
                _fields.Object,
                _startDefaults.Object),
            new UpdateCalendarEventDefaultsHandler(_modifier.Object));
    }

    [Fact]
    public async Task GetAsync_CurrentDefaults_ReturnsBothSettingsSections()
    {
        var fields = new EventTextFields(
            [new EventTextField("Episode", EventTextType.ShortText, 80)]);
        var startDefaults = new StartDefaults(
            DayOfWeek.Sunday,
            new TimeOnly(9, 5),
            "America/Vancouver");
        SetCurrentDefaults(fields, startDefaults);

        var result = await _api.GetAsync(
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
        CalendarEventDefaults? saved = null;
        _modifier
            .Setup(candidate => candidate.SaveAsync(
                It.IsAny<CalendarEventDefaults>(),
                CancellationToken.None))
            .Callback<CalendarEventDefaults, CancellationToken>(
                (defaults, _) => saved = defaults)
            .Returns(Task.CompletedTask);
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

        var result = await _api.UpdateAsync(request, CancellationToken.None);

        var response = Assert.IsType<CalendarEventDefaultsResponse>(
            Assert.IsType<OkObjectResult>(result).Value);
        Assert.NotNull(saved);
        Assert.Equal(
            ["text1", "text2"],
            saved!.EventTextFields.Fields.Select(field => field.FieldKey));
        Assert.Equal(
            ["text1", "text2"],
            response.EventTextFields.Fields.Select(field => field.FieldKey));
        Assert.Equal(
            ["Episode", "Notes"],
            response.EventTextFields.Fields.Select(field => field.Label));
        Assert.Equal(DayOfWeek.Sunday, saved.StartDefaults.DayOfWeek);
        Assert.Equal("09:05", response.StartDefaults.LocalTime);
    }

    [Fact]
    public async Task UpdateAsync_AllNullStartDefaults_ClearsDefaults()
    {
        CalendarEventDefaults? saved = null;
        _modifier
            .Setup(candidate => candidate.SaveAsync(
                It.IsAny<CalendarEventDefaults>(),
                CancellationToken.None))
            .Callback<CalendarEventDefaults, CancellationToken>(
                (defaults, _) => saved = defaults)
            .Returns(Task.CompletedTask);
        var result = await _api.UpdateAsync(
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
        Assert.Equal(StartDefaults.Empty, saved!.StartDefaults);
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
        var result = await _api.UpdateAsync(
            HttpRequestFactory.WithBody(json),
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        _modifier.Verify(candidate => candidate.SaveAsync(
            It.IsAny<CalendarEventDefaults>(),
            It.IsAny<CancellationToken>()), Times.Never());
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
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("not-json")]
    public async Task UpdateAsync_MissingSectionOrInvalidJson_ReturnsBadRequest(string json)
    {
        var result = await _api.UpdateAsync(
            HttpRequestFactory.WithBody(json),
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        _modifier.Verify(candidate => candidate.SaveAsync(
            It.IsAny<CalendarEventDefaults>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    private void SetCurrentDefaults(
        EventTextFields fields,
        StartDefaults startDefaults)
    {
        _fields
            .Setup(reader => reader.GetAsync(CancellationToken.None))
            .ReturnsAsync(fields);
        _startDefaults
            .Setup(reader => reader.GetAsync(CancellationToken.None))
            .ReturnsAsync(startDefaults);
    }
}
