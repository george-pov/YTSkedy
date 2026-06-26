namespace YTSkedy.Scheduling.Application.Platforms;

public sealed record PublicationDeleteResult(PublicationDeleteStatus Status)
{
    public static readonly PublicationDeleteResult Deleted =
        new(PublicationDeleteStatus.Deleted);

    public static readonly PublicationDeleteResult AlreadyGone =
        new(PublicationDeleteStatus.AlreadyGone);

    public static readonly PublicationDeleteResult StateConflict =
        new(PublicationDeleteStatus.StateConflict);

    public static readonly PublicationDeleteResult Failed =
        new(PublicationDeleteStatus.Failed);
}
