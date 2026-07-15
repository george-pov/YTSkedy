using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace YTSkedy.AzureFunctions.Http;

/// <summary>
/// Treats only a confirmed HTTP client disconnect as normal request
/// cancellation. Other cancellation sources continue through the worker so
/// they remain observable at their owning boundary.
/// </summary>
internal sealed class RequestCancellationMiddleware(
    ILogger<RequestCancellationMiddleware> logger) : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (
            context.GetHttpContext()?.RequestAborted.IsCancellationRequested == true)
        {
            logger.LogInformation(
                "HTTP request was canceled by the client for function {FunctionName} and " +
                "invocation {InvocationId}.",
                context.FunctionDefinition.Name,
                context.InvocationId);
        }
    }
}
