using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.CalendarEvents;

/// <summary>
/// Command to replace the localized descriptions of an existing calendar event.
/// The scheduled start (and therefore the event identity) is immutable, so this
/// command carries no start: only the descriptions change.
/// </summary>
public sealed record UpdateDescriptionsCommand(
    string CalendarEventId,
    IReadOnlyList<LocalizedDescription> Descriptions);
