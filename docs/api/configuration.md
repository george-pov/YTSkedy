# API Configuration

API runtime configuration belongs outside source control when values are
environment-specific or secret.

## Local Settings

The ignored Azure Functions local settings file is:

```text
src/api/YTSkedy.AzureFunctions/local.settings.json
```

Do not commit OAuth client secrets, refresh tokens, access tokens, API keys,
storage connection strings for real accounts, or local credential stores.

## Azure Storage

Calendar event persistence reads the storage connection string in this order:

1. `AzureStorage:ConnectionString`
2. `AzureWebJobsStorage`

For local Azurite development, set:

```json
{
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true"
  }
}
```

Calendar event table name lookup:

1. `AzureStorage:CalendarEventsTableName`
2. Default: `CalendarEvents`

## Function Authorization

Current HTTP triggers use Azure Functions `Function` authorization level. Local
manual checks pass the function key with `x-functions-key`.

Personal function keys and deployed host URLs belong in
`http-client.env.json.user`, not in tracked `.http` environment files.

## Production Configuration Requirements

Production configuration must define:

- YouTube OAuth client IDs and callback behavior.
- Credential store location or provider.
- Default channel assumptions.
- API base URL behavior for the frontend host.
- Feature flags for dry-run or preview flows.
- Telemetry settings that do not expose secrets or personal account data.

Configuration values required for deployment must be documented with:

- setting name
- owner
- environment where it is required
- whether the value is public or secret
- local-development fallback, when one exists
- validation behavior when the value is missing or invalid
