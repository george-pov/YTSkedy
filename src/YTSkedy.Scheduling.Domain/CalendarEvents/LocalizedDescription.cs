namespace YTSkedy.Scheduling.Domain.CalendarEvents;

public sealed record LocalizedDescription(
    string Language,
    string Title,
    string? Description);
