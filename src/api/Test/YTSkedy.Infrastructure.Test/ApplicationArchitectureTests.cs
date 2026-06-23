using YTSkedy.Scheduling.Application.Platforms;

namespace YTSkedy.Infrastructure.Test;

/// <summary>
/// Guards the dependency direction for the scheduling application layer: it must
/// not reference the infrastructure adapters or the Google SDK, so provider
/// details stay behind application ports such as <see cref="IPlatformPublisher"/>.
/// </summary>
public class ApplicationArchitectureTests
{
    [Fact]
    public void ApplicationAssembly_DoesNotReferenceGoogleSdk()
    {
        var applicationAssembly = typeof(IPlatformPublisher).Assembly;

        var googleReferences = applicationAssembly.GetReferencedAssemblies()
            .Select(name => name.Name)
            .Where(name => name is not null &&
                name.StartsWith("Google", StringComparison.Ordinal))
            .ToArray();

        Assert.Empty(googleReferences);
    }

    [Fact]
    public void ApplicationAssembly_DoesNotReferenceInfrastructure()
    {
        var applicationAssembly = typeof(IPlatformPublisher).Assembly;

        var infrastructureReferences = applicationAssembly.GetReferencedAssemblies()
            .Select(name => name.Name)
            .Where(name => name is not null &&
                name.StartsWith("YTSkedy.Infrastructure", StringComparison.Ordinal))
            .ToArray();

        Assert.Empty(infrastructureReferences);
    }
}
