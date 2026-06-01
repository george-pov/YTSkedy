# Testing Guidelines

YTSkedy tests should focus on business behavior and integration contracts, not
framework wiring. The selected test framework is recorded in
[`../architecture/technology-stack.md`](../architecture/technology-stack.md),
and runnable commands are listed in [`build-and-test.md`](build-and-test.md).

## Unit Tests

- Prefer unit tests for `YTSkedy.Scheduling.Domain` and
  `YTSkedy.Scheduling.Application`.
- Keep unit tests free of Azure, YouTube, WordPress, network, filesystem, and
  real credential dependencies.
- Test behavior through public module interfaces.
- Use clear test names that describe the expected behavior.
- Prefer small hand-written fakes or stubs for simple dependencies.
- Use Moq only when mocking behavior is complex enough that it reduces test
  code complexity.
- Avoid testing private implementation details.
- Keep test data explicit, especially scheduled start times and time zones.

## Application Tests

- Test application handlers with fake repositories and gateways.
- Verify commands are mapped into domain models correctly.
- Verify returned results contain the expected identifiers and state.
- Add validation tests when command validation is introduced.

## Integration Tests

- Keep integration tests separate from unit tests when they require Azure
  storage, YouTube, WordPress, or authentication behavior.
- Treat the current `.http` files under
  `src/Test/YTSkedy.AzureFunctions.IntegrationTest/` as manual integration
  checks. They are not executed by `dotnet test`.
- Use Azurite for local Azure Table Storage checks unless a real storage
  account has been explicitly selected for the run.
- Keep deployed host URLs, function keys, and other personal HTTP environment
  values in `http-client.env.json.user`, not in tracked environment files.
- Do not create, update, or delete real YouTube resources by default.
- Use local emulators, fakes, or explicitly approved test resources for
  integration coverage.
