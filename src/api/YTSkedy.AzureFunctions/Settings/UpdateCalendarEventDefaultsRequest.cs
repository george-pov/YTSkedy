namespace YTSkedy.AzureFunctions.Settings;

internal sealed record UpdateCalendarEventDefaultsRequest(
    UpdateEventTextFieldsRequest? EventTextFields,
    UpdateStartDefaultsRequest? StartDefaults);
