# UI Build And Test

Run frontend commands from the Angular workspace:

```powershell
cd src/ui
```

## Install

Install packages after a fresh checkout or when `package-lock.json` changes:

```powershell
npm ci
```

## Runtime Configuration

The browser app loads runtime configuration from:

```text
src/ui/public/config/app-config.json
```

That file is environment-specific and gitignored. After a fresh checkout,
create it from the tracked sample before running the dev server or browser
tests:

```powershell
Copy-Item public/config/app-config.sample.json public/config/app-config.json
```

Then replace every `<placeholder>` value with the Entra External ID and API
values for the environment you are using. The file must not contain function
keys, client secrets, access tokens, refresh tokens, passwords, or connection
strings.

`npm run build` can compile without the gitignored config file, but the built
app still requires `config/app-config.json` before it is served. Hosted
deployments must supply that file as part of deployment configuration.

## Development Server

Start the local Angular development server:

```powershell
npm start
```

The default Angular dev server is:

```text
http://localhost:4200
```

## Build

Build the frontend:

```powershell
npm run build
```

## Unit Tests

Run frontend unit tests:

```powershell
npm test
```

## End-to-End Tests

Install Playwright browser binaries when a fresh checkout or local environment
does not already have them:

```powershell
npx playwright install chromium
```

Run durable browser-level tests in Chromium:

```powershell
npm run test:e2e
```

Run the full configured browser matrix after installing all Playwright browser
binaries:

```powershell
npx playwright install
npm run test:e2e:all
```

Open Playwright UI mode for local debugging:

```powershell
npm run test:e2e:ui
```

The current frontend has routing and an initial calendar events page. Backend
API client integration and production deployment workflow are required release
work.

## Angular CLI

Use the npm script when invoking Angular CLI commands:

```powershell
npm run ng -- generate component pages/calendar-events/calendar-events-filter
npm run ng -- generate service pages/calendar-events/calendar-events-api
```

Keep generated files aligned with the naming and structure guidance in
[`development-guidelines.md`](development-guidelines.md) and
[`naming-conventions.md`](naming-conventions.md).
