namespace YTSkedy.Scheduling.Application.CalendarEvents;

public enum PublishCalendarEventOutcome
{
    Published,
    NotFound,
    AlreadyPublished,
    StartInPast,
    MissingEnglishDescription
}
