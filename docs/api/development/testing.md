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
- Prefer Moq for simple public interfaces when a test only needs configured
  results, exceptions, callbacks, or focused call verification.
- Use loose `Mock<T>` instances. Configure query results explicitly with
  `Setup` and `ReturnsAsync`, and configure expected failures with
  `ThrowsAsync`.
- Use `Callback` when the test needs to inspect several properties of a
  command or provider request. Keep the callback in the owning test.
- Use `Verify` only for behaviorally important commands and arguments. Use
  `Times.Never()` for guard paths where suppressing a dependency call is part
  of the contract.
- Do not use auto-mocking containers, recursive mock defaults, global strict
  mocks, blanket `VerifyAll`, or blanket `VerifyNoOtherCalls`.
- Use `LoggerMockExtensions.GetLogEntries` and `GetLogText` when a test needs to
  inspect the rendered level or message from a mocked `ILogger<T>`.
- Avoid testing private implementation details.
- Keep test data explicit, especially scheduled start times and time zones.

## Application Tests

- Test application handlers through their public repository and gateway
  interfaces. Arrange simple dependencies with Moq close to the behavior that
  uses them.
- Verify commands are mapped into domain models correctly.
- Verify returned results contain the expected identifiers and state.
- Add validation tests for command validation behavior.

## Retained Test Adapters

Keep a hand-written adapter when it concentrates framework or integration
semantics that a dynamic mock would make less clear. Current examples include:

- Azure SDK table clients, responses, paging, ETags, and blob containers.
- Protected `HttpMessageHandler` behavior and HTTP request capture.
- Deterministic `TimeProvider` behavior.
- Azure Functions context, definition, and invocation-feature objects.
- Generic publishing execution-scope finalization and cancellation behavior.
- Internal infrastructure interfaces that would otherwise require production
  `DynamicProxyGenAssembly2` friend access.

Do not change production visibility, interfaces, constructors, dependency
injection, or friend access only to make a type mockable. A retained adapter
should own meaningful reusable behavior, not merely wrap Moq or hide a setup.

## Integration Tests

- Keep integration tests separate from unit tests when they require Azure
  storage, a live backend host, YouTube, WordPress, or authentication behavior.
- Use Azurite for local Azure Table Storage checks unless a real storage
  account has been explicitly selected for the run.
- Keep deployed host URLs, bearer access tokens, and other personal environment
  values outside tracked files.
- Do not create, update, or delete real YouTube resources by default.
- Use local emulators, fakes, or explicitly approved test resources for
  integration coverage.

## Commands

Runnable API commands are listed in
[`build-and-test.md`](build-and-test.md).
