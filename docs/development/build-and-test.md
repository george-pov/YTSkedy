# Build And Test

Use these commands from the repository root.

## Build

Build the full solution:

```powershell
dotnet build src/YTSkedy.slnx
```

## Unit Tests

Run the current unit test project:

```powershell
dotnet test src/Test/YTSkedy.Scheduling.Application.Test/YTSkedy.Scheduling.Application.Test.csproj
```

Run all tests in the solution:

```powershell
dotnet test src/YTSkedy.slnx
```

Unit tests should not require Azure, YouTube, WordPress, network access, or
real credentials. See [`testing.md`](testing.md) for testing guidelines and
[`../architecture/technology-stack.md`](../architecture/technology-stack.md)
for selected tools.

## HTTP Integration Checks

Manual HTTP checks live under:

```text
src/Test/YTSkedy.AzureFunctions.IntegrationTest/
```

These checks are `.http` files for Visual Studio and are not run by
`dotnet test`.

The current calendar event request is:

```text
src/Test/YTSkedy.AzureFunctions.IntegrationTest/CalendarEvents/CreateCalendarEvent.http
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
dotnet build src/YTSkedy.slnx
```

```powershell
cd src/YTSkedy.AzureFunctions
func start --port 7087
```

The Functions host reads the table storage connection string from
`AzureStorage:ConnectionString` first, then from `AzureWebJobsStorage`.
For Azurite, set `AzureWebJobsStorage` to `UseDevelopmentStorage=true` in
`src/YTSkedy.AzureFunctions/local.settings.json`. That file is ignored and must
not be committed.

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
