using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Domain.Test.CalendarEvents;

public sealed class EventTextSnapshotTests
{
    [Fact]
    public void Create_MissingRequiredValue_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => EventTextSnapshot.Create(
                EventTextFields.Default,
                [new EventTextValue("text1", "Stream title")]));
    }

    [Fact]
    public void Create_BlankRequiredValue_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => EventTextSnapshot.Create(
                EventTextFields.Default,
                [
                    new EventTextValue("text1", "Stream title"),
                    new EventTextValue("text2", " ")
                ]));
    }

    [Fact]
    public void Create_UnknownFieldKey_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => EventTextSnapshot.Create(
                EventTextFields.Default,
                [
                    new EventTextValue("text1", "Stream title"),
                    new EventTextValue("text2", "Detailed description"),
                    new EventTextValue("text3", "Unexpected value")
                ]));
    }

    [Fact]
    public void Create_DuplicateFieldKey_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => EventTextSnapshot.Create(
                EventTextFields.Default,
                [
                    new EventTextValue("text1", "Stream title"),
                    new EventTextValue("text1", "Duplicate title"),
                    new EventTextValue("text2", "Detailed description")
                ]));
    }

    [Fact]
    public void Create_FirstShortTextValue_ReturnsDisplayTitle()
    {
        var fields = new EventTextFields(
            [
                new EventTextField("Body", EventTextType.LongText, 2500),
                new EventTextField("Episode title", EventTextType.ShortText, 80)
            ]);
        var snapshot = EventTextSnapshot.Create(
            fields,
            [
                new EventTextValue("text1", "Long body"),
                new EventTextValue("text2", "Short title")
            ]);

        Assert.Equal("Short title", snapshot.DisplayTitle);
    }

    [Fact]
    public void Create_NoShortText_FallsBackToFirstValueForDisplayTitle()
    {
        var fields = new EventTextFields(
            [
                new EventTextField("Body", EventTextType.LongText, 2500),
                new EventTextField("Notes", EventTextType.LongText, 2500)
            ]);
        var snapshot = EventTextSnapshot.Create(
            fields,
            [
                new EventTextValue("text1", "Long body"),
                new EventTextValue("text2", "Notes")
            ]);

        Assert.Equal("Long body", snapshot.DisplayTitle);
    }

    [Fact]
    public void Create_ValueTooLong_Throws()
    {
        var fields = new EventTextFields(
            [new EventTextField("Title", EventTextType.ShortText, 5)]);

        Assert.Throws<ArgumentException>(
            () => EventTextSnapshot.Create(
                fields,
                [new EventTextValue("text1", "too long")]));
    }

    [Fact]
    public void Create_ValidValues_StoresValuesInFieldOrder()
    {
        var snapshot = EventTextSnapshot.Create(
            EventTextFields.Default,
            [
                new EventTextValue("text2", "Detailed description"),
                new EventTextValue("text1", "Stream title")
            ]);

        Assert.Equal(["text1", "text2"], snapshot.Values.Select(value => value.FieldKey));
        Assert.Equal(
            ["Stream title", "Detailed description"],
            snapshot.Values.Select(value => value.Value));
    }
}
