using YTSkedy.Scheduling.Application.Settings;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Test;

public sealed class UpdateEventTextFieldsHandlerTests
{
    [Fact]
    public async Task HandleAsync_Fields_SavesAndReturnsNormalizedFields()
    {
        var modifier = new FakeEventTextFieldsModifier();
        var handler = new UpdateEventTextFieldsHandler(modifier);
        var command = new UpdateEventTextFieldsCommand(
            [
                new EventTextField("text7", " Title ", EventTextType.ShortText, 80),
                new EventTextField("text9", " Description ", EventTextType.LongText, 2000)
            ]);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.NotNull(modifier.Saved);
        Assert.Equal(["text1", "text2"], modifier.Saved!.Fields.Select(field => field.FieldKey));
        Assert.Equal(["text1", "text2"], result.Fields.Select(field => field.FieldKey));
        Assert.Equal(["Title", "Description"], result.Fields.Select(field => field.Label));
    }

    [Fact]
    public async Task HandleAsync_NullCommand_Throws()
    {
        var handler = new UpdateEventTextFieldsHandler(new FakeEventTextFieldsModifier());

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!, CancellationToken.None));
    }

    private sealed class FakeEventTextFieldsModifier : IEventTextFieldsModifier
    {
        public EventTextFields? Saved { get; private set; }

        public Task SaveAsync(
            EventTextFields eventTextFields,
            CancellationToken cancellationToken)
        {
            Saved = eventTextFields;

            return Task.CompletedTask;
        }
    }
}
