using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Azure.Functions.Worker;

namespace YTSkedy.AzureFunctions.Auth;

internal static class EndpointResolver
{
    private static readonly ConcurrentDictionary<string, MethodInfo?> MethodCache = new();

    /// <summary>
    /// Resolves the handler <see cref="MethodInfo"/> for a function from its
    /// entry point, caching the result. Returns null when the type or method
    /// cannot be found; callers must treat null as "unresolved" and fail closed
    /// rather than assuming the endpoint declares no requirements.
    /// </summary>
    public static MethodInfo? ResolveMethod(FunctionDefinition definition) =>
        MethodCache.GetOrAdd(definition.EntryPoint, static entryPoint =>
        {
            var lastDot = entryPoint.LastIndexOf('.');
            if (lastDot < 0)
            {
                return null;
            }

            var typeName = entryPoint[..lastDot];
            var methodName = entryPoint[(lastDot + 1)..];

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(typeName, throwOnError: false);
                if (type is not null)
                {
                    return type.GetMethod(methodName);
                }
            }

            return null;
        });

    public static bool AllowsAnonymous(FunctionDefinition definition) =>
        ResolveMethod(definition)?.GetCustomAttribute<AllowAnonymousAttribute>() is not null;
}
