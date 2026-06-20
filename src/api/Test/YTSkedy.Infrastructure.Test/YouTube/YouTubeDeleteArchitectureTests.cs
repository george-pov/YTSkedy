using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.YouTube;

namespace YTSkedy.Infrastructure.Test.YouTube;

/// <summary>
/// Guards the dependency direction for YouTube broadcast deletion: the
/// application layer must depend only on the <see cref="IYouTubeDeleter"/>
/// port, never on the infrastructure adapter or the Google SDK.
/// </summary>
public class YouTubeDeleteArchitectureTests
{
    [Fact]
    public void DeleteCalendarEventHandler_DependsOnYouTubeDeleterPort()
    {
        var constructor = Assert.Single(typeof(DeleteCalendarEventHandler).GetConstructors());

        Assert.Contains(
            constructor.GetParameters(),
            parameter => parameter.ParameterType == typeof(IYouTubeDeleter));
    }

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
