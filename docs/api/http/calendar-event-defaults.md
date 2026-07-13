# Calendar Event Defaults HTTP Contract

Canonical HTTP contract for application-wide defaults used when creating new
calendar events.

## Routes

```text
GET /api/settings/calendar-event-defaults
PUT /api/settings/calendar-event-defaults
```

Both routes require bearer authentication and the
`CalendarEvents.Operator` app role. `GET` requires `CalendarEvents.Read` and
`PUT` requires `CalendarEvents.Write`.

## Read Calendar Event Defaults

`GET` returns `200 OK` with both settings sections:

```json
{
  "eventTextFields": {
    "fields": [
      {
        "fieldKey": "text1",
        "label": "Title",
        "type": "ShortText",
        "maxLength": 50
      },
      {
        "fieldKey": "text2",
        "label": "Description",
        "type": "LongText",
        "maxLength": 2500
      }
    ]
  },
  "startDefaults": {
    "dayOfWeek": "Sunday",
    "localTime": "10:00",
    "timeZoneId": "America/Vancouver"
  }
}
```

When the event text fields row is missing, the backend returns the default
`text1` and `text2` definitions shown above. When the start defaults row is
missing, `dayOfWeek`, `localTime`, and `timeZoneId` are all `null`.

Event text fields define the current text controls used by newly created
calendar events. Existing calendar events keep their stored field snapshot and
are not reshaped when these defaults change.

Start defaults are optional values used to calculate a suggestion when a new
calendar event form opens. They do not modify stored events or edit forms.

## Replace Calendar Event Defaults

`PUT` accepts the complete shape returned by `GET`. Both `eventTextFields` and
`startDefaults` are required. The backend validates the complete request before
writing either settings row and replaces both rows in one storage transaction.
The response is `200 OK` with the complete normalized settings document.

Event text field behavior:

- `fields` must contain at least one item.
- `type` is `ShortText` or `LongText`.
- `label` is required.
- `maxLength` must be a positive whole number.
- The backend derives `fieldKey` values from order as `text1`, `text2`,
  `text3`, and so on. Clients may send existing keys, but the response contains
  normalized keys.

Start default behavior:

- Each property is independently nullable.
- `dayOfWeek` uses canonical .NET weekday names.
- `localTime` uses `HH:mm`.
- `timeZoneId` uses an accepted IANA time-zone id.
- A `null` property clears that default. Three null values clear all start
  defaults.

Invalid or missing JSON, a missing settings section, an invalid field list, an
empty string, a non-canonical weekday, a malformed time, or an unrecognized
time-zone id returns `400 Bad Request` without saving either section.

Calendar-event create and start-suggestion behavior is documented in
[`calendar-events.md`](calendar-events.md).
