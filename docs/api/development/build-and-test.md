# API Build And Test

Run these commands from the repository root unless noted.

## Build

Build the backend solution:

```powershell
dotnet build src/api/YTSkedy.slnx
```

## Unit Tests

Run the backend application unit test project:

```powershell
dotnet test src/api/Test/YTSkedy.Scheduling.Application.Test/YTSkedy.Scheduling.Application.Test.csproj
```

Run all backend tests in the solution:

```powershell
dotnet test src/api/YTSkedy.slnx
```

Unit tests should not require Azure, YouTube, WordPress, network access, or
real credentials. See [`testing.md`](testing.md) for testing guidelines.

## Manual HTTP Checks

Manual HTTP checks live under:

```text
src/api/Test/YTSkedy.AzureFunctions.IntegrationTest/
```

These checks are `.http` files for Visual Studio and are not run by
`dotnet test`.

Current calendar event checks:

```text
src/api/Test/YTSkedy.AzureFunctions.IntegrationTest/CalendarEvents/CreateCalendarEvent.http
src/api/Test/YTSkedy.AzureFunctions.IntegrationTest/CalendarEvents/ListCalendarEventsByMonth.http
```

Before sending local requests:

- Start Azurite or provide an Azure Storage connection string.
- Start the Azure Functions host from Visual Studio or with Azure Functions
  Core Tools.
- Select the `local` environment in the `.http` editor.
- Use the host port from the Azure Functions launch profile. The current local
  default is `http://localhost:7087`.

CLI host command:

```powershell
dotnet build src/api/YTSkedy.slnx
```

```powershell
cd src/api/YTSkedy.AzureFunctions
func start --port 7087 --cors "http://localhost:4200,http://127.0.0.1:4200"
```

For Azurite, set `AzureWebJobsStorage` to `UseDevelopmentStorage=true` in
`src/api/YTSkedy.AzureFunctions/local.settings.json`. That file is ignored and
must not be committed.

Each request folder can include a shared `http-client.env.json`. Put personal
values, deployed URLs, and bearer access tokens in a sibling
`http-client.env.json.user`; do not commit that file. Calendar event endpoints
no longer accept `x-functions-key`; manual checks must send
`Authorization: Bearer <token>` with an Entra External ID access token. See
[Acquiring a development access token](#acquiring-a-development-access-token).

After a successful calendar event create request, change `localDateTime` before
sending it again. The UTC scheduled start is used as the storage row key, so
the same instant cannot be inserted twice.

If Visual Studio reports `HTTP0012: Unable to evaluate expression`, confirm the
`local` environment is selected in the `.http` editor. After creating or moving
an environment file, close and reopen the `.http` file or reload the solution
so Visual Studio refreshes the environment selector.

## Acquiring A Development Access Token

The calendar event endpoints require an Entra External ID access token that
targets the YTSkedy API app registration and carries the
`CalendarEvents.Read` or `CalendarEvents.Write` scope plus the
`CalendarEvents.Operator` app role. Two recipes work in practice; both leave
the token unredacted on disk only inside `http-client.env.json.user`, which
is gitignored.

### Recipe A: Browser sign-in (recommended for External ID)

This is the canonical path because it uses the same SPA flow operators sign
in with, so the resulting token has the right audience, scopes, role, and
issuer for the tenant configured under `Auth:` in `local.settings.json`.

1. Start the API (Functions host) and the SPA dev server (`npm start` under
   `src/ui/`).
2. Open `http://localhost:4200`, click **Sign in**, complete the Entra
   External ID redirect, and land on `/calendar-events`.
3. Open browser DevTools (F12) → **Application** tab → **Storage** →
   **Session storage** → expand `http://localhost:4200`.
4. In the search box at the top of the storage view, type
   `accesstoken-`. The MSAL Browser 5 cache key for an access token has
   the shape:

   ```text
   <homeAccountId>.<tenantId>-login.windows.net-accesstoken-<apiClientId>-<tenantId>-calendarevents.read calendarevents.write--
   ```

   Pick the row whose **Key** contains
   `accesstoken-<apiClientId>` (the API client id from `Auth:ClientId` in
   `local.settings.json`) and whose key ends with the scopes you want
   (`calendarevents.read calendarevents.write`).
5. Click the row. The **Value** column shows a single-line JSON object
   like this (formatted here for readability — in DevTools it is all on
   one line):

   ```json
   {
     "credentialType": "AccessToken",
     "homeAccountId": "...",
     "environment": "login.windows.net",
     "clientId": "<spaClientId>",
     "secret": "eyJ0eXAi...<TWO DOTS, ~1500 chars total>...g7q",
     "realm": "<tenantId>",
     "target": "calendarevents.read calendarevents.write ...",
     "cachedAt": "...",
     "expiresOn": "...",
     "tokenType": "Bearer"
   }
   ```

   Copy **only** the `"secret"` field value (without the surrounding
   quotes). That string is the JWT.

   **Sanity-check the paste before continuing.** A valid JWT:
   - is a single line of ~1500 - 2500 characters,
   - has **exactly two `.` (dot) separators** dividing it into
     `header.payload.signature`,
   - starts with `eyJ` (base64url of `{"`).

   If you only see one `eyJ...` segment with no dots, you copied the
   key cell or only the first segment of the value. Re-copy from the
   **Value** column.

6. Paste the JWT into `http-client.env.json.user` (sibling of
   `http-client.env.json` under `CalendarEvents/`, gitignored):

   ```json
   {
     "local": {
       "accessToken": "eyJ0eXAi........g7q",
       "accessTokenReadOnly": "eyJ0eXAi....g7q",
       "accessTokenWriteOnly": "eyJ0eXAi....g7q"
     }
   }
   ```

   The same token can fill `accessToken` and either scope-specific slot
   if the SPA was granted both scopes at sign-in (the default). To
   exercise the 403-on-missing-scope manual checks, obtain a token whose
   `scp` only carries the opposite scope (request `CalendarEvents.Read`
   or `CalendarEvents.Write` exclusively by editing the SPA scope
   request before the sign-in, or skip those checks).

7. **Reload the `.http` editor's environment cache.** Visual Studio
   caches environment files at the time the `.http` file is opened, so
   a freshly created `http-client.env.json.user` is invisible until you
   either close and reopen the `.http` file or reload the
   `YTSkedy.AzureFunctions.IntegrationTest` project. The public
   `http-client.env.json` file intentionally declares empty token
   placeholders; keep real token values only in the `.user` file. If
   `{{accessToken}}` keeps raising `HTTP0012: Unable to evaluate
   expression`, confirm the `local` environment is selected and reload
   the `.http` file after both environment files are saved.

Tokens expire in roughly one hour. Repeat steps 3-7 when 401s start
appearing.

### Recipe B: Azure CLI

Useful when the SPA is not running or for scripted checks.

```powershell
az login --tenant <tenantSubdomain>.onmicrosoft.com --allow-no-subscriptions
az account get-access-token `
  --tenant <tenantId> `
  --scope api://<api-client-id>/CalendarEvents.Read `
  --query accessToken -o tsv
```

Substitute the values from `local.settings.json` (`Auth:TenantId`,
`Auth:ClientId`) and from the API app registration's
**Expose an API → Application ID URI**.

**Known limitation:** Entra External ID does not always permit the Azure
CLI's first-party application to acquire tokens against custom-scope APIs
in External ID tenants. If `az login --tenant` fails with `AADSTS65002`,
`AADSTS50020`, or `az account get-access-token` returns
`AADSTS65001 (consent required)` or `AADSTS500011 (resource principal not
found)`, fall back to Recipe A. This is an External ID tenant
configuration constraint, not a bug in the recipe.
