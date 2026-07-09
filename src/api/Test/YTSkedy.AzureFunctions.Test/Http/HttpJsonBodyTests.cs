using Microsoft.AspNetCore.Mvc;
using YTSkedy.AzureFunctions.Http;
using YTSkedy.TestSupport;

namespace YTSkedy.AzureFunctions.Test.Http;

public sealed class HttpJsonBodyTests
{
    [Fact]
    public async Task ReadRequiredAsync_ValidJson_ReturnsValue()
    {
        var request = HttpRequestFactory.WithBody("""{"name":"Weeknight stream"}""");

        var result = await HttpJsonBody.ReadRequiredAsync<SampleRequest>(
            request,
            CancellationToken.None);

        Assert.Null(result.Error);
        Assert.Equal("Weeknight stream", result.Value!.Name);
    }

    [Fact]
    public async Task ReadRequiredAsync_NullJson_ReturnsBadRequest()
    {
        var request = HttpRequestFactory.WithBody("null");

        var result = await HttpJsonBody.ReadRequiredAsync<SampleRequest>(
            request,
            CancellationToken.None);

        var error = Assert.IsType<BadRequestObjectResult>(result.Error);
        Assert.Equal("Request body is required.", error.Value);
    }

    [Fact]
    public async Task ReadRequiredAsync_InvalidJson_ReturnsBadRequest()
    {
        var request = HttpRequestFactory.WithBody("{");

        var result = await HttpJsonBody.ReadRequiredAsync<SampleRequest>(
            request,
            CancellationToken.None);

        var error = Assert.IsType<BadRequestObjectResult>(result.Error);
        Assert.Equal("Request body must be valid JSON.", error.Value);
    }

    private sealed record SampleRequest(string Name);
}
