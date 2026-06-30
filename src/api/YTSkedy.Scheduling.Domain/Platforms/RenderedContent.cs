namespace YTSkedy.Scheduling.Domain.Platforms;

/// <summary>
/// Transient title and description text rendered from a calendar event and
/// platform publishing content before publish starts.
/// </summary>
public sealed record RenderedContent
{
    public RenderedContent(string title, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        Title = title;
        Description = NormalizeDescription(description);
    }

    public string Title { get; }

    public string? Description { get; }

    public static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description)
            ? null
            : description;
}
