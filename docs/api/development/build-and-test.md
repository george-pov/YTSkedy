# API Build And Test

Run these commands from the repository root unless noted.

## Build

Build the backend solution:

```powershell
dotnet build src/api/YTSkedy.slnx
```

## Tests

Run the backend application unit test project:

```powershell
dotnet test src/api/Test/YTSkedy.Scheduling.Application.Test/YTSkedy.Scheduling.Application.Test.csproj
```

Run all backend tests in the solution:

```powershell
dotnet test src/api/YTSkedy.slnx
```

Tests should not require Azure, YouTube, WordPress, network access, or real
credentials unless they are isolated in a dedicated integration-test project
and explicitly documented. See [`testing.md`](testing.md) for testing
guidelines.
