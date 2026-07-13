using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Settings;

public sealed record CalendarEventDefaults(
    EventTextFields EventTextFields,
    StartDefaults StartDefaults);
