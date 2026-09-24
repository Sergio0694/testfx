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

- 2026-09-23: Added `ReflectHelperTests.cs` (12 tests) for
  `src/Adapter/MSTestAdapter.PlatformServices/Helpers/ReflectHelper.cs` — 6 shipped internal static
  helpers (`GetParallelizeAttribute`, `HasDiscoverInternalsAttribute`, `GetTestDataSourceDiscoveryOption`,
  `GetTestDataSourceOptions`, `IsDoNotParallelizeSet`, `MatchReturnType`) previously had zero direct unit
  tests, only incidental exercise via `AssemblyEnumerator`. Used `AssemblyBuilder.DefineDynamicAssembly`
  (same pattern as `TypeCacheTestFilterProviderTests.cs`) to construct minimal in-memory assemblies with/
  without the target attribute, covering both "absent" and "present with a specific value" branches for
  each. PR branch: test-assist/reflect-helper-tests. `MSTestAdapter.PlatformServices.UnitTests` net9.0:
  1,122 total / 1,098 passed / 24 failed (pre-existing, unrelated). All 12 new tests passed.

- 2026-09-24: Added `AssemblyUtilityTests.cs` (6 tests) for
  `src/Adapter/MSTestAdapter.PlatformServices/Utilities/AssemblyUtility.cs`'s `IsAssemblyExtension` —
  the only member of that class NOT gated behind `#if NETFRAMEWORK`, so testable on Linux net9.0. Covers
  `.dll`/`.exe` true, case-insensitivity, non-assembly extension, missing leading dot, and empty string.
  PR branch: test-assist/assembly-utility-is-assembly-extension. `MSTestAdapter.PlatformServices.UnitTests`
  net9.0: 1116 total / 1092 passed / 24 failed (pre-existing, unrelated) after adding the 6 new tests.
  All 6 new tests passed. (Note: absolute totals vs. prior runs aren't directly comparable since each
  session's baseline only reflects PRs merged to main, not other still-open test-improver PR branches.)

## Cursor
- 2026-09-24: `AssemblyUtility.IsAssemblyExtension` now covered (see above). The low-hanging
  internal-utility backlog testable on Linux net9.0 in `src/Adapter/MSTestAdapter.PlatformServices/`
  (`Helpers/` and `Utilities/`) is now essentially exhausted — remaining candidates there
  (`AssemblyUtility.IsAssembly`, `GetSatelliteAssemblies`, `GetFullPathToDependentAssemblies`,
  `RandomIntPermutation.cs`, `SequentialIntPermutation.cs`) are all `#if NETFRAMEWORK`-gated and need a
  Windows leg. Next run should pivot to: (a) Task 4 — PR maintenance, since 18+ test/perf/efficiency-improver
  PRs are open awaiting review with none yet merged/closed; (b) scanning other product areas
  (`src/TestFramework/`, `src/Platform/`, `src/Analyzers/`) for untested internal APIs testable on Linux;
  or (c) Task 6 — test infrastructure investment.

## Monthly Activity Summary tracking
- 2026-09-24: Verified via `github issue_read` on individual issue numbers (list_issues/search_issues both
  return empty results in this sandbox for unknown reasons — reads work fine when the number is known
  directly) that issues #1-#3 are integrity-filtered, #4-#5 are closed unrelated bug-fix issues, and
  #6-#24 are ALL pull requests (perf/efficiency/test-improver, all still open, none merged/closed). Issue
  numbers 25+ do not exist yet (404), confirming NO `[test-improver] Monthly Activity` issue currently
  exists — prior runs' `create_issue` calls may not have landed, or this is genuinely the first one to
  land. Created `[test-improver] Monthly Activity 2026-09` this run with full Suggested Actions list
  (PRs #6-#24 plus the new AssemblyUtilityTests PR) and Run History starting fresh. CONFIRMED WORKAROUND
  for future runs: `list_issues`/`search_issues` MCP calls appear broken/empty in this environment
  (returns `totalCount: 0` even for `state: "all"` with no filters) — use `github issue_read` with
  sequential issue numbers instead to enumerate real issues, or rely on PR-derived issue numbers from
  `list_pull_requests` (which DOES work) since every PR in this repo is also an "issue" under the GitHub
  API. Next run: use `issue_read` on issue numbers above the highest known PR number to find the Monthly
  Activity issue once created, rather than trusting `list_issues`/`search_issues`.
