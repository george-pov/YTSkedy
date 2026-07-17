using Microsoft.Extensions.Logging;

namespace YTSkedy.TestSupport;

public static class LoggerMockExtensions
{
    public static IReadOnlyList<(LogLevel Level, string Message)> GetLogEntries(
        this Mock<ILogger> logger) =>
        logger.Invocations
            .Where(invocation => invocation.Method.Name == nameof(ILogger.Log))
            .Select(invocation => (
                (LogLevel)invocation.Arguments[0],
                invocation.Arguments[2]?.ToString() ?? string.Empty))
            .ToArray();

    public static IReadOnlyList<(LogLevel Level, string Message)> GetLogEntries<T>(
        this Mock<ILogger<T>> logger) =>
        logger.Invocations
            .Where(invocation => invocation.Method.Name == nameof(ILogger.Log))
            .Select(invocation => (
                (LogLevel)invocation.Arguments[0],
                invocation.Arguments[2]?.ToString() ?? string.Empty))
            .ToArray();

    public static string GetLogText<T>(this Mock<ILogger<T>> logger) =>
        string.Join(
            Environment.NewLine,
            logger.GetLogEntries().Select(entry => entry.Message));

    public static string GetLogText(this Mock<ILogger> logger) =>
        string.Join(
            Environment.NewLine,
            logger.GetLogEntries().Select(entry => entry.Message));
}
