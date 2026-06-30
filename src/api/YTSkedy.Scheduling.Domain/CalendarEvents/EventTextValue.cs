namespace YTSkedy.Scheduling.Domain.CalendarEvents;

public sealed record EventTextValue
{
    public EventTextValue(string fieldKey, string value)
    {
        if (string.IsNullOrWhiteSpace(fieldKey))
        {
            throw new ArgumentException(
                "Field key must be non-empty.",
                nameof(fieldKey));
        }

        ArgumentNullException.ThrowIfNull(value);

        FieldKey = fieldKey.Trim();
        Value = value;
    }

    public string FieldKey { get; init; }

    public string Value { get; init; }
}
