# Build And Test

Use these commands from the repository root.

## Backend Build

Build the backend solution:

```powershell
dotnet build src/api/YTSkedy.slnx
```

## Backend Unit Tests

Run the backend application unit test project:

```powershell
dotnet test src/api/Test/YTSkedy.Scheduling.Application.Test/YTSkedy.Scheduling.Application.Test.csproj
```

Run all tests in the solution:

```powershell
dotnet test src/api/YTSkedy.slnx
```

Unit tests should not require Azure, YouTube, WordPress, network access, or
real credentials. See [`testing.md`](testing.md) for testing guidelines and
[`../architecture/technology-stack.md`](../architecture/technology-stack.md)
for selected tools.

## Frontend Setup

Run frontend commands from the Angular workspace:

```powershell
cd src/ui
```

Install packages after a fresh checkout or when `package-lock.json` changes:

```powershell
npm ci
```

Start the local Angular development server:

```powershell
npm start
```

The default Angular dev server is `http://localhost:4200`.

## Frontend Build And Tests

Build the frontend:

```powershell
npm run build
```

Run frontend unit tests:

```powershell
npm test
```

The current frontend is the generated Angular shell. It has no backend API
integration or production deployment workflow yet.

## HTTP Integration Checks

Manual HTTP checks live under:

```text
src/api/Test/YTSkedy.AzureFunctions.IntegrationTest/
```

These checks are `.http` files for Visual Studio and are not run by
`dotnet test`.

The current calendar event request is:

```text
src/api/Test/YTSkedy.AzureFunctions.IntegrationTest/CalendarEvents/CreateCalendarEvent.http
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
func start --port 7087
```

The Functions host reads the table storage connection string from
`AzureStorage:ConnectionString` first, then from `AzureWebJobsStorage`.
For Azurite, set `AzureWebJobsStorage` to `UseDevelopmentStorage=true` in
`src/api/YTSkedy.AzureFunctions/local.settings.json`. That file is ignored and
must not be committed.

Each request folder can include a shared `http-client.env.json`. Put personal
values, deployed URLs, and function keys in a sibling
`http-client.env.json.user`; do not commit that file.

After a successful calendar event create request, change `localDateTime` before
sending it again. The UTC scheduled start is used as the storage row key, so the
same instant cannot be inserted twice.

If Visual Studio reports `HTTP0012: Unable to evaluate expression`, confirm the
`local` environment is selected in the `.http` editor. After creating or moving
an environment file, close and reopen the `.http` file or reload the solution
so Visual Studio refreshes the environment selector.
