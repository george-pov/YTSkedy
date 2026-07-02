namespace YTSkedy.Scheduling.Domain.CalendarEvents;

public sealed record EventTextFields
{
    public const int DefaultShortTextMaxLength = 50;
    public const int DefaultLongTextMaxLength = 2500;

    public EventTextFields(IEnumerable<EventTextField> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);

        Fields = NormalizeFields(fields).ToArray();

        if (Fields.Count == 0)
        {
            throw new ArgumentException(
                "At least one event text field is required.",
                nameof(fields));
        }
    }

    public static EventTextFields Default { get; } = new(
        [
            new EventTextField(
                "Title",
                EventTextType.ShortText,
                DefaultShortTextMaxLength),
            new EventTextField(
                "Description",
                EventTextType.LongText,
                DefaultLongTextMaxLength)
        ]);

    public IReadOnlyList<EventTextField> Fields { get; }

    private static IEnumerable<EventTextField> NormalizeFields(
        IEnumerable<EventTextField> fields)
    {
        var index = 0;
        foreach (var field in fields)
        {
            ArgumentNullException.ThrowIfNull(field);

            yield return field with { FieldKey = FieldKeyFor(index) };

            index++;
        }
    }

    private static string FieldKeyFor(int index) => $"text{index + 1}";
}
