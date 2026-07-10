using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using YTSkedy.AzureFunctions.CalendarEvents;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Settings;
using YTSkedy.Scheduling.Domain.CalendarEvents;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.Scheduling.TestSupport;
using YTSkedy.TestSupport;

namespace YTSkedy.AzureFunctions.Test.CalendarEvents;

public sealed class CalendarEventsApiTests
{
    private const string CalendarEventId = SchedulingSampleIds.CalendarEventId;
    private const string InvalidTextsMessage = "Text entries must each have a field key and value.";

    public static TheoryData<object, string> InvalidCreateRequests =>
        new()
        {
            {
                new CreateCalendarEventRequest(
                    null!,
                    [new EventTextPayload("text1", "English stream 1")]),
                "Start local date-time and time zone id are required."
            },
            {
                new CreateCalendarEventRequest(
                    new CalendarEventStart(
                        new DateTime(2026, 6, 15, 10, 0, 0),
                        "America/Vancouver"),
                    null!),
                InvalidTextsMessage
            },
            {
                new CreateCalendarEventRequest(
                    new CalendarEventStart(
                        new DateTime(2026, 6, 15, 10, 0, 0),
                        "America/Vancouver"),
                    [null!]),
                InvalidTextsMessage
            },
            {
                new CreateCalendarEventRequest(
                    new CalendarEventStart(
                        new DateTime(2026, 6, 15, 10, 0, 0),
                        "America/Vancouver"),
                    [new EventTextPayload("   ", "English stream 1")]),
                InvalidTextsMessage
            }
        };

    public static TheoryData<object, string> InvalidUpdateRequests =>
        new()
        {
            {
                new UpdateCalendarEventRequest(
                    new CalendarEventStart(
                        new DateTime(2026, 6, 15, 10, 0, 0),
                        "America/Vancouver"),
                    null!),
                InvalidTextsMessage
            }
        };

    [Fact]
    public async Task ListAsync_EventPage_MapsDisplayTitleAndNotPublishedStatus()
    {
        var api = new CalendarEventsApi(
            null!,
            new ListEventsHandler(
                new FakeCalendarEventReader([CreateEvent()]),
                new FakePlatformReader()),
            null!,
            null!,
            null!);
        var request = new DefaultHttpContext().Request;

        var result = await api.ListAsync(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<CalendarEventListResponse>(ok.Value);
        var item = Assert.Single(response.Items);
        Assert.Equal("English stream 1", item.DisplayTitle);
        Assert.Equal("NotPublished", item.PublicationStatus);
    }

    [Fact]
    public async Task ListAsync_EventPage_SerializesPublicationStatusFieldName()
    {
        var api = new CalendarEventsApi(
            null!,
            new ListEventsHandler(
                new FakeCalendarEventReader([CreateEvent()]),
                new FakePlatformReader()),
            null!,
            null!,
            null!);

        var result = await api.ListAsync(
            new DefaultHttpContext().Request,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<CalendarEventListResponse>(ok.Value);
        var json = JsonSerializer.Serialize(
            response,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var document = JsonDocument.Parse(json);
        var item = document.RootElement.GetProperty("items")[0];
        Assert.Equal("NotPublished", item.GetProperty("publicationStatus").GetString());
    }

    [Theory]
    [InlineData(PublishingStatus.NotPublished, "NotPublished")]
    [InlineData(PublishingStatus.PartiallyPublished, "PartiallyPublished")]
    [InlineData(PublishingStatus.FullyPublished, "FullyPublished")]
    [InlineData(PublishingStatus.Failed, "Failed")]
    public void ToPublishingStatusString_Status_ReturnsContractValue(
        PublishingStatus status,
        string expected)
    {
        var result = CalendarEventsApi.ToPublishingStatusString(status);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void TryBuildCreateCommand_ValidRequest_BuildsCommand()
    {
        var request = new CreateCalendarEventRequest(
            new CalendarEventStart(new DateTime(2026, 6, 15, 10, 0, 0), "America/Vancouver"),
            [
                new EventTextPayload("text1", "English stream 1"),
                new EventTextPayload("text2", "Event description")
            ]);

        var built = CalendarEventsApi.TryBuildCreateCommand(request, out var command, out _);

        Assert.True(built);
        Assert.Equal(new DateTime(2026, 6, 15, 10, 0, 0), command.Start.LocalDateTime);
        Assert.Equal("America/Vancouver", command.Start.TimeZoneId);
        Assert.Equal(["text1", "text2"], command.Texts.Select(text => text.FieldKey));
        Assert.Equal(
            ["English stream 1", "Event description"],
            command.Texts.Select(text => text.Value));
    }

    [Theory]
    [MemberData(nameof(InvalidCreateRequests))]
    public void TryBuildCreateCommand_InvalidRequest_ReturnsBadRequest(
        object request,
        string expectedMessage)
    {
        var built = CalendarEventsApi.TryBuildCreateCommand(
            (CreateCalendarEventRequest)request,
            out _,
            out var error);

        Assert.False(built);
        Assert.Equal(expectedMessage, ActionResultAssertions.BadRequestMessage(error));
    }

    [Fact]
    public void TryBuildUpdateCommand_ValidRequest_BuildsCommand()
    {
        var request = new UpdateCalendarEventRequest(
            new CalendarEventStart(new DateTime(2026, 7, 20, 9, 30, 0), "Europe/London"),
            [
                new EventTextPayload("text1", "Updated title"),
                new EventTextPayload("text2", "Updated description")
            ]);

        var built = CalendarEventsApi.TryBuildUpdateCommand(
            CalendarEventId,
            request,
            out var command,
            out _);

        Assert.True(built);
        Assert.Equal(CalendarEventId, command.CalendarEventId);
        Assert.Equal(new DateTime(2026, 7, 20, 9, 30, 0), command.Start.LocalDateTime);
        Assert.Equal("Europe/London", command.Start.TimeZoneId);
        Assert.Equal(["text1", "text2"], command.Texts.Select(text => text.FieldKey));
        Assert.Equal(
            ["Updated title", "Updated description"],
            command.Texts.Select(text => text.Value));
    }

    [Fact]
    public void TryBuildUpdateCommand_MissingStart_ReturnsBadRequest()
    {
        var request = new UpdateCalendarEventRequest(
            null!,
            [new EventTextPayload("text1", "Updated title")]);

        var built = CalendarEventsApi.TryBuildUpdateCommand(
            CalendarEventId,
            request,
            out _,
            out var error);

        Assert.False(built);
        Assert.Equal(
            "Start local date-time and time zone id are required.",
            ActionResultAssertions.BadRequestMessage(error));
    }

    [Theory]
    [MemberData(nameof(InvalidUpdateRequests))]
    public void TryBuildUpdateCommand_InvalidRequest_ReturnsBadRequest(
        object request,
        string expectedMessage)
    {
        var built = CalendarEventsApi.TryBuildUpdateCommand(
            CalendarEventId,
            (UpdateCalendarEventRequest)request,
            out _,
            out var error);

        Assert.False(built);
        Assert.Equal(expectedMessage, ActionResultAssertions.BadRequestMessage(error));
    }

    [Fact]
    public async Task CreateCalendarEvent_DuplicateScheduledStart_ReturnsDuplicateScheduledStart()
    {
        var scheduledStartUtc = new DateTimeOffset(2026, 6, 15, 17, 0, 0, TimeSpan.Zero);
        var modifier = new FakeCalendarEventModifier
        {
            DuplicateScheduledStartUtc = scheduledStartUtc
        };
        var api = new CalendarEventsApi(
            new CreateCalendarEventHandler(
                new FakeEventTextFieldsReader(EventTextFields.Default),
                modifier),
            null!,
            null!,
            null!,
            null!);

        var result = await api.CreateCalendarEventAsync(
            HttpRequestFactory.WithBody("""
                {
                  "start": {
                    "localDateTime": "2026-06-15T10:00:00",
                    "timeZoneId": "America/Vancouver"
                  },
                  "texts": [
                    {
                      "fieldKey": "text1",
                      "value": "English stream"
                    },
                    {
                      "fieldKey": "text2",
                      "value": "Live stream"
                    }
                  ]
                }
                """),
            CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
        Assert.Equal(
            "Calendar event scheduled for '2026-06-15T17:00:00.0000000+00:00' already exists.",
            conflict.Value);
    }

    [Fact]
    public void ToCreateResult_DuplicateScheduledStart_Returns409()
    {
        var result = CalendarEventsApi.ToCreateResult(
            CreateCalendarEventResult.DuplicateScheduledStart(
                new DateTimeOffset(2026, 6, 15, 17, 0, 0, TimeSpan.Zero)));

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
    }

    private static CalendarEventView CreateEvent() =>
        SchedulingSamples.CalendarEvent(
            calendarEventId: CalendarEventId,
            scheduledStartUtc: new DateTimeOffset(2026, 6, 15, 17, 0, 0, TimeSpan.Zero),
            start: SchedulingSamples.ScheduledStart(
                new DateTime(2026, 6, 15, 10, 0, 0),
                "America/Vancouver"),
            text: SchedulingSamples.Text(
                title: "English stream 1",
                description: "Event description"));

    private sealed class FakeCalendarEventReader(
        IReadOnlyList<CalendarEventView> items) : ICalendarEventReader
    {
        public Task<IReadOnlyList<CalendarEventListRecord>> ListAsync(
            CalendarEventMonthCriteria? criteria,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<CalendarEventListRecord>>(
                items.Select(item => new CalendarEventListRecord(
                    item,
                    new HashSet<string>(StringComparer.Ordinal))).ToArray());

        public Task<CalendarEventView?> GetByIdAsync(
            string calendarEventId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakePlatformReader : IPlatformReader
    {
        public Task<IReadOnlyList<PlatformView>> ListAsync(
            PlatformType? type,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlySet<string>> ListIdsAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlySet<string>>(
                new HashSet<string>(StringComparer.Ordinal));

        public Task<PlatformView?> GetAsync(
            string platformId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeEventTextFieldsReader(EventTextFields eventTextFields) :
        IEventTextFieldsReader
    {
        public Task<EventTextFields> GetAsync(CancellationToken cancellationToken) =>
            Task.FromResult(eventTextFields);
    }

    private sealed class FakeCalendarEventModifier : ICalendarEventModifier
    {
        public DateTimeOffset? DuplicateScheduledStartUtc { get; init; }

        public Task<string> CreateAsync(
            CalendarEvent calendarEvent,
            DateTimeOffset scheduledStartUtc,
            CancellationToken cancellationToken)
        {
            if (DuplicateScheduledStartUtc is { } duplicateScheduledStartUtc)
            {
                throw new DuplicateScheduledStartException(duplicateScheduledStartUtc);
            }

            return Task.FromResult(CalendarEventId);
        }

        public Task<bool> UpdateAsync(
            string calendarEventId,
            CalendarEvent calendarEvent,
            DateTimeOffset scheduledStartUtc,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task DeleteAsync(
            string calendarEventId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
