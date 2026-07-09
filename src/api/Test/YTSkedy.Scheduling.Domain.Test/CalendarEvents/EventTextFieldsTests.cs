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
}
