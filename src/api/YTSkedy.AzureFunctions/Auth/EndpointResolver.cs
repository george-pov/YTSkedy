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
    /// cannot be found, or when an overloaded method name makes the entry point
    /// ambiguous; callers must treat null as "unresolved" and fail closed rather
    /// than assuming the endpoint declares no requirements.
    /// </summary>
    internal static MethodInfo? ResolveMethod(FunctionDefinition definition) =>
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
                    try
                    {
                        return type.GetMethod(methodName);
                    }
                    catch (AmbiguousMatchException)
                    {
                        // Overloaded method name: the entry point cannot be
                        // resolved unambiguously. Fail closed (null) so the
                        // caller denies rather than guessing which overload's
                        // authorization attributes apply.
                        return null;
                    }
                }
            }

            return null;
        });

    internal static bool AllowsAnonymous(FunctionDefinition definition) =>
        ResolveMethod(definition)?.GetCustomAttribute<AllowAnonymousAttribute>() is not null;
}
