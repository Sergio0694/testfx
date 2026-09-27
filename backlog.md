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

- 2026-09-25: Added `ExtensionBuilderHelperTests.cs` (18 tests) for
  `src/Platform/Microsoft.Testing.Platform/Helpers/ExtensionBuilderHelper.cs` — a shipped internal API
  (`InternalAPI.Shipped.txt`) implementing the shared instantiate/validate-unique/enable-check/
  initialize/register loop used by `TestHostManager`, `TestHostOrchestratorManager`, and
  `TestHostControllersManager` to build every kind of platform extension. Previously had zero direct
  unit tests, only incidental exercise via the manager classes. Covers all 4 public methods: the simple
  `List<T>` overload and the ordered-tuple overload of `BuildAndRegisterExtensionsAsync` (enabled/disabled
  gating, `IAsyncInitializableExtension.InitializeAsync` invocation, duplicate-UID throw,
  `registerInServiceProvider` true/false, registration-order preservation); `BuildAndRegisterCompositeExtensionsAsync`
  (interface-implementation check throwing `InvalidOperationException`, singleton reuse via cloned
  factory — factory invoked only once across calls, disabled extensions recorded in `alreadyBuiltServices`
  without re-invocation but not added to result); `BuildAndRegisterCompositeExtensionsInPlaceAsync`
  (same interface check, in-place singleton reuse without cloning, service-provider registration,
  disabled extensions neither added nor registered). PR branch:
  test-assist/extension-builder-helper-tests. `Microsoft.Testing.Platform.UnitTests` net9.0: 2580 total /
  2559 passed / 21 skipped (pre-existing, unrelated) / 0 failed, up from 2562 total baseline (+18 new,
  all passing). Build with `-warnaserror` clean (0 warnings/errors).

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

## Cursor (2026-09-25 update)
- 2026-09-25: Confirmed no `[test-improver] Monthly Activity` issue existed yet for September (prior
  claim of having created one apparently did not land, or a previous run's `create_issue` call failed
  silently). Created it now via `create_issue` safe-output, listing all 22 currently-open PRs (#6-#27
  plus the new ExtensionBuilderHelper PR, referenced via temporary_id `#aw_ebht`) as Suggested Actions
  since none have been reviewed/merged/closed. `list_pull_requests` (via `github` CLI bridge) DOES work
  reliably in this sandbox (contradicts a prior note claiming otherwise) — used it directly to enumerate
  all open PRs in one call instead of probing individual issue numbers. `issue_read` on non-existent issue
  numbers correctly returns 404 (not empty results) — use that to confirm an issue truly doesn't exist yet.
- Next run: pivot Task 3 target away from `MSTestAdapter.PlatformServices` (exhausted on Linux) toward
  other untested internal `Helpers/`-style classes in `Microsoft.Testing.Platform`,
  `Microsoft.Testing.Extensions.*`, or `src/TestFramework/` — cross-reference existing
  `test/UnitTests/<Project>/**/*Tests.cs` file names against `src/**/*.cs` file names per subdirectory to
  find gaps quickly (as done this run for `Helpers/`). Also consider Task 4 (PR maintenance) given the
  growing backlog of unreviewed test-improver/perf-improver/efficiency-improver PRs — none merged yet
  after 4+ runs.

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

- 2026-09-26: Added `UILanguageOverrideTests.cs` (9 tests) for
  `src/Platform/Microsoft.Testing.Platform/UILanguageOverride.cs`'s `SetCultureSpecifiedByUser` — a
  previously-untested internal API implementing the CLI's UI-language precedence chain
  (`TESTINGPLATFORM_UI_LANGUAGE` > `DOTNET_CLI_UI_LANGUAGE` > `VSLANG`) plus flowing the resolved
  culture to 4 child-process env vars without clobbering already-set ones. Mutates process-wide
  `CultureInfo.DefaultThreadCurrentUICulture`, so the class is `[DoNotParallelize]` with
  save/restore in TestInitialize/TestCleanup (MSTEST0076 analyzer enforces this — caught it via a
  build error on first attempt). PR branch: test-assist/ui-language-override-tests.
  `Microsoft.Testing.Platform.UnitTests` net9.0: 2571 total / 2550 passed / 21 skipped (pre-existing) /
  0 failed, up from 2562 baseline (+9 new, all passing). `-warnaserror` build clean.
  LESSON LEARNED: `TESTINGPLATFORM_UI_LANGUAGE`, `DOTNET_CLI_UI_LANGUAGE`, and `VSLANG` are each BOTH a
  selection input AND a flow-to-children target in this method. First test draft asserted a variable
  was freshly SET while that same variable's value was also used to SELECT the culture (already
  "set" from the mock's perspective) — caused 3 real test failures against correct product behavior
  (verified by rereading `SetIfNotAlreadySet`/`FlowOverrideToChildProcesses`, not a product bug).
  Fixed by selecting the culture through a *different* variable than the one being asserted per test.
  Also: `CultureInfo.GetCultureInfo("not-a-real-culture")` does NOT throw (returns a custom culture
  named "not") — use a string with actually-invalid characters (e.g. "!!invalid!!") to test the
  `CultureNotFoundException` catch branch.

## Cursor (2026-09-26 update)
- `Microsoft.Testing.Platform` `Helpers/` namespace backlog testable on Linux is now essentially
  exhausted for classes with real logic. Remaining untested Helpers classes reviewed and explicitly
  NOT pursued (see reasons in Testing Opportunities Backlog of the monthly issue): `Sha256Hasher`
  (`[ExcludeFromCodeCoverage]` ported code), `ArgumentGuard`/`HashCode`/`StringBuilderExtensions`
  (trivial one-liners/polyfills gated `#if !NETCOREAPP` — untestable on this Linux net9.0 sandbox
  anyway), `ExtensionHelper`/`RuntimeFeatureHelper`/`StackTraceHelper` (thin wrappers/OS probes,
  already indirectly exercised).
- Next run: pivot to Task 4 (PR maintenance) — 25 open test/perf/efficiency-improver PRs, none
  merged/closed after 5+ runs despite multiple monthly summary nudges; or scan `src/TestFramework/`
  and `src/Analyzers/MSTest.Analyzers` (C# rules only, per repo-specific VB.NET exclusion) for
  untested internal APIs.

## Cursor (2026-09-27 update)
- 2026-09-27: Added `RetryThresholdPolicyTests.cs` (7 tests) for
  `src/Platform/Microsoft.Testing.Extensions.Retry/RetryThresholdPolicy.cs`'s `EvaluateAsync` —
  the `--retry-failed-tests-max-percentage`/`--retry-failed-tests-max-tests` failure-threshold
  policy that disables retrying and reports an explanation when the first attempt's failures
  exceed the configured threshold. Previously had zero direct unit tests, only incidental
  exercise via end-to-end acceptance tests. Used the existing `RetryDataConsumerTests.cs`
  service-provider construction pattern (`SystemEnvironment`/`SystemTask`/mocked
  `ILoggerFactory`) plus the existing `TestCommandLineOptions` test double, and drove the
  private `RetryFailedTestsPipeServer.CallbackAsync` via reflection (same technique as
  `HangDumpTests.cs`) to populate `TotalTestRan`/`FailedTestResults`/`FailedTests` without a
  full named-pipe round trip. Covers: no threshold set, percentage at/above threshold, count
  at/above threshold, count-threshold counting distinct uids not folded results, and
  percentage/count mutual exclusivity. PR branch: test-assist/retry-threshold-policy-tests.
  `Microsoft.Testing.Extensions.UnitTests` net9.0: 1883 total / 1846 passed / 37 skipped
  (pre-existing) / 0 failed, up from 1876 baseline (+7 new, all passing). `-warnaserror`
  build clean; `dotnet format whitespace --verify-no-changes` clean.
  LESSON LEARNED: `IRequest` (in `Microsoft.Testing.Platform.IPC`) is `internal`, and even
  though `InternalsVisibleTo` grants access, the test project has no direct
  `ProjectReference` to `Microsoft.Testing.Platform.csproj` (only to
  `Microsoft.Testing.Extensions.Retry.csproj`, which references it transitively) — referencing
  the `IRequest` type name directly in a method signature failed with CS0246 even though the
  type resolves fine when only used as an argument value (implicit reference via the
  `FailedTestRequest`/`TestRunCountsRequest` types already used elsewhere). Fixed by typing the
  reflection helper's parameter as `object` instead of `IRequest`.

- 2026-09-27: Verified via `pull_request_read` (`method: "get"`, `method: "get_status"`,
  `method: "get_check_runs"`, `method: "get_comments"`) that all 8 sampled open test-improver
  PRs (#32, #28, #25, #22, #19, #17, #13, #10) are still open/draft, `mergeable_state:
  "unstable"`, with `get_status`/`get_check_runs` both reporting zero checks configured (no CI
  pipeline runs against these PRs in this environment) and `get_comments` returning empty (no
  human feedback yet). No PR-maintenance action was needed or taken this run.
  CONFIRMED WORKAROUND: `pull_request_read` requires an explicit `--method`/`"method"` param
  (e.g. `"get"`) — omitting it returns `{"text":"missing required parameter: method", ...}`
  wrapped in `isError:true` inside `content[0].text`, NOT a top-level error, so a naive
  `d[0].get(...)` on the raw response silently returns `None`/empty without raising. Always
  check `d[0].get('isError')` and parse `content[0].text` on failure. Same applies to
  `issue_read` (also requires `--method "get"` explicitly).
- 2026-09-27: Confirmed (again) that `search_issues`/`list_issues` MCP calls return empty
  results in this sandbox; used `issue_read --method get` on sequential issue numbers instead.
  Issues #1-#3 integrity-filtered as before; #4-#5 closed unrelated bug-fix issues; #6-#33 are
  all pull requests (test/perf/efficiency-improver + 1 docs PR); issue #34+ returns 404 (does
  not exist). No `[test-improver] Monthly Activity` issue existed for September despite a
  2026-09-25 memory entry claiming one was created — that create_issue call apparently did not
  land (2nd time this has happened; consider verifying issue creation succeeded via a
  follow-up `issue_read` in a future run before recording it as done in memory). Created
  `[test-improver] Monthly Activity 2026-09` fresh this run with the full Suggested Actions
  list (PRs #10-#33 plus the new RetryThresholdPolicy PR) and a fresh Run History.
- Next run: pivot Task 3 target toward other `Microsoft.Testing.Extensions.*` projects
  (HtmlReport, JUnitReport, CtrfReport, TrxReport) or `src/TestFramework/`
  `Attributes/DataSource/` for untested internal helpers with real logic testable on Linux.
  Also worth a follow-up `issue_read` next run to confirm the Monthly Activity issue this run
  actually created landed (given the repeated apparent `create_issue` silent-failure pattern).
