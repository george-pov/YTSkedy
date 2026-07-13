using YTSkedy.Scheduling.Application.Settings;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Test;

public sealed class GetStartDefaultsHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsPortValue()
    {
        var expected = new StartDefaults(DayOfWeek.Friday, new TimeOnly(9, 15), "UTC");
        var store = new FakeStartDefaultsStore(expected);

        var result = await new GetStartDefaultsHandler(store).HandleAsync(CancellationToken.None);

        Assert.Equal(expected, result);
        Assert.Equal(1, store.GetCallCount);
    }
}
