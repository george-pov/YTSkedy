namespace YTSkedy.Scheduling.Domain.Templates;

/// <summary>
/// Read model for a stored template. Carries the persisted GUID id alongside the
/// editable label, platform type, and content. Mirrors
/// <see cref="CalendarEvents.CalendarEventView"/> as the read counterpart to the
/// <see cref="Template"/> create-input.
/// </summary>
public sealed class TemplateView
{
    public TemplateView(string id, string name, TemplateType type, string content)
    {
        Id = id;
        Name = name;
        Type = type;
        Content = content;
    }

    public string Id { get; }

    public string Name { get; }

    public TemplateType Type { get; }

    public string Content { get; }
}
