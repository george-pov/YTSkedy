using YTSkedy.Scheduling.Application.YouTube;

namespace YTSkedy.Infrastructure.Test.YouTube;

/// <summary>
/// Guards the dependency direction for YouTube ports: the application layer
/// must not reference the infrastructure adapter or the Google SDK.
/// </summary>
public class YouTubeDeleteArchitectureTests
{
    [Fact]
    public void ApplicationAssembly_DoesNotReferenceGoogleSdk()
    {
        var applicationAssembly = typeof(IYouTubeDeleter).Assembly;

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
        var applicationAssembly = typeof(IYouTubeDeleter).Assembly;

        var infrastructureReferences = applicationAssembly.GetReferencedAssemblies()
            .Select(name => name.Name)
            .Where(name => name is not null &&
                name.StartsWith("YTSkedy.Infrastructure", StringComparison.Ordinal))
            .ToArray();

        Assert.Empty(infrastructureReferences);
    }
}
