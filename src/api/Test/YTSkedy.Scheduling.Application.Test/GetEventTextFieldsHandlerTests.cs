using YTSkedy.Scheduling.Application.Settings;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Test;

public sealed class GetEventTextFieldsHandlerTests
{
    [Fact]
    public async Task HandleAsync_CurrentSettings_ReturnsReaderValue()
    {
        var settings = new EventTextFields(
            [new EventTextField("Episode title", EventTextType.ShortText, 100)]);
        var reader = new FakeEventTextFieldsReader(settings);
        var handler = new GetEventTextFieldsHandler(reader);

        var result = await handler.HandleAsync(CancellationToken.None);

        Assert.Same(settings, result);
        Assert.True(reader.WasCalled);
    }

    private sealed class FakeEventTextFieldsReader(EventTextFields eventTextFields) :
        IEventTextFieldsReader
    {
        public bool WasCalled { get; private set; }

        public Task<EventTextFields> GetAsync(CancellationToken cancellationToken)
        {
            WasCalled = true;

            return Task.FromResult(eventTextFields);
        }
    }
}
