# Calendar Event Start Defaults HTTP Contract

Calendar event start defaults are application-wide optional values used only
to initialize a newly opened calendar event create form. They do not modify
stored events or edit forms.

Both routes require bearer authentication and the
`CalendarEvents.Operator` app role. The read route requires
`CalendarEvents.Read`; the update route requires `CalendarEvents.Write`.

## Read Start Defaults

```text
GET /api/settings/calendar-event-start-defaults
```

Success returns `200 OK`:

```json
{
  "dayOfWeek": "Sunday",
  "localTime": "10:00",
  "timeZoneId": "America/Vancouver"
}
```

Each property is independently nullable. A missing settings row returns all
three properties as `null`. `dayOfWeek` uses the canonical .NET weekday names,
`localTime` uses `HH:mm`, and `timeZoneId` is an accepted IANA time-zone id.

## Replace Start Defaults

```text
PUT /api/settings/calendar-event-start-defaults
```

The request and `200 OK` response use the same shape as the read. The request
replaces all three values together. A `null` or omitted property clears that
default; an all-null request clears the complete setting. Empty strings,
non-canonical weekday names, malformed times, and unrecognized time-zone ids
return `400 Bad Request`. Invalid or missing JSON also returns `400 Bad Request`.

The setting has a separate Save action from event text fields. Saving either
setting does not write the other.
