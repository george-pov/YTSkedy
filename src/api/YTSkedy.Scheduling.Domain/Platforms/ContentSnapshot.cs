namespace YTSkedy.Scheduling.Domain.Platforms;

/// <summary>
/// Rendered title and description copied to a platform-publication row when a
/// publish attempt starts.
/// </summary>
public sealed record ContentSnapshot
{
    public ContentSnapshot(string title, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        Title = title;
        Description = RenderedContent.NormalizeDescription(description);
    }

    public string Title { get; }

    public string? Description { get; }
}
