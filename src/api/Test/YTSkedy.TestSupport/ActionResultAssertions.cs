using Microsoft.AspNetCore.Mvc;

namespace YTSkedy.TestSupport;

public static class ActionResultAssertions
{
    public static string BadRequestMessage(IActionResult actionResult)
    {
        var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult);
        return Assert.IsType<string>(badRequest.Value);
    }

    public static T OkObject<T>(IActionResult actionResult)
    {
        var ok = Assert.IsType<OkObjectResult>(actionResult);
        return Assert.IsType<T>(ok.Value);
    }
}
