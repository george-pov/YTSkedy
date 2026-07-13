using YTSkedy.Scheduling.Application.Settings;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Test;

public sealed class UpdateStartDefaultsHandlerTests
{
    [Fact]
    public async Task HandleAsync_Replacement_SavesAndReturnsAllValues()
    {
        var store = new FakeStartDefaultsStore(
            new StartDefaults(DayOfWeek.Monday, new TimeOnly(8, 0), "UTC"));
        var command = new UpdateStartDefaultsCommand(null, new TimeOnly(10, 30), null);

        var result = await new UpdateStartDefaultsHandler(store)
            .HandleAsync(command, CancellationToken.None);

        Assert.Equal(new StartDefaults(null, new TimeOnly(10, 30), null), result);
        Assert.Equal(result, store.Saved);
    }

    [Fact]
    public async Task HandleAsync_InvalidTimeZone_DoesNotSave()
    {
        var store = new FakeStartDefaultsStore(StartDefaults.Empty);
        var handler = new UpdateStartDefaultsHandler(store);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.HandleAsync(
                new UpdateStartDefaultsCommand(null, null, "Unknown/Zone"),
                CancellationToken.None));

        Assert.Null(store.Saved);
    }
}
