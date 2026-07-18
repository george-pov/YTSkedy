# Calendar Event Thumbnails HTTP Contract

Canonical HTTP contract for calendar-event thumbnail upload, retrieval, and
deletion.

## Upload Calendar Event Thumbnail

```text
PUT /api/calendar-events/{calendarEventId}/thumbnail
```

Uploads or replaces one current thumbnail for a calendar event. Requires the
`CalendarEvents.Write` scope. The request must be `multipart/form-data` with
one file part named `thumbnail`.

The backend accepts JPEG and PNG thumbnails only. Validation checks the file
extension (`.jpg`, `.jpeg`, or `.png`), content type (`image/jpeg` or
`image/png`), byte size (2 MB or smaller), and readable image dimensions.
The backend does not crop, resize, rotate, recompress, optimize, strip
metadata, enforce a 16:9 aspect ratio, or enforce a minimum width.

Success response (`200 OK`):

```json
{
  "fileName": "stream.png",
  "contentType": "image/png",
  "sizeBytes": 123456,
  "width": 1280,
  "height": 720,
  "updatedUtc": "2026-06-01T12:00:00+00:00"
}
```

Status codes:

- `200 OK` when the thumbnail is stored and metadata is saved.
- `400 Bad Request` when the upload is not `multipart/form-data`, the
  `thumbnail` part is missing, the form body cannot be read, or validation
  fails.
- `404 Not Found` when the calendar event id does not exist.
- `409 Conflict` when any platform publication row exists for the event.
- `409 Conflict` when the calendar event metadata changed during the upload.
  Reload the event before retrying.

## Get Calendar Event Thumbnail

```text
GET /api/calendar-events/{calendarEventId}/thumbnail
```

Returns the current thumbnail bytes. Requires the `CalendarEvents.Read` scope.
The response body is the image content and the response content type is the
stored thumbnail content type. Browser clients should fetch this route through
the typed API client and create an object URL rather than using the protected
route directly as an image URL.

Status codes:

- `200 OK` with image bytes when metadata and blob content exist.
- `404 Not Found` when the calendar event id does not exist.
- `404 Not Found` when the event has no thumbnail metadata or the stored blob
  content is missing.

## Delete Calendar Event Thumbnail

```text
DELETE /api/calendar-events/{calendarEventId}/thumbnail
```

Deletes the current thumbnail metadata and blob content. Requires the
`CalendarEvents.Write` scope.

Status codes:

- `204 No Content` when the thumbnail is deleted.
- `404 Not Found` when the calendar event id does not exist.
- `404 Not Found` when the event has no thumbnail metadata.
- `409 Conflict` when any platform publication row exists for the event.
- `409 Conflict` when the calendar event metadata changed during the delete.
  Reload the event before retrying.


Calendar-event metadata and mutation rules are documented in
[`calendar-events.md`](calendar-events.md). Platform thumbnail application is
documented in [`platform-publications.md`](platform-publications.md).
