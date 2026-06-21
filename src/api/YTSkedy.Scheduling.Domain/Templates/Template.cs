namespace YTSkedy.Scheduling.Domain.Templates;

/// <summary>
/// Create-input for a reusable publishing template. Carries the editable label,
/// the platform type, and the free-text content. Mirrors
/// <see cref="CalendarEvents.CalendarEvent"/>: it holds no persisted id because
/// the repository generates the id on create. The constructor enforces the same
/// non-empty and length rules the API boundary exposes through
/// <see cref="IsValidName"/> and <see cref="IsValidContent"/>, so the boundary
/// and the domain share one source of truth.
/// </summary>
public sealed class Template
{
    public const int MaxNameLength = 50;
    public const int MaxContentLength = 2000;

    public Template(string name, TemplateType type, string content)
    {
        if (!IsValidName(name))
        {
            throw new ArgumentException(
                $"Name must be non-empty and at most {MaxNameLength} characters.",
                nameof(name));
        }

        if (!IsValidContent(content))
        {
            throw new ArgumentException(
                $"Content must be non-empty and at most {MaxContentLength} characters.",
                nameof(content));
        }

        Name = name;
        Type = type;
        Content = content;
    }

    public string Name { get; }

    public TemplateType Type { get; }

    public string Content { get; }

    /// <summary>
    /// True when <paramref name="name"/> is non-empty and at most
    /// <see cref="MaxNameLength"/> characters. The API boundary uses this to
    /// reject input with <c>400 Bad Request</c> before constructing a
    /// <see cref="Template"/>.
    /// </summary>
    public static bool IsValidName(string? name) =>
        !string.IsNullOrWhiteSpace(name) && name.Length <= MaxNameLength;

    /// <summary>
    /// True when <paramref name="content"/> is non-empty and at most
    /// <see cref="MaxContentLength"/> characters. Content is stored as free text
    /// and its tokens are not validated in this slice.
    /// </summary>
    public static bool IsValidContent(string? content) =>
        !string.IsNullOrWhiteSpace(content) && content.Length <= MaxContentLength;
}
