namespace YTSkedy.AzureFunctions.Settings;

internal sealed record UpdateEventTextFieldsRequest(
    IReadOnlyList<UpdateEventTextFieldRequest>? Fields);
