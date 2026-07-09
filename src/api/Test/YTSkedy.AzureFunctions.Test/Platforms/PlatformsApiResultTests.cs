using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using YTSkedy.AzureFunctions.Platforms;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.TestSupport;
using static YTSkedy.AzureFunctions.Test.Platforms.PlatformTestData;

namespace YTSkedy.AzureFunctions.Test.Platforms;

public sealed class PlatformsApiResultTests
{
    [Fact]
    public void ToCreateResult_WordPressCreated_ReturnsRedactedResponse()
    {
        var actionResult = PlatformsApi.ToCreateResult(
            CreatePlatformResult.Created("wp-platform"),
            WordPressCreateCommand());

        var response = ActionResultAssertions.OkObject<PlatformResponse>(actionResult);
        Assert.Equal("wp-platform", response.PlatformId);
        Assert.Equal("company-blog", response.ReferenceKey);
        Assert.Equal("WordPress", response.Type);
        Assert.Equal("title-template", response.PublishingContent.TitleTemplateId);
        Assert.Equal("description-template", response.PublishingContent.DescriptionTemplateId);
        AssertWordPressRedacted(response.PublishSettings);
    }

    [Fact]
    public void ToCreateResult_DuplicateReferenceKey_Returns409()
    {
        var actionResult = PlatformsApi.ToCreateResult(
            CreatePlatformResult.ReferenceKeyAlreadyExists(),
            WordPressCreateCommand());

        Assert.Equal(
            "A platform reference key 'company-blog' already exists.",
            ActionResultAssertions.ConflictMessage(actionResult));
    }

    [Fact]
    public void ToCreateResult_LinkedTemplateNotFound_Returns400()
    {
        var actionResult = PlatformsApi.ToCreateResult(
            CreatePlatformResult.LinkedTemplateNotFound(),
            WordPressCreateCommand(
                publishingContent: RequiredPublishingContent(
                    titleTemplateId: "missing-template")));

        Assert.IsType<BadRequestObjectResult>(actionResult);
    }

    [Fact]
    public void ToUpdateResult_WordPressUpdated_ReturnsRedactedResponse()
    {
        var actionResult = PlatformsApi.ToUpdateResult(
            UpdatePlatformResult.Updated,
            WordPressUpdateCommand());

        var response = ActionResultAssertions.OkObject<PlatformResponse>(actionResult);
        Assert.Equal("company-blog", response.ReferenceKey);
        Assert.Equal("WordPress", response.Type);
        Assert.Equal("title-template", response.PublishingContent.TitleTemplateId);
        Assert.Equal("description-template", response.PublishingContent.DescriptionTemplateId);
        AssertWordPressRedacted(response.PublishSettings, "draft");
    }

    [Fact]
    public void ToUpdateResult_DuplicateReferenceKey_Returns409()
    {
        var actionResult = PlatformsApi.ToUpdateResult(
            UpdatePlatformResult.ReferenceKeyAlreadyExists,
            WordPressUpdateCommand());

        Assert.Equal(
            "A platform reference key 'company-blog' already exists.",
            ActionResultAssertions.ConflictMessage(actionResult));
    }

    [Fact]
    public void ToUpdateResult_NotFound_Returns404()
    {
        var actionResult = PlatformsApi.ToUpdateResult(
            UpdatePlatformResult.NotFound,
            WordPressUpdateCommand(platformId: "missing", referenceKey: null));

        Assert.IsType<NotFoundObjectResult>(actionResult);
    }

    [Fact]
    public void ToUpdateResult_LinkedTemplateNotFound_Returns400()
    {
        var actionResult = PlatformsApi.ToUpdateResult(
            UpdatePlatformResult.LinkedTemplateNotFound,
            WordPressUpdateCommand(
                referenceKey: null,
                publishingContent: RequiredPublishingContent(
                    titleTemplateId: "missing-template")));

        Assert.IsType<BadRequestObjectResult>(actionResult);
    }

    [Fact]
    public void ToDeleteResult_Deleted_Returns204()
    {
        var actionResult = PlatformsApi.ToDeleteResult(
            DeletePlatformResult.Deleted,
            "wp-platform");

        Assert.Equal(StatusCodes.Status204NoContent, ActionResultAssertions.StatusCode(actionResult));
    }

    [Fact]
    public void ToDeleteResult_NotFound_Returns404()
    {
        var actionResult = PlatformsApi.ToDeleteResult(
            DeletePlatformResult.NotFound,
            "missing");

        Assert.Equal(StatusCodes.Status404NotFound, ActionResultAssertions.StatusCode(actionResult));
    }

    [Fact]
    public void ToDeleteResult_Conflict_Returns409()
    {
        var actionResult = PlatformsApi.ToDeleteResult(
            DeletePlatformResult.Conflict,
            "wp-platform");

        Assert.Equal(StatusCodes.Status409Conflict, ActionResultAssertions.StatusCode(actionResult));
    }
}
