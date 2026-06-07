# API Testing

API tests should focus on business behavior and integration contracts, not
framework wiring.

## Unit Tests

- Prefer unit tests for `YTSkedy.Scheduling.Domain` and
  `YTSkedy.Scheduling.Application`.
- Keep unit tests free of Azure, YouTube, WordPress, network, filesystem, and
  real credential dependencies.
- Test behavior through public module interfaces.
- Use test names that follow
  [`MethodName_Scenario_ExpectedBehavior`](../../development/naming-guidance.md#unit-test-names),
  with behavior names allowed for generic entry points.
- Prefer small hand-written fakes or stubs for simple dependencies.
- Use Moq only when mocking behavior is complex enough that it reduces backend
  test code complexity.
- Avoid testing private implementation details.
- Keep test data explicit, especially scheduled start times and time zones.

## Application Tests

- Test application handlers with fake repositories and gateways.
- Verify commands are mapped into domain models correctly.
- Verify returned results contain the expected identifiers and state.
- Add validation tests when command validation is introduced.

## Integration Tests

- Keep integration tests separate from unit tests when they require Azure
  storage, a live backend host, YouTube, WordPress, or authentication behavior.
- Treat the current `.http` files under
  `src/api/Test/YTSkedy.AzureFunctions.IntegrationTest/` as manual integration
  checks. They are not executed by `dotnet test`.
- Use Azurite for local Azure Table Storage checks unless a real storage
  account has been explicitly selected for the run.
- Keep deployed host URLs, bearer access tokens, and other personal HTTP
  environment values in `http-client.env.json.user`, not in tracked
  environment files. Calendar event endpoints no longer accept
  `x-functions-key`; use `Authorization: Bearer <token>` (see
  [`build-and-test.md`](build-and-test.md)).
- Do not create, update, or delete real YouTube resources by default.
- Use local emulators, fakes, or explicitly approved test resources for
  integration coverage.

## Commands

Runnable API commands are listed in
[`build-and-test.md`](build-and-test.md).
