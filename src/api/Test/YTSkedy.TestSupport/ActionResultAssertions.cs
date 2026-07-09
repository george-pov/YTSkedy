using Microsoft.AspNetCore.Mvc;

namespace YTSkedy.TestSupport;

public static class ActionResultAssertions
{
    public static string BadRequestMessage(IActionResult actionResult)
    {
        var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult);
        return Assert.IsType<string>(badRequest.Value);
    }

    public static string ConflictMessage(IActionResult actionResult)
    {
        var conflict = Assert.IsType<ConflictObjectResult>(actionResult);
        return Assert.IsType<string>(conflict.Value);
    }

    public static T OkObject<T>(IActionResult actionResult)
    {
        var ok = Assert.IsType<OkObjectResult>(actionResult);
        return Assert.IsType<T>(ok.Value);
    }

    public static int? StatusCode(IActionResult actionResult) =>
        actionResult switch
        {
            ObjectResult objectResult => objectResult.StatusCode,
            StatusCodeResult statusCodeResult => statusCodeResult.StatusCode,
            _ => null
        };
}
