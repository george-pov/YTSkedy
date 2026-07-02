using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Domain.Test.CalendarEvents;

public sealed class EventTextFieldsTests
{
    [Fact]
    public void Default_ReturnsConfiguredShortAndLongTextFields()
    {
        var fields = EventTextFields.Default.Fields;

        Assert.Collection(
            fields,
            first =>
            {
                Assert.Equal("text1", first.FieldKey);
                Assert.Equal("Title", first.Label);
                Assert.Equal(EventTextType.ShortText, first.Type);
                Assert.Equal(EventTextFields.DefaultShortTextMaxLength, first.MaxLength);
            },
            second =>
            {
                Assert.Equal("text2", second.FieldKey);
                Assert.Equal("Description", second.Label);
                Assert.Equal(EventTextType.LongText, second.Type);
                Assert.Equal(EventTextFields.DefaultLongTextMaxLength, second.MaxLength);
            });
    }

    [Fact]
    public void Constructor_ReassignsFieldKeysFromOrderAndTrimsLabels()
    {
        var fields = new EventTextFields(
            [
                new EventTextField(" Primary title ", EventTextType.ShortText, 80),
                new EventTextField(" Long body ", EventTextType.LongText, 1000)
            ]);

        Assert.Collection(
            fields.Fields,
            first =>
            {
                Assert.Equal("text1", first.FieldKey);
                Assert.Equal("Primary title", first.Label);
                Assert.Equal(EventTextType.ShortText, first.Type);
                Assert.Equal(80, first.MaxLength);
            },
            second =>
            {
                Assert.Equal("text2", second.FieldKey);
                Assert.Equal("Long body", second.Label);
                Assert.Equal(EventTextType.LongText, second.Type);
                Assert.Equal(1000, second.MaxLength);
            });
    }

    [Fact]
    public void Constructor_RenumbersFieldKeysFromOrder()
    {
        EventTextField[] remaining =
        [
            new("Title", EventTextType.ShortText, 50),
            new("Details", EventTextType.LongText, 2500)
        ];

        var normalized = new EventTextFields(remaining);

        Assert.Equal(["text1", "text2"], normalized.Fields.Select(field => field.FieldKey));
    }

    [Fact]
    public void Constructor_BlankLabel_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => new EventTextField(" ", EventTextType.ShortText, 50));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_InvalidMaxLength_Throws(int maxLength)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new EventTextField("Title", EventTextType.ShortText, maxLength));
    }

    [Fact]
    public void Constructor_InvalidType_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new EventTextField("Title", (EventTextType)42, 50));
    }

    [Fact]
    public void Constructor_EmptyFieldList_Throws()
    {
        Assert.Throws<ArgumentException>(() => new EventTextFields([]));
    }

    [Fact]
    public void CreateSnapshot_MissingRequiredValue_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => EventTextSnapshot.Create(
                EventTextFields.Default,
                [new EventTextValue("text1", "Stream title")]));
    }

    [Fact]
    public void CreateSnapshot_BlankRequiredValue_Throws()
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
    public void CreateSnapshot_UnknownFieldKey_Throws()
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
    public void CreateSnapshot_DuplicateFieldKey_Throws()
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
    public void CreateSnapshot_DisplayTitle_ReturnsFirstShortTextValue()
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
    public void CreateSnapshot_DisplayTitle_NoShortText_FallsBackToFirstValue()
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
    public void CreateSnapshot_ValueTooLong_Throws()
    {
        var fields = new EventTextFields(
            [new EventTextField("Title", EventTextType.ShortText, 5)]);

        Assert.Throws<ArgumentException>(
            () => EventTextSnapshot.Create(
                fields,
                [new EventTextValue("text1", "too long")]));
    }

    [Fact]
    public void CreateSnapshot_ValidValues_StoresValuesInFieldOrder()
    {
        var snapshot = EventTextSnapshot.Create(
            EventTextFields.Default,
            [
                new EventTextValue("text2", "Detailed description"),
                new EventTextValue("text1", "Stream title")
            ]);

        Assert.Equal(["text1", "text2"], snapshot.Values.Select(value => value.FieldKey));
        Assert.Equal(["Stream title", "Detailed description"], snapshot.Values.Select(value => value.Value));
    }
}
