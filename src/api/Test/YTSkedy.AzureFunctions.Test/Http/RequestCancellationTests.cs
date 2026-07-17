using System.Collections.Immutable;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using YTSkedy.AzureFunctions.Http;
using YTSkedy.TestSupport;

namespace YTSkedy.AzureFunctions.Test.Http;

public sealed class RequestCancellationTests
{
    [Fact]
    public async Task Invoke_ConfirmedClientAbort_IsHandledAndLoggedAsInformation()
    {
        using var requestAbort = new CancellationTokenSource();
        requestAbort.Cancel();
        var context = TestFunctionContext.ForHttp(requestAbort.Token);
        var logger = CreateLogger();
        var middleware = new RequestCancellationMiddleware(logger.Typed);

        await middleware.Invoke(
            context,
            _ => throw new OperationCanceledException(requestAbort.Token));

        var entry = Assert.Single(logger.Mock.GetLogEntries());
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Contains("TestFunction", entry.Message, StringComparison.Ordinal);
        Assert.Contains(context.InvocationId, entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Invoke_InvocationCancellationWithoutRequestAbort_Propagates()
    {
        using var invocationCancellation = new CancellationTokenSource();
        invocationCancellation.Cancel();
        var context = TestFunctionContext.ForHttp(CancellationToken.None);
        var middleware = new RequestCancellationMiddleware(CreateLogger().Typed);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            middleware.Invoke(
                context,
                _ => throw new OperationCanceledException(invocationCancellation.Token)));
    }

    [Fact]
    public async Task Invoke_UnrelatedOperationCanceledException_Propagates()
    {
        var context = TestFunctionContext.ForHttp(CancellationToken.None);
        var middleware = new RequestCancellationMiddleware(CreateLogger().Typed);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            middleware.Invoke(context, _ => throw new OperationCanceledException()));
    }

    [Fact]
    public async Task Invoke_NormalInvocation_Completes()
    {
        var context = TestFunctionContext.ForHttp(CancellationToken.None);
        var logger = CreateLogger();
        var middleware = new RequestCancellationMiddleware(logger.Typed);
        var called = false;

        await middleware.Invoke(
            context,
            _ =>
            {
                called = true;
                return Task.CompletedTask;
            });

        Assert.True(called);
        Assert.Empty(logger.Mock.GetLogEntries());
    }

    private static (
        ILogger<RequestCancellationMiddleware> Typed,
        Mock<ILogger> Mock) CreateLogger()
    {
        var logger = new Mock<ILogger>();
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory
            .Setup(candidate => candidate.CreateLogger(It.IsAny<string>()))
            .Returns(logger.Object);

        return (new Logger<RequestCancellationMiddleware>(loggerFactory.Object), logger);
    }

    private sealed class TestFunctionContext : FunctionContext
    {
        private readonly TestFunctionDefinition functionDefinition = new();
        private readonly TestInvocationFeatures features = new();

        private TestFunctionContext(HttpContext httpContext)
        {
            Items["HttpRequestContext"] = httpContext;
        }

        public static TestFunctionContext ForHttp(CancellationToken requestAborted)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.RequestAborted = requestAborted;
            return new TestFunctionContext(httpContext);
        }

        public override string InvocationId { get; } = Guid.NewGuid().ToString("N");

        public override string FunctionId => "test-function-id";

        public override TraceContext TraceContext => null!;

        public override BindingContext BindingContext => null!;

        public override RetryContext RetryContext => null!;

        public override IServiceProvider InstanceServices { get; set; } = null!;

        public override FunctionDefinition FunctionDefinition => functionDefinition;

        public override IDictionary<object, object> Items { get; set; } =
            new Dictionary<object, object>();

        public override IInvocationFeatures Features => features;
    }

    private sealed class TestInvocationFeatures : IInvocationFeatures
    {
        private readonly Dictionary<Type, object> values = [];

        public void Set<T>(T instance) => values[typeof(T)] = instance!;

        public T Get<T>() =>
            values.TryGetValue(typeof(T), out var value) ? (T)value : default!;

        public IEnumerator<KeyValuePair<Type, object>> GetEnumerator() =>
            values.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() =>
            GetEnumerator();
    }

    private sealed class TestFunctionDefinition : FunctionDefinition
    {
        public override ImmutableArray<FunctionParameter> Parameters => [];

        public override string PathToAssembly => string.Empty;

        public override string EntryPoint => string.Empty;

        public override string Id => "test-function-id";

        public override string Name => "TestFunction";

        public override IImmutableDictionary<string, BindingMetadata> InputBindings =>
            ImmutableDictionary<string, BindingMetadata>.Empty;

        public override IImmutableDictionary<string, BindingMetadata> OutputBindings =>
            ImmutableDictionary<string, BindingMetadata>.Empty;
    }

}
