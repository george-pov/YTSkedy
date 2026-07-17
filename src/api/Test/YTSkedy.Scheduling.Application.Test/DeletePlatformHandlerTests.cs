using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Platforms.Publications;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Test;

public class DeletePlatformHandlerTests
{
    private const string PlatformId = "p1";

    private static readonly PlatformView ExistingPlatform = ApplicationTestData.Platform(
        platformId: PlatformId,
        name: "Main channel");
    private readonly Mock<IPlatformReader> _platforms = new();
    private readonly Mock<IPlatformPublicationReader> _publications = new();
    private readonly Mock<IPublicationHistoryWriter> _history = new();
    private readonly Mock<IPlatformModifier> _modifier = new();
    private readonly DeletePlatformHandler _handler;

    public DeletePlatformHandlerTests()
    {
        _handler = new DeletePlatformHandler(
            _platforms.Object,
            _modifier.Object,
            _publications.Object,
            _history.Object);
    }

    [Fact]
    public async Task HandleAsync_NoPublishingRows_OrphansThenDeletesAndReturnsDeleted()
    {
        PlatformReader(ExistingPlatform);
        var publicationReader = PublicationReader([]);
        var sequence = new MockSequence();
        _history
            .InSequence(sequence)
            .Setup(repository => repository.OrphanPublishedByPlatformAsync(
                PlatformId,
                CancellationToken.None))
            .ReturnsAsync(0);
        _modifier
            .InSequence(sequence)
            .Setup(candidate => candidate.DeleteAsync(PlatformId, CancellationToken.None))
            .ReturnsAsync(DeletePlatformResult.Deleted);
        var result = await _handler.HandleAsync(
            new DeletePlatformCommand(PlatformId),
            CancellationToken.None);

        Assert.Equal(DeletePlatformResult.Deleted, result);
        publicationReader.Verify(candidate => candidate.ListPublishingByPlatformAsync(
            PlatformId,
            CancellationToken.None));
        _history.Verify(repository => repository.OrphanPublishedByPlatformAsync(
            PlatformId,
            CancellationToken.None));
        _modifier.Verify(candidate => candidate.DeleteAsync(
            PlatformId,
            CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_PlatformMissing_ReturnsNotFoundWithoutOrphanOrDelete()
    {
        PlatformReader(null);
        PublicationReader([]);

        var result = await _handler.HandleAsync(
            new DeletePlatformCommand("missing"),
            CancellationToken.None);

        Assert.Equal(DeletePlatformResult.NotFound, result);
        _history.Verify(repository => repository.OrphanPublishedByPlatformAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
        _modifier.Verify(candidate => candidate.DeleteAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task HandleAsync_PublishingRowExists_ReturnsConflictWithoutOrphanOrDelete()
    {
        PlatformReader(ExistingPlatform);
        PublicationReader(
            [CreatePublication(PublishStatus.Publishing)]);

        var result = await _handler.HandleAsync(
            new DeletePlatformCommand(PlatformId),
            CancellationToken.None);

        Assert.Equal(DeletePlatformResult.Conflict, result);
        _history.Verify(repository => repository.OrphanPublishedByPlatformAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
        _modifier.Verify(candidate => candidate.DeleteAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task HandleAsync_DeleteRacesToNotFound_ReturnsNotFoundAfterOrphaning()
    {
        PlatformReader(ExistingPlatform);
        _modifier
            .Setup(candidate => candidate.DeleteAsync(PlatformId, CancellationToken.None))
            .ReturnsAsync(DeletePlatformResult.NotFound);
        PublicationReader([]);
        _history
            .Setup(repository => repository.OrphanPublishedByPlatformAsync(
                PlatformId,
                CancellationToken.None))
            .ReturnsAsync(0);
        var result = await _handler.HandleAsync(
            new DeletePlatformCommand(PlatformId),
            CancellationToken.None);

        Assert.Equal(DeletePlatformResult.NotFound, result);
        _history.Verify(repository => repository.OrphanPublishedByPlatformAsync(
            PlatformId,
            CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_NullCommand_Throws()
    {
        PlatformReader(ExistingPlatform);
        PublicationReader([]);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _handler.HandleAsync(null!, CancellationToken.None));

        _history.Verify(repository => repository.OrphanPublishedByPlatformAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
        _modifier.Verify(candidate => candidate.DeleteAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    private static PlatformPublication CreatePublication(PublishStatus status) =>
        ApplicationTestData.Publication(
            status,
            platformId: PlatformId,
            platformName: "Main channel");

    private Mock<IPlatformReader> PlatformReader(PlatformView? platform)
    {
        _platforms
            .Setup(candidate => candidate.GetAsync(
                It.IsAny<string>(),
                CancellationToken.None))
            .ReturnsAsync(platform);
        return _platforms;
    }

    private Mock<IPlatformPublicationReader> PublicationReader(
        IReadOnlyList<PlatformPublication> publications)
    {
        _publications
            .Setup(candidate => candidate.ListPublishingByPlatformAsync(
                PlatformId,
                CancellationToken.None))
            .ReturnsAsync(publications);
        return _publications;
    }

}
