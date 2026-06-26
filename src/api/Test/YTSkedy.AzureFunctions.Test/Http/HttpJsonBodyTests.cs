using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using YTSkedy.AzureFunctions.Http;

namespace YTSkedy.AzureFunctions.Test.Http;

public sealed class HttpJsonBodyTests
{
    [Fact]
    public async Task ReadRequiredAsync_ValidJson_ReturnsValue()
    {
        var request = RequestWithBody("""{"name":"Weeknight stream"}""");

        var result = await HttpJsonBody.ReadRequiredAsync<SampleRequest>(
            request,
            CancellationToken.None);

        Assert.Null(result.Error);
        Assert.Equal("Weeknight stream", result.Value!.Name);
    }

    [Fact]
    public async Task ReadRequiredAsync_NullJson_ReturnsBadRequest()
    {
        var request = RequestWithBody("null");

        var result = await HttpJsonBody.ReadRequiredAsync<SampleRequest>(
            request,
            CancellationToken.None);

        var error = Assert.IsType<BadRequestObjectResult>(result.Error);
        Assert.Equal("Request body is required.", error.Value);
    }

    [Fact]
    public async Task ReadRequiredAsync_InvalidJson_ReturnsBadRequest()
    {
        var request = RequestWithBody("{");

        var result = await HttpJsonBody.ReadRequiredAsync<SampleRequest>(
            request,
            CancellationToken.None);

        var error = Assert.IsType<BadRequestObjectResult>(result.Error);
        Assert.Equal("Request body must be valid JSON.", error.Value);
    }

    private static HttpRequest RequestWithBody(string body)
    {
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        return context.Request;
    }

    private sealed record SampleRequest(string Name);
}
