using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using YTSkedy.Infrastructure.YouTube;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Infrastructure.Test.YouTube;

public class YouTubePublisherTests
{
    [Fact]
    public void Type_IsYouTube()
    {
        var publisher = new YouTubePublisher(
            new FakeCredentialStore(null),
            NullLogger<YouTubePublisher>.Instance);

        Assert.Equal(PlatformType.YouTube, publisher.Type);
    }

    [Fact]
    public async Task PublishAsync_UnconfiguredCredentials_LogsReferenceNameAndThrows()
    {
        var logger = new CapturingLogger<YouTubePublisher>();
        var publisher = new YouTubePublisher(new FakeCredentialStore(null), logger);
        var request = Request(new YouTubeSettings("missing-channel", "private", false));

        var exception = await Assert.ThrowsAsync<PlatformPublishException>(
            () => publisher.PublishAsync(request, CancellationToken.None));

        // The credentials reference is a non-secret name; no secret material is
        // available on this path, so the message and log carry only the reference.
        Assert.Contains("missing-channel", exception.Message);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Contains("missing-channel", entry.Message);
    }

    [Fact]
    public async Task PublishAsync_NonYouTubeSettings_Throws()
    {
        var publisher = new YouTubePublisher(
            new FakeCredentialStore(null),
            NullLogger<YouTubePublisher>.Instance);
        var request = Request(new OtherSettings());

        await Assert.ThrowsAsync<PlatformPublishException>(
            () => publisher.PublishAsync(request, CancellationToken.None));
    }

    private static PlatformPublishRequest Request(PublishSettings settings) =>
        new(
            "20260615T170000Z",
            "4fb4a32f3f344de1a7c3a9f4a2f94918",
            settings,
            "English title",
            "English description",
            new DateTimeOffset(2026, 6, 25, 17, 0, 0, TimeSpan.Zero));

    private sealed record OtherSettings : PublishSettings;

    private sealed class FakeCredentialStore(YouTubeChannelCredentials? credentials)
        : IYouTubeChannelCredentialStore
    {
        public YouTubeChannelCredentials? Find(string credentialsReference) => credentials;
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception)));
    }
}
