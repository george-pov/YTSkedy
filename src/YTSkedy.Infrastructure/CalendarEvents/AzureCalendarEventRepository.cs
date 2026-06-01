using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Infrastructure.CalendarEvents;

public sealed class AzureCalendarEventRepository : ICalendarEventRepository
{
    public Task<string> CreateAsync(
        CalendarEvent calendarEvent,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
