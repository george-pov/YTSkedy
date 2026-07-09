using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Domain.Test.CalendarEvents;

public sealed class EventTextSnapshotTests
{
    public static TheoryData<InvalidCreateCase> InvalidCreateCases => new()
    {
        new(
            "MissingRequiredValue",
            EventTextFields.Default,
            [new EventTextValue("text1", "Stream title")]),
        new(
            "BlankRequiredValue",
            EventTextFields.Default,
            [
                new EventTextValue("text1", "Stream title"),
                new EventTextValue("text2", " ")
            ]),
        new(
            "UnknownFieldKey",
            EventTextFields.Default,
            [
                new EventTextValue("text1", "Stream title"),
                new EventTextValue("text2", "Detailed description"),
                new EventTextValue("text3", "Unexpected value")
            ]),
        new(
            "DuplicateFieldKey",
            EventTextFields.Default,
            [
                new EventTextValue("text1", "Stream title"),
                new EventTextValue("text1", "Duplicate title"),
                new EventTextValue("text2", "Detailed description")
            ]),
        new(
            "ValueTooLong",
            new EventTextFields([new EventTextField("Title", EventTextType.ShortText, 5)]),
            [new EventTextValue("text1", "too long")])
    };

    [Theory]
    [MemberData(nameof(InvalidCreateCases))]
    public void Create_InvalidValues_Throws(InvalidCreateCase scenario)
    {
        Assert.Throws<ArgumentException>(
            () => EventTextSnapshot.Create(scenario.Fields, scenario.Values));
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

    public sealed record InvalidCreateCase(
        string Name,
        EventTextFields Fields,
        EventTextValue[] Values)
    {
        public override string ToString() => Name;
    }
}
