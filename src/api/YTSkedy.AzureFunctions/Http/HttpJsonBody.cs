using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace YTSkedy.AzureFunctions.Http;

internal static class HttpJsonBody
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    internal static async Task<RequiredJsonBody<T>> ReadRequiredAsync<T>(
        HttpRequest request,
        CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            var value = await JsonSerializer.DeserializeAsync<T>(
                request.Body,
                JsonOptions,
                cancellationToken);

            return value is null
                ? RequiredJsonBody<T>.Failure(new BadRequestObjectResult("Request body is required."))
                : RequiredJsonBody<T>.Success(value);
        }
        catch (JsonException)
        {
            return RequiredJsonBody<T>.Failure(
                new BadRequestObjectResult("Request body must be valid JSON."));
        }
    }
}

internal readonly record struct RequiredJsonBody<T>(T? Value, IActionResult? Error)
    where T : class
{
    internal static RequiredJsonBody<T> Success(T value) => new(value, null);

    internal static RequiredJsonBody<T> Failure(IActionResult error) => new(null, error);
}
