# Testing Guidelines

YTSkedy tests should focus on business behavior and integration contracts, not
framework wiring. The selected test framework is recorded in
[`../architecture/technology-stack.md`](../architecture/technology-stack.md),
and runnable commands are listed in [`build-and-test.md`](build-and-test.md).

## Backend Unit Tests

- Prefer unit tests for `YTSkedy.Scheduling.Domain` and
  `YTSkedy.Scheduling.Application`.
- Keep unit tests free of Azure, YouTube, WordPress, network, filesystem, and
  real credential dependencies.
- Test behavior through public module interfaces.
- Use test names that follow
  [`MethodName_Scenario_ExpectedBehavior`](naming-guidance.md#unit-test-names),
  with behavior names allowed for generic entry points.
- Prefer small hand-written fakes or stubs for simple dependencies.
- Use Moq only when mocking behavior is complex enough that it reduces test
  code complexity.
- Avoid testing private implementation details.
- Keep test data explicit, especially scheduled start times and time zones.

## Backend Application Tests

- Test application handlers with fake repositories and gateways.
- Verify commands are mapped into domain models correctly.
- Verify returned results contain the expected identifiers and state.
- Add validation tests when command validation is introduced.

## Frontend Tests

- Keep frontend tests under `src/ui/`.
- Use the Angular test setup exposed through `npm test`.
- Prefer component and service tests that cover user-facing behavior, form
  state, route behavior, formatting, and frontend API client mapping.
- Mock or fake backend HTTP calls in unit tests.
- Do not require a live Azure Functions host, Azure Storage, YouTube,
  WordPress, or real credentials for frontend unit tests.
- Keep date, time zone, locale, and API response fixture values explicit.
- Add browser end-to-end tooling only after a concrete workflow needs it. No
  frontend end-to-end test framework has been selected yet.

## Integration Tests

- Keep integration tests separate from unit tests when they require Azure
  storage, a live backend host, YouTube, WordPress, or authentication behavior.
- Treat the current `.http` files under
  `src/api/Test/YTSkedy.AzureFunctions.IntegrationTest/` as manual integration
  checks. They are not executed by `dotnet test`.
- Treat future frontend-to-backend browser tests as integration or end-to-end
  tests, not frontend unit tests.
- Use Azurite for local Azure Table Storage checks unless a real storage
  account has been explicitly selected for the run.
- Keep deployed host URLs, function keys, and other personal HTTP environment
  values in `http-client.env.json.user`, not in tracked environment files.
- Do not create, update, or delete real YouTube resources by default.
- Use local emulators, fakes, or explicitly approved test resources for
  integration coverage.
