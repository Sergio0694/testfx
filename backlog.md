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
- Last task 2 (opportunity discovery) scan: searched for files changed in 60 days (repo only has 1 commit
  of history available in this shallow clone — history-based "bug-prone area" heuristic is NOT usable here;
  rely on architecture/complexity reading instead).
- 2026-09-20: Systematically reviewed all files in `src/TestFramework/TestFramework/Internal/` for
  untested/undertested internal utilities. Remaining files in that folder (`StringEx`,
  `ApplicationStateGuard`, `ReflectionTestMethodInfo`, `TelemetryCollector`, `IEnvironment`,
  `TestDataSourceUtilities`, `CombinatorialValuesUtilities`, `DebugEx`, `DebuggerLaunchMode`) are now
  all either trivial (not worth testing), already well-covered, or covered by this run's/prior runs'
  PRs. Next opportunity scan should look at `src/TestFramework/TestFramework/Attributes/` (analyzers
  reviewed for C#-only scope) or `src/Adapter/` for untested internal logic.
- Next run: consider Task 4 (check on PR #8, #10, #13, and the new managed-name-parser-tests PR
  for CI status / maintainer feedback — as of 2026-09-21 all four had no CI checks reported yet,
  `pending`/0 statuses) or Task 6 (test infrastructure). Next opportunity scan candidates:
  `ManagedNameHelper` (see note above), `src/Adapter/MSTestAdapter.PlatformServices/Helpers/`
  remaining untested files (`ReflectHelper.cs`, `ManagedNameHelper.cs`), or
  `src/Adapter/MSTestAdapter.PlatformServices/Utilities/` (`AssemblyUtility.cs`,
  `RandomIntPermutation.cs`, `SequentialIntPermutation.cs` have no dedicated test files either).

## Monthly Activity Summary tracking
- Created `[test-improver] Monthly Activity 2026-09` issue on 2026-09-20 (no prior monthly issue existed
  in the repo). Listed PRs #8, #10, and the new combinatorial-values-utilities-tests PR as pending
  maintainer review actions.
- 2026-09-21: Re-checked for an open Monthly Activity issue — none found (search returned 0 results;
  the 2026-09-20 create_issue call may not have landed, or repo issue history is not queryable from
  this environment). Re-created `[test-improver] Monthly Activity 2026-09` issue with updated Run
  History (2026-09-18 through 2026-09-21) and Suggested Actions listing PRs #8, #10, #13, and the new
  managed-name-parser-tests PR. If a duplicate monthly issue turns out to exist, close the older one on
  the next run and keep only the most recent.
