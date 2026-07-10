# Event Text Fields HTTP Contract

Canonical HTTP contract for the application event text field setting.

## Event Text Fields Setting

```text
GET /api/settings/event-text-fields
PUT /api/settings/event-text-fields
```

`GET` requires `CalendarEvents.Read`; `PUT` requires `CalendarEvents.Write`.
The setting defines the current text fields used by newly created calendar
events. Existing new-shape calendar events keep the field snapshot stored on
the event and are not reshaped when this setting changes.

When no settings row exists, `GET` returns the backend default list:

```json
{
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
}
```

`PUT` accepts the same ordered `fields` array. The backend derives `fieldKey`
values from order as `text1`, `text2`, `text3`, and so on. Clients may send
existing keys, but the response contains the normalized keys.

```json
{
  "fields": [
    {
      "fieldKey": "text1",
      "label": "Title",
      "type": "ShortText",
      "maxLength": 50
    },
    {
      "fieldKey": "text2",
      "label": "Summary",
      "type": "ShortText",
      "maxLength": 100
    },
    {
      "fieldKey": "text3",
      "label": "Description",
      "type": "LongText",
      "maxLength": 2500
    }
  ]
}
```

Current behavior:

- `type` is `ShortText` or `LongText`.
- `label` is required.
- `maxLength` must be a positive whole number.
- At least one field is required.
- Invalid JSON, a missing body, or an invalid field list returns
  `400 Bad Request`.


Calendar-event create and details behavior is documented in
[`calendar-events.md`](calendar-events.md).
