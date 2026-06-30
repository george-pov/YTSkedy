namespace YTSkedy.AzureFunctions.Settings;

internal sealed record EventTextFieldResponse(
    string FieldKey,
    string Label,
    string Type,
    int MaxLength);
