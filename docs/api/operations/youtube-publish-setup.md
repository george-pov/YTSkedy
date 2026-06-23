# YouTube Publish Setup

One-time setup to let the API publish calendar events as scheduled YouTube live
broadcasts. The result is three values for one channel that go in backend
configuration under a chosen reference name: `YouTubeChannels:{reference}:ClientId`,
`YouTubeChannels:{reference}:ClientSecret`, and
`YouTubeChannels:{reference}:RefreshToken`. The `{reference}` name is what a
YouTube platform stores as its non-secret `publishSettings.credentials` value.
See [`../configuration.md`](../configuration.md) for how the API consumes them
and [`../http/platforms.md`](../http/platforms.md) for the platform and publish
contract.

This is a proof-of-concept integration. The credentials are static and shared:
every publish through a platform acts on the single YouTube channel that minted
that channel's refresh token, regardless of which user is signed in to YTSkedy. A
per-user Google OAuth flow is deferred. You can configure more than one channel
by repeating this runbook with a different reference name. Do not commit any
value produced here.

## Prerequisites

- A Google account that manages the target YouTube channel. The YouTube Data
  API authorizes Data API calls as the account that owns the broadcasting
  channel, so this account determines which channel receives broadcasts.
- Live streaming enabled on that channel. Enable it in YouTube Studio under
  Create, then Go live. First-time activation requires channel verification and
  can take up to 24 hours. `liveBroadcasts.insert` fails until live streaming is
  active. See YouTube Help: <https://support.google.com/youtube/answer/2474026>.
- YTSkedy sign-in already works. The Entra sign-in, write scope, and operator
  role are unrelated to Google and are covered in
  [`../configuration.md`](../configuration.md).

## Part A: Google Cloud project and OAuth client

1. Open the Google Cloud console at <https://console.cloud.google.com> and
   create or select a project.
2. Enable the API. Go to APIs and Services, then Library, search for
   `YouTube Data API v3`, and click Enable.
3. Configure the OAuth consent screen. Go to APIs and Services, then OAuth
   consent screen. Google now presents this as the Google Auth Platform with
   separate left-menu pages. Complete them in order:
   - Branding: set the app name and a support email, then save. The other pages
     stay locked until branding is saved.
   - Audience: set User type to External. Under Test users, add the Google
     account that manages the channel.
   - Data access: click Add or remove scopes, then add
     `https://www.googleapis.com/auth/youtube` (filter for `youtube` or paste it
     into the manual box), then update and save.

     Some projects still show the older single-page consent screen, where
     Scopes and Test users are inline sections on one form. The values are the
     same.
   - Read the token-lifetime note in Part E before deciding whether to keep the
     publishing status as Testing or move it to In production.
   See the official help article for the consent screen:
   <https://support.google.com/cloud/answer/10311615>.
4. Create the OAuth client. Go to APIs and Services, then Credentials (the
   Clients page), then Create credentials, then OAuth client ID.
   - Application type: Web application. This is the correct type for a
     server-side app; do not use a desktop or other type.
   - Under Authorized redirect URIs, add exactly:
     `https://developers.google.com/oauthplayground`
   - Create, then copy the Client ID and Client Secret. The secret is shown
     once; store it securely.

## Part B: Mint a refresh token with the OAuth Playground

The OAuth Playground performs the standard authorization-code flow with offline
access and returns a refresh token. Using it avoids the deprecated out-of-band
flow.

1. Open <https://developers.google.com/oauthplayground>.
2. Click the gear icon (top right), check Use your own OAuth credentials, and
   paste the Client ID and Client Secret from Part A.
3. In the Step 1 panel, paste `https://www.googleapis.com/auth/youtube` into the
   Input your own scopes box, then click Authorize APIs.
4. Sign in with the Google account that manages the channel. If the account
   controls more than one channel through a Brand Account, pick the correct
   channel. If an unverified-app warning appears, choose Advanced, then proceed;
   as the owner you can continue past it.
5. Click Exchange authorization code for tokens.
6. Copy the Refresh token value. This is the
   `YouTubeChannels:{reference}:RefreshToken` secret.

## Part C: Configure the backend

1. If you do not already have the gitignored settings file, copy the tracked
   sample to it:

   ```text
   src/api/YTSkedy.AzureFunctions/local.settings.sample.json
   -> src/api/YTSkedy.AzureFunctions/local.settings.json
   ```

2. Choose a reference name for the channel (for example `main-youtube-channel`)
   and fill the values under `Values`:

   ```json
   "YouTubeChannels:main-youtube-channel:ClientId": "your-google-oauth-client-id",
   "YouTubeChannels:main-youtube-channel:ClientSecret": "your-google-oauth-client-secret",
   "YouTubeChannels:main-youtube-channel:RefreshToken": "your-refresh-token-from-the-playground"
   ```

   The reference name must match the `publishSettings.credentials` value of the
   YouTube platform you create later. To configure more channels, repeat this
   runbook and add another `YouTubeChannels:{anotherReference}:*` group.

Privacy and the made-for-kids flag are not configured here. They are part of each
platform's publish settings, so keep test platforms at `private` to avoid public
test broadcasts. Channel configuration is not validated on host start: a publish
that references a missing or incomplete channel fails that request as a provider
error, but the host still starts and the non-publish endpoints still work.

For a hosted Azure deployment, set these as Function App application settings
using the double-underscore form
(`YouTubeChannels__main-youtube-channel__ClientId`,
`YouTubeChannels__main-youtube-channel__ClientSecret`,
`YouTubeChannels__main-youtube-channel__RefreshToken`) and prefer a secret store
such as Key Vault for the two secret values. See [`deployment.md`](deployment.md).

## Part D: Run and verify

1. Start Azurite, or provide an Azure Storage connection string.
2. Start the Azure Functions host. The local default port is
   `http://localhost:7087`.
3. Create a YouTube platform whose `publishSettings.credentials` equals the
   reference name you chose in Part C, for example with
   `POST /api/platforms`:

   ```json
   {
     "name": "Main YouTube channel",
     "type": "YouTube",
     "publishSettings": {
       "credentials": "main-youtube-channel",
       "privacyStatus": "private",
       "selfDeclaredMadeForKids": false
     }
   }
   ```

4. Create a calendar event with a future start time and an English (`en`) title.
5. Publish the event to the platform with
   `POST /api/calendar-events/{calendarEventId}/platforms/{platformId}/publish`
   and an empty body `{}`. On success the response carries
   `status: "Published"`, an `externalResourceId`, and `publishedUtc`.
6. Confirm in YouTube Studio under Content, then Live: a private scheduled
   broadcast appears with the English title at the scheduled time.

The platform CRUD, event platform listing, and publish manual checks under
`src/api/Test/YTSkedy.AzureFunctions.IntegrationTest/` exercise these steps.

## Part E: Token lifetime and troubleshooting

Token lifetime:

- While the consent screen publishing status is Testing, an external-user-type
  project issues a refresh token that expires after 7 days. For a stable
  proof-of-concept token, move the publishing status to In production on the
  Audience page; you can still proceed past the unverified-app warning as the
  owner. Otherwise re-mint the token weekly with Part B.
- A refresh token also stops working if it is unused for six months, if the
  account owner revokes access, or if the per-account, per-client token limit is
  exceeded.

A failed provider call returns `502 Bad Gateway` and releases the reservation,
so the event/platform pair returns to `NotPublished` and can be retried. Check
the Functions host console for the underlying Google error:

- `liveStreamingNotEnabled`: enable live streaming on the channel (see
  Prerequisites).
- `invalid_grant`: the refresh token expired (the 7-day Testing limit), was
  revoked, or is wrong. Mint a new one, or move the consent screen to In
  production.
- Insufficient permissions or a scope error: the `youtube` scope was not granted
  during consent. Redo Part B and confirm the scope.
- Broadcast created on the wrong channel: redo Part B and select the correct
  Brand Account during sign-in.

A reference to a channel that is missing from `YouTubeChannels` or is missing a
field also returns `502`; the host logs only the non-secret reference name.

Other publish responses are unrelated to Google: `400` means a past start time
or a missing English title, `409` means the publication is already `Published`,
already in progress, or orphaned because the platform was deleted, `404` means
the calendar event or platform id is unknown, `501` means no provider serves the
platform type, and `401` or `403` are the Entra sign-in, write-scope, or
operator-role checks. See [`../http/platforms.md`](../http/platforms.md).

## Security

- `local.settings.json` is gitignored. Never commit it.
  `YouTubeChannels:{reference}:ClientSecret` and
  `YouTubeChannels:{reference}:RefreshToken` are secrets; do not paste them into
  shared chats, issues, or logs.
- Revoke a leaked or unwanted token at
  <https://myaccount.google.com/permissions>.
- The adapter does not log credentials, tokens, or authorization headers, and
  the credential reference name is the only channel value written to logs. Keep
  it that way when extending this integration.
