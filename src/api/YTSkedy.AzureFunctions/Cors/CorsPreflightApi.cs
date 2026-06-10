using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace YTSkedy.AzureFunctions.Cors;

public sealed class CorsPreflightApi
{
    [Function("CorsPreflight")]
    [AllowAnonymous]
    public IActionResult HandleAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "options", Route = "calendar-events")]
        HttpRequest request)
    {
        return new NoContentResult();
    }
}
