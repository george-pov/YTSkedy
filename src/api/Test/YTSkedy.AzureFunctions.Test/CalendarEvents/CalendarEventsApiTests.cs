using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using YTSkedy.AzureFunctions.CalendarEvents;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.AzureFunctions.Test.CalendarEvents;

public sealed class CalendarEventsApiTests
{
    private const string CalendarEventId = "f81d4fae7dec11d0a76500a0c91e6bf6";

    [Fact]
    public async Task ListAsync_EventPage_MapsDisplayTitle()
    {
        var api = new CalendarEventsApi(
            null!,
            new ListEventsHandler(new FakeCalendarEventReader([CreateEvent()])),
            null!,
            null!,
            null!);
        var request = new DefaultHttpContext().Request;

        var result = await api.ListAsync(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<CalendarEventListResponse>(ok.Value);
        var item = Assert.Single(response.Items);
        Assert.Equal("English stream 1", item.DisplayTitle);
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

    [Fact]
    public void TryBuildCreateCommand_NullStart_ReturnsBadRequest()
    {
        var request = new CreateCalendarEventRequest(
            null!,
            [new EventTextPayload("text1", "English stream 1")]);

        var built = CalendarEventsApi.TryBuildCreateCommand(request, out _, out var error);

        Assert.False(built);
        Assert.IsType<BadRequestObjectResult>(error);
    }

    [Fact]
    public void TryBuildCreateCommand_NullTexts_ReturnsBadRequest()
    {
        var request = new CreateCalendarEventRequest(
            new CalendarEventStart(new DateTime(2026, 6, 15, 10, 0, 0), "America/Vancouver"),
            null!);

        var built = CalendarEventsApi.TryBuildCreateCommand(request, out _, out var error);

        Assert.False(built);
        Assert.IsType<BadRequestObjectResult>(error);
    }

    [Fact]
    public void TryBuildCreateCommand_NullTextEntry_ReturnsBadRequest()
    {
        var request = new CreateCalendarEventRequest(
            new CalendarEventStart(new DateTime(2026, 6, 15, 10, 0, 0), "America/Vancouver"),
            [null!]);

        var built = CalendarEventsApi.TryBuildCreateCommand(request, out _, out var error);

        Assert.False(built);
        Assert.IsType<BadRequestObjectResult>(error);
    }

    [Fact]
    public void TryBuildCreateCommand_BlankFieldKey_ReturnsBadRequest()
    {
        var request = new CreateCalendarEventRequest(
            new CalendarEventStart(new DateTime(2026, 6, 15, 10, 0, 0), "America/Vancouver"),
            [new EventTextPayload("   ", "English stream 1")]);

        var built = CalendarEventsApi.TryBuildCreateCommand(request, out _, out var error);

        Assert.False(built);
        Assert.IsType<BadRequestObjectResult>(error);
    }

    [Fact]
    public void TryBuildUpdateCommand_ValidRequest_BuildsCommand()
    {
        var request = new UpdateCalendarEventRequest(
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
        Assert.Equal(["text1", "text2"], command.Texts.Select(text => text.FieldKey));
        Assert.Equal(
            ["Updated title", "Updated description"],
            command.Texts.Select(text => text.Value));
    }

    [Fact]
    public void TryBuildUpdateCommand_NullTexts_ReturnsBadRequest()
    {
        var request = new UpdateCalendarEventRequest(null!);

        var built = CalendarEventsApi.TryBuildUpdateCommand(
            CalendarEventId,
            request,
            out _,
            out var error);

        Assert.False(built);
        Assert.IsType<BadRequestObjectResult>(error);
    }

    private static CalendarEventView CreateEvent() =>
        new(
            CalendarEventId,
            new ScheduledStart(new DateTime(2026, 6, 15, 10, 0, 0), "America/Vancouver"),
            new DateTimeOffset(2026, 6, 15, 17, 0, 0, TimeSpan.Zero),
            EventTextSnapshot.Create(
                EventTextFields.Default,
                [
                    new EventTextValue("text1", "English stream 1"),
                    new EventTextValue("text2", "Event description")
                ]));

    private sealed class FakeCalendarEventReader(
        IReadOnlyList<CalendarEventView> items) : ICalendarEventReader
    {
        public Task<IReadOnlyList<CalendarEventView>> ListAsync(
            CalendarEventMonthCriteria? criteria,
            CancellationToken cancellationToken) =>
            Task.FromResult(items);

        public Task<CalendarEventView?> GetByIdAsync(
            string calendarEventId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
