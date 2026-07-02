using Microsoft.AspNetCore.Mvc;
using YTSkedy.AzureFunctions.CalendarEvents;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.AzureFunctions.Test.CalendarEvents;

public sealed class CalendarEventsApiTests
{
    private const string CalendarEventId = "f81d4fae7dec11d0a76500a0c91e6bf6";

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
}
