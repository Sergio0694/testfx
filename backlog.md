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

## Cursor
- 2026-09-23: `ReflectHelper` static assembly-level helpers now covered (see Completed). Remaining
  candidates in `src/Adapter/MSTestAdapter.PlatformServices/Helpers/` and `Utilities/`:
  `AssemblyUtility.cs` — only `IsAssemblyExtension` is testable on Linux (small case-insensitive
  extension-matching method); the rest (`IsAssembly`, `GetSatelliteAssemblies`,
  `GetFullPathToDependentAssemblies`) is `#if NETFRAMEWORK`-gated AppDomain machinery needing a Windows
  leg. `RandomIntPermutation.cs`/`SequentialIntPermutation.cs` are wrapped in `#if NETFRAMEWORK` too —
  same Windows-only limitation. The low-hanging internal-utility backlog testable on Linux net9.0 is now
  quite thin; next run should consider Task 4 (PR maintenance — 6 open test-improver PRs awaiting
  review/merge) or Task 6 (test infrastructure) before further scanning for individual untested internal
  utilities.

## Monthly Activity Summary tracking
- 2026-09-23: Again searched for open `[test-improver] Monthly Activity` issue (list_issues with
  label:testing, state:OPEN) — 0 results, same as every prior run this month. Created a new
  `[test-improver] Monthly Activity 2026-09` issue with full Run History 2026-09-18 → 2026-09-23 and
  Suggested Actions listing PRs #8, #10, #13, #17, #19, and the new ReflectHelperTests PR
  (test-assist/reflect-helper-tests). STRONG SUSPICION: each run's `create_issue` safe-output call is
  landing as a *new* issue every time (not being found/updated) — likely because the safe-outputs
  handler processes issues asynchronously after the workflow session ends, so this session's read-only
  GitHub queries can never see issues created by earlier runs. If a maintainer reports multiple
  `[test-improver] Monthly Activity 2026-09` issues piling up, that confirms this theory — the fix would
  be to track the issue number in memory directly (once known from a safe-output response or maintainer
  comment) rather than relying on search. Next run: check whether maintainer closed/consolidated any
  duplicates and note the surviving issue number here if mentioned.
