namespace YTSkedy.Scheduling.Domain.CalendarEvents;

public sealed record EventTextSnapshot
{
    public EventTextSnapshot(
        IReadOnlyList<EventTextField> fields,
        IReadOnlyList<EventTextValue> values)
    {
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(values);

        Fields = fields.ToArray();
        Values = values.ToArray();
    }

    public IReadOnlyList<EventTextField> Fields { get; }

    public IReadOnlyList<EventTextValue> Values { get; }

    public static EventTextSnapshot Create(
        EventTextFields fields,
        IEnumerable<EventTextValue> values)
    {
        ArgumentNullException.ThrowIfNull(fields);

        return Create(fields.Fields, values);
    }

    public EventTextSnapshot UpdateValues(IEnumerable<EventTextValue> values) =>
        Create(Fields, values);

    public string? ValueFor(string fieldKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldKey);

        return Values
            .FirstOrDefault(value => string.Equals(
                value.FieldKey,
                fieldKey,
                StringComparison.Ordinal))
            ?.Value;
    }

    private static EventTextSnapshot Create(
        IReadOnlyList<EventTextField> fields,
        IEnumerable<EventTextValue> values)
    {
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(values);

        var configuredKeys = fields
            .Select(field => field.FieldKey)
            .ToHashSet(StringComparer.Ordinal);
        var valuesByKey = new Dictionary<string, EventTextValue>(StringComparer.Ordinal);

        foreach (var value in values)
        {
            ArgumentNullException.ThrowIfNull(value);

            if (!configuredKeys.Contains(value.FieldKey))
            {
                throw new ArgumentException(
                    $"Field key '{value.FieldKey}' is not configured.",
                    nameof(values));
            }

            if (!valuesByKey.TryAdd(value.FieldKey, value))
            {
                throw new ArgumentException(
                    $"Field key '{value.FieldKey}' has multiple values.",
                    nameof(values));
            }
        }

        var orderedValues = new List<EventTextValue>();

        foreach (var field in fields)
        {
            if (!valuesByKey.TryGetValue(field.FieldKey, out var value) ||
                string.IsNullOrWhiteSpace(value.Value))
            {
                throw new ArgumentException(
                    $"Field key '{field.FieldKey}' requires a value.",
                    nameof(values));
            }

            if (value.Value.Length > field.MaxLength)
            {
                throw new ArgumentException(
                    $"Field key '{field.FieldKey}' value must be at most {field.MaxLength} characters.",
                    nameof(values));
            }

            orderedValues.Add(new EventTextValue(field.FieldKey, value.Value));
        }

        return new EventTextSnapshot(fields, orderedValues);
    }
}
