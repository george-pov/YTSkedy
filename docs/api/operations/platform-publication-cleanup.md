# Platform Publication Cleanup

Provider cleanup for row-level platform publication delete.

Use this runbook when deleting a completed provider publication from the
calendar event details page or manual API checks. This is different from
deleting a calendar event and different from deleting a platform:

- Calendar event delete removes the application-owned event row and does not
  contact providers.
- Platform delete removes the configured destination and preserves published
  rows as orphan history.
- Platform-publication delete contacts the provider first, then removes the
  local publication row so the same event and platform can be published again
  while the event is still future by backend UTC time.

The HTTP contract is:

```text
DELETE /api/calendar-events/{calendarEventId}/platforms/{platformId}/publication
```

See [`../http/platform-publications.md`](../http/platform-publications.md) for the full request,
response, and status code contract.

## Preconditions

The backend allows cleanup only when all of these are true:

- The calendar event exists and its scheduled start is future by backend UTC
  time.
- The platform exists and is active.
- The publication row is `Published`, is not orphaned, and has an
  `externalResourceId`.
- The publication's secret-free target snapshot still matches the active
  platform.
- No publish is currently in progress for the same event and platform.

The browser must use `canDeletePublication` from the event-platform row. It
must not compute delete eligibility from browser time, status, or provider ids.

## YouTube Cleanup

For YouTube, `externalResourceId` is the YouTube live broadcast id created
during publish. Cleanup calls the YouTube Live Streaming API
`liveBroadcasts.delete` operation with that id and the stored YouTube OAuth
credentials for the platform.

Provider outcomes:

- Success deletes the YouTube scheduled broadcast, then YTSkedy deletes the
  local publication row.
- YouTube not-found is treated as success-equivalent because the provider
  resource is already gone.
- `liveBroadcastDeletionNotAllowed` maps to `409 Conflict`. The local
  publication row is kept.
- Credential, permission, quota, network, and other provider failures map to
  `502 Bad Gateway`. The local publication row is kept.

Recovery:

- For `409 Conflict`, inspect the broadcast in YouTube Studio. If YouTube no
  longer allows deletion, resolve it in YouTube and leave the YTSkedy row as
  provider history.
- For `502 Bad Gateway`, fix or refresh the platform's YouTube `clientId`,
  `clientSecret`, or `refreshToken`, then retry while the event is still future.
- If the event is no longer future, automated cleanup is blocked. Clean up in
  YouTube manually and treat the YTSkedy row as historical state.

## WordPress Cleanup

For WordPress, `externalResourceId` is the numeric WordPress post id returned by
publish. Cleanup discovers the WordPress REST API root from the active
platform's site URL and calls logical route:

```text
DELETE /wp/v2/posts/{id}
```

with `force=true`. The resolved provider URL may use a pretty REST root or
WordPress' `rest_route` query form. The request uses Basic Auth with the
platform's stored WordPress username and Application Password. `force=true`
bypasses Trash and hard-deletes the post.

Provider outcomes:

- Success hard-deletes the WordPress post, then YTSkedy deletes the local
  publication row.
- WordPress not-found is treated as success-equivalent because the provider
  resource is already gone.
- Invalid stored post ids, credential failures, permission failures, network
  failures, and other provider failures keep the local row. Credential and
  provider failures surface as `502 Bad Gateway`; invalid ids surface as
  `409 Conflict`.

Recovery:

- For credential or permission failures, update the platform's WordPress
  username or Application Password and retry while the event is still future.
- For target mismatch, restore the platform's site URL to the site that created
  the post, retry cleanup, then change the platform to the new site if needed.
- If the event is no longer future, clean up the post in WordPress manually and
  leave the YTSkedy row as historical state.

## Target Mismatch

YTSkedy stores a secret-free target snapshot on each publication row when a
publish starts. Cleanup compares that snapshot to the active platform before it
contacts the provider:

- YouTube compares the stored Google OAuth client id.
- WordPress compares the normalized site URL.

A mismatch returns `409 Conflict` and no provider call is made. This prevents a
rotated or repointed platform from deleting a resource in the wrong provider
target.

Recovery is operational:

1. Restore the platform settings to the original target.
2. Retry publication delete while the calendar event is still future.
3. Change the platform settings after cleanup succeeds.

If restoring the original target is impossible, clean up the provider resource
manually and keep the local row as historical state.

## Secret Handling

Provider cleanup must not log or return OAuth client secrets, refresh tokens,
Application Passwords, access tokens, or authorization headers. Logs may include
calendar event id, platform id, and the provider resource id when needed for
diagnosis.
