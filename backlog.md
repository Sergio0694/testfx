# Testing Backlog (testfx) — cursor & opportunities

## Completed
- 2026-09-18: Added unit tests for `TestDataSourceUtilities.ComputeDefaultDisplayName`
  (src/TestFramework/TestFramework/Internal/TestDataSourceUtilities.cs) — previously had ZERO direct
  unit test coverage despite being a shipped internal API used by DataRowAttribute, DynamicDataAttribute,
  CombinatorialDataAttribute, TestMethodRunner.DataSource, and AssemblyEnumerator. Covers: null data,
  primitive formatting, string/char quoting, null argument literal, single-object-array-parameter
  special case, recursive nested array humanization, non-object array humanization, empty data array,
  ReflectionTestMethodInfo.DisplayName usage, and repeated-call StringBuilder-cache safety.
  PR #8 (branch test-assist/test-data-source-utilities-display-name). Still open as of 2026-09-19.
- 2026-09-19: Added 2 unit tests to `TestMethodRunnerTests.cs` covering the folded (`ITestDataSource`)
  data-driven path's display-name precedence in `TestMethodRunner.DataSource.cs`
  (`testDataSource.GetDisplayName(...) ?? ComputeDefaultDisplayName(...) ?? displayName`) — this
  precedence chain had no direct test, only incidental coverage via `DataRowAttribute`'s own
  `GetDisplayName`. Added a minimal private `CustomDisplayNameDataSourceAttribute : ITestDataSource`
  test double. PR branch: test-assist/test-method-runner-display-name-precedence.

- 2026-09-20: Added `CombinatorialValuesUtilitiesTests.cs` (4 tests) covering
  `CombinatorialValuesUtilities.GetValuesFor` branches not reached by existing
  `CombinatorialDataAttributeTests`: null-parameter guard, nullable-enum recursive inference
  (`Nullable.GetUnderlyingType` branch), nullable-unsupported-type exception message, and explicit
  `ICombinatorialValuesProvider` precedence over an otherwise-inferrable `bool` type. PR branch:
  test-assist/combinatorial-values-utilities-tests. 1570 passed (up from 1566 baseline) on
  TestFramework.UnitTests net9.0.

- 2026-09-22: Added `ManagedNameHelperTests.cs` (19 tests) for
  `src/Adapter/MSTestAdapter.PlatformServices/Helpers/ManagedNameHelper.cs` — a shipped internal API
  (`GetManagedNameAndHierarchy`, `GetMethod`, `ParseEscapedString`, all in `InternalAPI.Shipped.txt`)
  used by discovery/execution for the VSTest RFC-0017 managed name format, previously had zero direct
  unit tests (only incidental exercise via `TrimAndAotAssertions.cs`). Covers: null-arg guard, simple
  method (no parens for zero-arg), parameter-type/array-type formatting, generic-method arity, closed
  generic declaring type stripped to open generic definition, no-namespace hierarchy branch (separate
  file for the no-namespace fixture type since file-scoped namespace covers the whole file), `GetMethod`
  round-trips against 3 method shapes plus 3 `InvalidManagedNameException` paths, and `ParseEscapedString`
  passthrough/quoted/escaped-quote-backslash/unicode-escape plus 2 exception paths. PR branch:
  test-assist/managed-name-helper-tests. MSTestAdapter.PlatformServices.UnitTests net9.0 went from
  1088/1112 passed to 1105/1129 passed (24 pre-existing unrelated Windows-path failures unchanged).
  Nested fixture types mean `ClassIndex` hierarchy value is `ManagedNameHelperTests+SampleClass` (not a
  bare class name) — asserted correctly, learned this the hard way via first test-run failure.

## Backlog / opportunities not yet actioned
- `src/TestFramework/TestFramework/Internal/StringEx.cs` — reviewed 2026-09-19: these are trivial
  one-line pass-throughs around `string.IsNullOrEmpty`/`string.IsNullOrWhiteSpace`. NOT pursuing —
  no meaningful logic to test (see "What NOT to Test" guideline). Remove from backlog.
- `src/TestFramework/TestFramework/Internal/ApplicationStateGuard.cs` and
  `src/TestFramework/TestFramework/Internal/ReflectionTestMethodInfo.cs` reviewed 2026-09-20: mostly
  thin pass-throughs/wrappers around reflection `MethodInfo` members; `ReflectionTestMethodInfo` is
  already indirectly exercised via `TestDataSourceUtilitiesTests` (DisplayName usage) and
  `GetParameters()` caching behavior is covered there too. Not pursuing further — low marginal value.
  `ApplicationStateGuard.Unreachable` is a trivial exception-message formatter; not worth a dedicated
  test. `TelemetryCollector` and `IEnvironment`/`EnvironmentWrapper`/`CIEnvironmentDetector` already
  have adequate existing test coverage (`TelemetryCollectorTests.cs`, `CIEnvironmentDetectorTests.cs`).
- Review `src/Analyzers/MSTest.Analyzers` C# rule test coverage gaps (VB.NET tests are explicitly OUT OF
  SCOPE per repo-specific constraint — do not propose VB tests for analyzers).
- No coverage tooling was run this session (time-boxed); consider running the existing coverage pipeline
  (check azure-pipelines.yml / stryker-config.json for mutation testing config) in a future run to find
  quantified gaps rather than relying on git-history heuristics.
- The unfolded (non-`ITestDataSource`) path in `TestMethodRunner.DataSource.cs` uses the same
  `ExecuteTestWithDataSourceAsync` method but is invoked with `testDataSource: null` from
  `TestMethodRunner.cs` — that branch always takes the `displayNameFromTestDataRow ?? displayName`
  path (no `ComputeDefaultDisplayName` involved), already implicitly covered by existing `DataRow`-based
  tests. No further action needed there.

- 2026-09-21: Added `ManagedNameParserTests.cs` (12 tests) for
  `src/Adapter/MSTestAdapter.PlatformServices/Helpers/ManagedNameParser.cs` — a non-trivial
  recursive-descent parser (RFC 0017 managed-name grammar: method name/arity, F#-style quoted
  names, nested generic `<...>` and array `[...]` parameter brackets, several
  `InvalidManagedNameException` error paths) that previously had zero direct unit tests (only
  incidental exercise via `TrimAndAotAssertions.cs` acceptance test). PR branch:
  test-assist/managed-name-parser-tests. 1100 passed (up from 1088 baseline, +12) on
  MSTestAdapter.PlatformServices.UnitTests net9.0; 24 pre-existing unrelated failures unchanged.
  `ManagedNameHelper` itself (the caller, doing reflection-based name generation/lookup) still has
  no direct unit tests — candidate for a future run, though it's more entangled with live
  reflection (MethodBase/Type) than the pure-string ManagedNameParser, so tests would need real
  types/methods as fixtures rather than pure string-in/string-out cases.

## Cursor
- 2026-09-22: `ManagedNameHelper` now covered (see Completed). Remaining candidates in
  `src/Adapter/MSTestAdapter.PlatformServices/Helpers/` and `Utilities/`: `ReflectHelper.cs`,
  `AssemblyUtility.cs` (no dedicated test file). `RandomIntPermutation.cs`/`SequentialIntPermutation.cs`
  are wrapped in `#if NETFRAMEWORK` — cannot be built/tested on a net9.0-only Linux run; would need a
  net462 leg (Windows) to exercise directly, or a `#if` review to see if worth conditionally testing.
  Next opportunity scan: `ReflectHelper.cs`, `AssemblyUtility.cs`, or move to Task 4/6 (PR maintenance /
  test infrastructure) since the low-hanging internal-utility backlog in this area is getting thin.

## Monthly Activity Summary tracking
- 2026-09-22: Re-searched for open `[test-improver] Monthly Activity` issue — search_issues again
  returned 0 results (consistent with 2026-09-21 finding; either the issue creation isn't reliably
  landing/indexed, or this environment's search can't see issues created via safe-outputs in the same
  or a prior run). Created a new `[test-improver] Monthly Activity 2026-09` issue with Run History
  covering 2026-09-18 through 2026-09-22 and Suggested Actions listing PRs #8, #10, #13, #17, and the
  new ManagedNameHelper tests PR. IMPORTANT for next run: search for this issue by title fragment before
  assuming none exists — if duplicates are ever found, close all but the most recent and note it here.
