using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using YTSkedy.AzureFunctions.Http;

namespace YTSkedy.AzureFunctions.Test.Http;

public sealed class HttpQueryTests
{
    [Fact]
    public void TryGetSingleValue_MissingValue_ReturnsNull()
    {
        var parsed = HttpQuery.TryGetSingleValue(
            Request(""),
            "type",
            out var value,
            out var error);

        Assert.True(parsed);
        Assert.Null(value);
        Assert.IsType<EmptyResult>(error);
    }

    [Fact]
    public void TryGetSingleValue_DuplicateValue_ReturnsBadRequest()
    {
        var parsed = HttpQuery.TryGetSingleValue(
            Request("?type=YouTube&type=WordPress"),
            "type",
            out _,
            out var error);

        Assert.False(parsed);
        var badRequest = Assert.IsType<BadRequestObjectResult>(error);
        Assert.Equal("Query parameter 'type' must have a single value.", badRequest.Value);
    }

    [Fact]
    public void TryParseRequiredInt_MissingValue_ReturnsBadRequest()
    {
        var parsed = HttpQuery.TryParseRequiredInt(
            Request(""),
            "year",
            out _,
            out var error);

        Assert.False(parsed);
        var badRequest = Assert.IsType<BadRequestObjectResult>(error);
        Assert.Equal("Query parameter 'year' is required.", badRequest.Value);
    }

    [Fact]
    public void TryParseOptionalInt_InvalidValue_ReturnsCustomBadRequest()
    {
        var parsed = HttpQuery.TryParseOptionalInt(
            Request("?pageSize=wide"),
            "pageSize",
            out _,
            out _,
            out var error,
            "Query parameter 'pageSize' must be an integer between 1 and 100.");

        Assert.False(parsed);
        var badRequest = Assert.IsType<BadRequestObjectResult>(error);
        Assert.Equal(
            "Query parameter 'pageSize' must be an integer between 1 and 100.",
            badRequest.Value);
    }

    [Fact]
    public void TryValidateRange_OutOfRange_ReturnsBadRequest()
    {
        var parsed = HttpQuery.TryValidateRange("month", 13, 1, 12, out var error);

        Assert.False(parsed);
        var badRequest = Assert.IsType<BadRequestObjectResult>(error);
        Assert.Equal("Query parameter 'month' must be between 1 and 12.", badRequest.Value);
    }

    private static HttpRequest Request(string queryString)
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString(queryString);
        return context.Request;
    }
}
