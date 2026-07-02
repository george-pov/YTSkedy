using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Infrastructure.CalendarEvents;

/// <summary>
/// Stored JSON shape for one <see cref="EventTextField"/>, shared by the calendar
/// event snapshot serializer and the event text fields settings serializer so the
/// field persistence shape, its wire type spelling, and its parsing live in one
/// place. Invalid stored data throws <see cref="ArgumentException"/>; each caller
/// wraps that in its own contextual message.
/// </summary>
internal sealed record EventTextFieldItem(
    string? FieldKey,
    string? Label,
    string? Type,
    int MaxLength)
{
    public static EventTextFieldItem From(EventTextField field)
    {
        ArgumentNullException.ThrowIfNull(field);

        return new EventTextFieldItem(
            field.FieldKey,
            field.Label,
            field.Type.ToString(),
            field.MaxLength);
    }

    public static EventTextField ToDomain(EventTextFieldItem? item)
    {
        if (item is null)
        {
            throw new ArgumentException(
                "Stored event text field cannot be null.",
                nameof(item));
        }

        if (!EventTextTypeParser.TryParse(item.Type, out var type))
        {
            throw new ArgumentException(
                $"Stored event text type '{item.Type ?? "<null>"}' is invalid.",
                nameof(item));
        }

        return new EventTextField(
            item.FieldKey ?? string.Empty,
            item.Label ?? string.Empty,
            type,
            item.MaxLength);
    }
}
