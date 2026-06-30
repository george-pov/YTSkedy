namespace YTSkedy.Scheduling.Domain.CalendarEvents;

public sealed record EventTextField
{
    public EventTextField(
        string fieldKey,
        string label,
        EventTextType type,
        int maxLength)
    {
        if (!IsValidLabel(label))
        {
            throw new ArgumentException(
                "Label must be non-empty.",
                nameof(label));
        }

        if (!IsValidMaxLength(maxLength))
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxLength),
                "Max length must be greater than zero.");
        }

        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(
                nameof(type),
                "Event text type is invalid.");
        }

        FieldKey = NormalizeFieldKey(fieldKey);
        Label = NormalizeLabel(label);
        Type = type;
        MaxLength = maxLength;
    }

    public string FieldKey { get; init; }

    public string Label { get; init; }

    public EventTextType Type { get; init; }

    public int MaxLength { get; init; }

    public static bool IsValidLabel(string? label) =>
        !string.IsNullOrWhiteSpace(label);

    public static bool IsValidMaxLength(int maxLength) => maxLength > 0;

    internal static string NormalizeFieldKey(string? fieldKey) =>
        fieldKey?.Trim() ?? string.Empty;

    internal static string NormalizeLabel(string label) => label.Trim();
}
