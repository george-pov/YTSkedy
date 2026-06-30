namespace YTSkedy.AzureFunctions.Settings;

internal sealed record EventTextFieldsResponse(
    IReadOnlyList<EventTextFieldResponse> Fields);
