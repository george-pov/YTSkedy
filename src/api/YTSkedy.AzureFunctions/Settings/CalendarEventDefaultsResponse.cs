namespace YTSkedy.AzureFunctions.Settings;

internal sealed record CalendarEventDefaultsResponse(
    EventTextFieldsResponse EventTextFields,
    StartDefaultsResponse StartDefaults);
