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

    [Fact]
    public async Task HandleAsync_NoPublishingRows_OrphansThenDeletesAndReturnsDeleted()
    {
        var reader = PlatformReader(ExistingPlatform);
        var modifier = new Mock<IPlatformModifier>();
        var publicationReader = PublicationReader([]);
        var publicationRepository = new Mock<IPublicationHistoryWriter>();
        var sequence = new MockSequence();
        publicationRepository
            .InSequence(sequence)
            .Setup(repository => repository.OrphanPublishedByPlatformAsync(
                PlatformId,
                CancellationToken.None))
            .ReturnsAsync(0);
        modifier
            .InSequence(sequence)
            .Setup(candidate => candidate.DeleteAsync(PlatformId, CancellationToken.None))
            .ReturnsAsync(DeletePlatformResult.Deleted);
        var handler = new DeletePlatformHandler(
            reader.Object,
            modifier.Object,
            publicationReader.Object,
            publicationRepository.Object);

        var result = await handler.HandleAsync(
            new DeletePlatformCommand(PlatformId),
            CancellationToken.None);

        Assert.Equal(DeletePlatformResult.Deleted, result);
        publicationReader.Verify(candidate => candidate.ListPublishingByPlatformAsync(
            PlatformId,
            CancellationToken.None));
        publicationRepository.Verify(repository => repository.OrphanPublishedByPlatformAsync(
            PlatformId,
            CancellationToken.None));
        modifier.Verify(candidate => candidate.DeleteAsync(
            PlatformId,
            CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_PlatformMissing_ReturnsNotFoundWithoutOrphanOrDelete()
    {
        var reader = PlatformReader(null);
        var modifier = new Mock<IPlatformModifier>();
        var publicationReader = PublicationReader([]);
        var publicationRepository = new Mock<IPublicationHistoryWriter>();
        var handler = new DeletePlatformHandler(
            reader.Object,
            modifier.Object,
            publicationReader.Object,
            publicationRepository.Object);

        var result = await handler.HandleAsync(
            new DeletePlatformCommand("missing"),
            CancellationToken.None);

        Assert.Equal(DeletePlatformResult.NotFound, result);
        publicationRepository.Verify(repository => repository.OrphanPublishedByPlatformAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
        modifier.Verify(candidate => candidate.DeleteAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task HandleAsync_PublishingRowExists_ReturnsConflictWithoutOrphanOrDelete()
    {
        var reader = PlatformReader(ExistingPlatform);
        var modifier = new Mock<IPlatformModifier>();
        var publicationReader = PublicationReader(
            [CreatePublication(PublishStatus.Publishing)]);
        var publicationRepository = new Mock<IPublicationHistoryWriter>();
        var handler = new DeletePlatformHandler(
            reader.Object,
            modifier.Object,
            publicationReader.Object,
            publicationRepository.Object);

        var result = await handler.HandleAsync(
            new DeletePlatformCommand(PlatformId),
            CancellationToken.None);

        Assert.Equal(DeletePlatformResult.Conflict, result);
        publicationRepository.Verify(repository => repository.OrphanPublishedByPlatformAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
        modifier.Verify(candidate => candidate.DeleteAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task HandleAsync_DeleteRacesToNotFound_ReturnsNotFoundAfterOrphaning()
    {
        var reader = PlatformReader(ExistingPlatform);
        var modifier = new Mock<IPlatformModifier>();
        modifier
            .Setup(candidate => candidate.DeleteAsync(PlatformId, CancellationToken.None))
            .ReturnsAsync(DeletePlatformResult.NotFound);
        var publicationReader = PublicationReader([]);
        var publicationRepository = new Mock<IPublicationHistoryWriter>();
        publicationRepository
            .Setup(repository => repository.OrphanPublishedByPlatformAsync(
                PlatformId,
                CancellationToken.None))
            .ReturnsAsync(0);
        var handler = new DeletePlatformHandler(
            reader.Object,
            modifier.Object,
            publicationReader.Object,
            publicationRepository.Object);

        var result = await handler.HandleAsync(
            new DeletePlatformCommand(PlatformId),
            CancellationToken.None);

        Assert.Equal(DeletePlatformResult.NotFound, result);
        publicationRepository.Verify(repository => repository.OrphanPublishedByPlatformAsync(
            PlatformId,
            CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_NullCommand_Throws()
    {
        var modifier = new Mock<IPlatformModifier>();
        var publicationRepository = new Mock<IPublicationHistoryWriter>();
        var handler = new DeletePlatformHandler(
            PlatformReader(ExistingPlatform).Object,
            modifier.Object,
            PublicationReader([]).Object,
            publicationRepository.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!, CancellationToken.None));

        publicationRepository.Verify(repository => repository.OrphanPublishedByPlatformAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
        modifier.Verify(candidate => candidate.DeleteAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    private static PlatformPublication CreatePublication(PublishStatus status) =>
        ApplicationTestData.Publication(
            status,
            platformId: PlatformId,
            platformName: "Main channel");

    private static Mock<IPlatformReader> PlatformReader(PlatformView? platform)
    {
        var reader = new Mock<IPlatformReader>();
        reader
            .Setup(candidate => candidate.GetAsync(
                It.IsAny<string>(),
                CancellationToken.None))
            .ReturnsAsync(platform);
        return reader;
    }

    private static Mock<IPlatformPublicationReader> PublicationReader(
        IReadOnlyList<PlatformPublication> publications)
    {
        var reader = new Mock<IPlatformPublicationReader>();
        reader
            .Setup(candidate => candidate.ListPublishingByPlatformAsync(
                PlatformId,
                CancellationToken.None))
            .ReturnsAsync(publications);
        return reader;
    }

}
