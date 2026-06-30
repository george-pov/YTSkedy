namespace YTSkedy.AzureFunctions.Settings;

internal sealed record UpdateEventTextFieldRequest(
    string? FieldKey,
    string? Label,
    string? Type,
    int MaxLength);
