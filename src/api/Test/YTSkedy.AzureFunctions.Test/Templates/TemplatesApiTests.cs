using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using YTSkedy.AzureFunctions.Templates;
using YTSkedy.Scheduling.Application.Templates;
using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.AzureFunctions.Test.Templates;

/// <summary>
/// Covers the template API boundary logic without constructing the Functions
/// host: type parsing, request-to-command mapping with 400 validation,
/// result-to-status mapping, and the token list shape. These are the integration
/// contract surfaces for the templates and template-tokens routes.
/// </summary>
public sealed class TemplatesApiTests
{
    [Theory]
    [InlineData("YouTube", TemplateType.YouTube)]
    [InlineData("youtube", TemplateType.YouTube)]
    [InlineData("WordPress", TemplateType.WordPress)]
    [InlineData("wordpress", TemplateType.WordPress)]
    public void TryParseTemplateType_KnownType_ReturnsTrue(string value, TemplateType expected)
    {
        var parsed = TemplatesApi.TryParseTemplateType(value, out var type);

        Assert.True(parsed);
        Assert.Equal(expected, type);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("bogus")]
    [InlineData("0")]
    public void TryParseTemplateType_UnknownType_ReturnsFalse(string? value)
    {
        var parsed = TemplatesApi.TryParseTemplateType(value, out _);

        Assert.False(parsed);
    }

    [Fact]
    public void TryBuildCreateCommand_ValidRequest_BuildsCommand()
    {
        var request = new CreateTemplateRequest(
            "Weeknight stream",
            "YouTube",
            "Live at {{ localizedTime }}");

        var built = TemplatesApi.TryBuildCreateCommand(request, out var command, out _);

        Assert.True(built);
        Assert.Equal("Weeknight stream", command.Name);
        Assert.Equal(TemplateType.YouTube, command.Type);
        Assert.Equal("Live at {{ localizedTime }}", command.Content);
    }

    [Fact]
    public void TryBuildCreateCommand_EmptyName_ReturnsBadRequest()
    {
        var request = new CreateTemplateRequest("", "YouTube", "content");

        var built = TemplatesApi.TryBuildCreateCommand(request, out _, out var error);

        Assert.False(built);
        Assert.IsType<BadRequestObjectResult>(error);
    }

    [Fact]
    public void TryBuildCreateCommand_NameTooLong_ReturnsBadRequest()
    {
        var request = new CreateTemplateRequest(
            new string('n', Template.MaxNameLength + 1),
            "YouTube",
            "content");

        var built = TemplatesApi.TryBuildCreateCommand(request, out _, out var error);

        Assert.False(built);
        Assert.IsType<BadRequestObjectResult>(error);
    }

    [Fact]
    public void TryBuildCreateCommand_EmptyContent_ReturnsBadRequest()
    {
        var request = new CreateTemplateRequest("name", "YouTube", "");

        var built = TemplatesApi.TryBuildCreateCommand(request, out _, out var error);

        Assert.False(built);
        Assert.IsType<BadRequestObjectResult>(error);
    }

    [Fact]
    public void TryBuildCreateCommand_ContentTooLong_ReturnsBadRequest()
    {
        var request = new CreateTemplateRequest(
            "name",
            "YouTube",
            new string('c', Template.MaxContentLength + 1));

        var built = TemplatesApi.TryBuildCreateCommand(request, out _, out var error);

        Assert.False(built);
        Assert.IsType<BadRequestObjectResult>(error);
    }

    [Fact]
    public void TryBuildCreateCommand_InvalidType_ReturnsBadRequest()
    {
        var request = new CreateTemplateRequest("name", "bogus", "content");

        var built = TemplatesApi.TryBuildCreateCommand(request, out _, out var error);

        Assert.False(built);
        Assert.IsType<BadRequestObjectResult>(error);
    }

    [Fact]
    public void TryBuildUpdateCommand_ValidRequest_BuildsCommand()
    {
        var request = new UpdateTemplateRequest("Renamed", "Updated content");

        var built = TemplatesApi.TryBuildUpdateCommand(
            "WordPress",
            "9f8b1c2d3e4f",
            request,
            out var command,
            out _);

        Assert.True(built);
        Assert.Equal(TemplateType.WordPress, command.Type);
        Assert.Equal("9f8b1c2d3e4f", command.Id);
        Assert.Equal("Renamed", command.Name);
        Assert.Equal("Updated content", command.Content);
    }

    [Fact]
    public void TryBuildUpdateCommand_InvalidType_ReturnsBadRequest()
    {
        var request = new UpdateTemplateRequest("name", "content");

        var built = TemplatesApi.TryBuildUpdateCommand(
            "bogus",
            "9f8b1c2d3e4f",
            request,
            out _,
            out var error);

        Assert.False(built);
        Assert.IsType<BadRequestObjectResult>(error);
    }

    [Fact]
    public void TryBuildUpdateCommand_EmptyName_ReturnsBadRequest()
    {
        var request = new UpdateTemplateRequest("", "content");

        var built = TemplatesApi.TryBuildUpdateCommand(
            "YouTube",
            "9f8b1c2d3e4f",
            request,
            out _,
            out var error);

        Assert.False(built);
        Assert.IsType<BadRequestObjectResult>(error);
    }

    [Fact]
    public void TryBuildUpdateCommand_EmptyContent_ReturnsBadRequest()
    {
        var request = new UpdateTemplateRequest("name", "");

        var built = TemplatesApi.TryBuildUpdateCommand(
            "YouTube",
            "9f8b1c2d3e4f",
            request,
            out _,
            out var error);

        Assert.False(built);
        Assert.IsType<BadRequestObjectResult>(error);
    }

    [Fact]
    public void ToCreateResult_Created_Returns200WithId()
    {
        var result = CreateTemplateResult.Created("9f8b1c2d3e4f");

        var actionResult = TemplatesApi.ToCreateResult(result, "Weeknight stream", "YouTube");

        var ok = Assert.IsType<OkObjectResult>(actionResult);
        var response = Assert.IsType<CreateTemplateResponse>(ok.Value);
        Assert.Equal("9f8b1c2d3e4f", response.Id);
        Assert.Equal("Weeknight stream", response.Name);
        Assert.Equal("YouTube", response.Type);
    }

    [Fact]
    public void ToCreateResult_NameAlreadyExists_Returns409()
    {
        var result = CreateTemplateResult.NameAlreadyExists();

        var actionResult = TemplatesApi.ToCreateResult(result, "Weeknight stream", "YouTube");

        var conflict = Assert.IsType<ConflictObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
    }

    [Fact]
    public void ToUpdateResult_Updated_Returns200()
    {
        var actionResult = TemplatesApi.ToUpdateResult(
            UpdateTemplateResult.Updated,
            "9f8b1c2d3e4f",
            "Renamed",
            "YouTube");

        var ok = Assert.IsType<OkObjectResult>(actionResult);
        var response = Assert.IsType<UpdateTemplateResponse>(ok.Value);
        Assert.Equal("9f8b1c2d3e4f", response.Id);
        Assert.Equal("Renamed", response.Name);
        Assert.Equal("YouTube", response.Type);
    }

    [Fact]
    public void ToUpdateResult_NotFound_Returns404()
    {
        var actionResult = TemplatesApi.ToUpdateResult(
            UpdateTemplateResult.NotFound,
            "9f8b1c2d3e4f",
            "name",
            "YouTube");

        Assert.IsType<NotFoundObjectResult>(actionResult);
    }

    [Fact]
    public void ToUpdateResult_NameAlreadyExists_Returns409()
    {
        var actionResult = TemplatesApi.ToUpdateResult(
            UpdateTemplateResult.NameAlreadyExists,
            "9f8b1c2d3e4f",
            "name",
            "YouTube");

        var conflict = Assert.IsType<ConflictObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
    }

    [Fact]
    public void ToDeleteResult_Deleted_Returns204()
    {
        var actionResult = TemplatesApi.ToDeleteResult(DeleteTemplateResult.Deleted, "9f8b1c2d3e4f");

        var noContent = Assert.IsType<NoContentResult>(actionResult);
        Assert.Equal(StatusCodes.Status204NoContent, noContent.StatusCode);
    }

    [Fact]
    public void ToDeleteResult_NotFound_Returns404()
    {
        var actionResult = TemplatesApi.ToDeleteResult(DeleteTemplateResult.NotFound, "9f8b1c2d3e4f");

        Assert.IsType<NotFoundObjectResult>(actionResult);
    }

    [Fact]
    public void ToListResponse_Views_MapEveryField()
    {
        var views = new[]
        {
            new TemplateView("id1", "First", TemplateType.YouTube, "content one"),
            new TemplateView("id2", "Second", TemplateType.WordPress, "content two")
        };

        var response = TemplatesApi.ToListResponse(views);

        Assert.Collection(
            response.Templates,
            first =>
            {
                Assert.Equal("id1", first.Id);
                Assert.Equal("First", first.Name);
                Assert.Equal("YouTube", first.Type);
                Assert.Equal("content one", first.Content);
            },
            second =>
            {
                Assert.Equal("id2", second.Id);
                Assert.Equal("Second", second.Name);
                Assert.Equal("WordPress", second.Type);
                Assert.Equal("content two", second.Content);
            });
    }

    [Fact]
    public void ToTokenListResponse_Catalog_MapsEveryTokenName()
    {
        var response = TemplatesApi.ToTokenListResponse(TemplateTokenCatalog.All);

        Assert.Equal(
            ["localizedDate", "localizedTime", "youTubeBroadcastId", "calendarEventTitle"],
            response.Tokens.Select(token => token.Name));
    }
}
