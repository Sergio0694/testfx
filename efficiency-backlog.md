# Efficiency Backlog

## Completed
- HIGH (Code-Level): `SynchronousAwaiter.Await` (VSTestBridge, `Helpers/SynchronousAwaiter.cs`) defaulted
  `busyWait: true`, a SpinWait-based CPU busy-spin, on 7 call sites: `FrameworkHandlerAdapter.RecordResult`
  /`RecordStart`/`RecordAttachments` (once per test result/start, i.e. proportional to test count),
  `MessageLoggerAdapter.SendMessage` x3, `TestCaseDiscoverySinkAdapter.SendTestCase` (once per discovered
  test). Verified the entire downstream async chain (AsynchronousMessageBus, AsyncConsumerDataProcessor/
  BlockingConsumerDataProcessor, MessageBusProxy, ProxyOutputDevice) is ConfigureAwait(false) end-to-end with
  no SynchronizationContext capture, so blocking via GetAwaiter().GetResult() cannot deadlock. Flipped the
  default to `busyWait: false`. Measured via standalone (uncommitted) benchmark: BenchmarkDotNet
  steady-state Channel<T> write/read (near-instant completion, best case for busy-wait) showed BusyWait
  23.15ns vs Blocking 21.18ns (~9% CPU reduction even in the best case); manual TotalProcessorTime
  comparison with realistic 50us consumer delay (simulating report-writer work) over 2000 iterations showed
  BusyWait 294-475us CPU/op vs Blocking 189-210us CPU/op (~1.5-2.3x more CPU under busy-wait once the
  awaited task takes any real time). Required updating InternalAPI.Shipped.txt (default-value-only
  signature change to a tracked internal API). PR: efficiency/synchronous-awaiter-blocking-default. Full
  build of Microsoft.Testing.Extensions.VSTestBridge.csproj (all 3 TFMs) clean 0 warnings/errors;
  Microsoft.Testing.Extensions.VSTestBridge.UnitTests (net9.0) 97/97 passed; format check clean.
- HIGH (Code-Level): `JUnitReportMerger.MergeRetryAttempts` did 3x `Elements().Any(...)` scans per
  test case to detect failure/error/skipped. Replaced with single foreach.
  PR: efficiency/junit-merge-single-pass-status. Measured via standalone microbenchmark
  (XElement testcases, various sizes): ~3.2x time speedup, 66.7% allocation reduction at
  1000-10000 testcases. Small collections (100) still ~1.4x / 66.7% alloc reduction.
  Exercised whenever `--report-junit` + retry-merge (CollapseRetryAttempts mode) runs, e.g.
  CI with `--retry-failed-tests` and JUnit report enabled.
- Task 6 (measurement infrastructure): Added `JUnitReportMergerBenchmarks` to
  test/Performance/MSTest.Performance.Benchmarks covering Merge(Concatenate) small/large and
  Merge(CollapseRetryAttempts). Required adding InternalsVisibleTo for MSTest.Performance.Benchmarks
  to Microsoft.Testing.Extensions.JUnitReport.csproj + a ProjectReference (mirrors existing IVT pattern
  from TestFramework.csproj/MSTestAdapter.PlatformServices.csproj). PR:
  efficiency/junit-merger-benchmark. Baseline numbers (--job short, informational): Concatenate small
  report 3.85us/9.58KB, Concatenate large (5x200) 104.3us/370.67KB, CollapseRetryAttempts large (3
  reports x 5x200) 2.62ms/7475.97KB.

## Investigated, NOT pursued (already well-optimized or too risky/low-confidence this run)
- Regex usage across repo (StackTraceHelper x2, QuarantineFile, AzureDevOpsReporter stack-frame
  filters, Assert.Matches ToRegex): all already cached/compiled/lazy. No action needed.
- ReflectHelper / attribute reflection (Adapter): attribute caching centralized in
  ReflectionOperations._attributeCache already. No action needed.
- TestDataSourceUtilities.ComputeDefaultDisplayName: already uses ThreadStatic StringBuilder pool
  and ConditionalWeakTable method-shape cache. No action needed.
- AsynchronousMessageBus: core message bus, complex/sensitive, did not touch this run.
- DependsOnShouldBeValidAnalyzer (Roslyn analyzer, cycle detection via DFS): potential caching
  opportunity for EnumerateMethods/GetDependsOnAttributes results across repeated calls in a single
  compilation pass, but this is analyzer code (build-time hot path, runs on every build) — worth
  investigating in a future run with more time for careful correctness verification (Roslyn analyzer
  changes are high-risk for subtle regressions).
- TrxReportEngine attachment relocation, HtmlReportMerger, AzureDevOpsResultIdStore: reviewed,
  already use Dictionary/HashSet-based dedup; no O(n^2) patterns found.

## Candidates for future runs (not yet measured/implemented)
- MEDIUM: DependsOnShouldBeValidAnalyzer caching (see above) — needs careful review given analyzer
  correctness sensitivity.
- LOW: Terminal rendering code (AnsiTerminalTestProgressFrame, TerminalTestReporter.Formatting.cs)
  reviewed this run — both already heavily pre-optimized (pooled buffers, cached ANSI escape strings,
  reused comparer). No further action identified.

- Task 6 (measurement infrastructure): Added `HtmlReportMergerBenchmarks` to
  test/Performance/MSTest.Performance.Benchmarks covering Merge(Concatenate) small/large and
  Merge(CollapseRetryAttempts). Required adding InternalsVisibleTo for MSTest.Performance.Benchmarks
  to Microsoft.Testing.Extensions.HtmlReport.csproj + a ProjectReference (same pattern as the
  JUnitReportMerger benchmark PR). PR: efficiency/html-report-merger-benchmark. Baseline numbers
  (--job short, informational): Concatenate small (2x10) 133.8us/430.63KB, Concatenate large (5x200)
  2711.2us/3612.52KB (8.39x alloc ratio), CollapseRetryAttempts large (3 reports x 5x200)
  1792.3us/2409.14KB (5.59x alloc ratio, cheaper than Concatenate-large because it folds matching test
  identities into one row instead of concatenating every row).

## Run 2026-09-22
- Investigated `RetryArgumentsBuilder.BuildAttemptArgumentsAsync` (src/Platform/Microsoft.Testing.Extensions.Retry):
  the two `indexToCleanup.Contains(i)` loops looked like a classic O(n*m) List-lookup pattern (candidate for
  HashSet<int>). Built a standalone microbenchmark (System.Runtime, `dotnet run -c Release`, realistic sizes:
  executableArguments.Length 10-100, indexToCleanup 2-10 entries, 500k iterations) comparing List.Contains vs.
  building+using a HashSet<int> per call. Result: HashSet was *slower* at every realistic size (ratio
  list/hashset 0.46x-0.85x, i.e. HashSet took 1.2x-2.2x longer) because this method runs once per retry attempt
  with only a handful of cleanup indices — HashSet construction overhead dominates over such tiny N. Reverted
  the change; not pursued. Lesson for future runs: always microbenchmark with the *actual* call-site problem
  size before assuming HashSet/Dictionary beats List for small N — the crossover point for List.Contains vs.
  HashSet.Contains in this repo's typical CLI-argument-sized collections (rarely >100 elements) is well above
  what most command-line-argument-processing code here handles per call.
- Reviewed DependsOnShouldBeValidAnalyzer.AnalyzeNamedType/EnumerateEffectiveMethods/HasDuplicateSignature again:
  each SymbolAction callback already scopes its own type-hierarchy walk (bounded by inheritance depth, not
  compilation size), and the walk itself already uses HashSet-based signature/overridden-method dedup. No
  additional caching opportunity found without changing correctness-sensitive AnalysisSymbols plumbing across
  callback invocations (Roslyn analyzers don't get a natural safe per-compilation cache without a
  ConcurrentDictionary keyed by symbol, which adds contention risk for a rarely-hot path). Leaving open as a
  LOW priority item, deprioritized further given no incoming issue/PR pressure on this analyzer's performance.
- No other new HIGH/MEDIUM opportunities found this run after scanning: ServiceProvider._services (List, but
  bounded by extension count, tiny), PropertyBag (already extensively optimized, confirmed no regression
  candidates), CiCoverageSummary (LINQ-heavy but runs once per test session, not hot), AzureDevOpsSummaryReporter
  detailedFailures.Contains (bounded by failureDetailLimit, typically small).
- Repo currently has zero GitHub issues (fresh fork state - only PRs exist, mostly opened by prior
  efficiency-improver/perf-improver/test-improver runs). No efficiency-tagged issues to comment on this run.
- All 4 open efficiency-improver PRs (#7, #9, #12, #16) still show CI status "pending" with 0 reported statuses
  (checked via pull_request_read get_status) — consistent with previous runs' notes that this is
  infra/CI-configuration related, not a code problem. Did not push changes to them this run.

## Run 2026-09-23
- Task 6 (measurement infrastructure): Added `CtrfReportMergerBenchmarks` to
  test/Performance/MSTest.Performance.Benchmarks covering Merge(Concatenate) small (2x10)/large (5x200)
  and Merge(CollapseRetryAttempts) (3 reports x 200 shared-identity tests). Required InternalsVisibleTo
  for MSTest.Performance.Benchmarks in Microsoft.Testing.Extensions.CtrfReport.csproj + ProjectReference
  (same pattern as JUnit/HTML/TRX merger benchmarks — CtrfReportMerger is a 4th JSON-based merge engine
  that had zero benchmark coverage until now). PR: efficiency/ctrf-report-merger-benchmark. Baseline
  numbers (--job short, informational): Merge_SmallReport 52.44us/71.05KB, Merge_LargeReport
  2384.91us/2766.31KB (38.94x alloc ratio), Merge_LargeReport_CollapseRetryAttempts
  1904.07us/2410.15KB (33.92x alloc ratio — cheaper than Concatenate-large because it folds 1000 rows
  down to 200 final rows despite each carrying extra retryAttempts[] history). Full ./build.sh (Debug)
  ran clean (0 warnings/errors) confirming the new IVT entry doesn't break anything; format check on the
  new file was clean.
- Task 2 scan this run: reviewed Contains()-in-loop patterns across ~15 more Platform/Extensions files
  (TreeNodeFilter, ArtifactPostProcessingManager/DispatcherTool, CommandLineOptionsValidator
  Registration/UnknownAndBootstrap, CommonTestHost/TestHostControllersTestHost disposal,
  GitHubActionsSummaryArtifactPostProcessor, AzureDevOpsSummaryReporter.Markdown,
  RetryOrchestrator/RetryArtifactProcessor, CtrfReportEngine.InProcessRetries, FrameworkHandlerAdapter).
  All are already HashSet/Dictionary-based or operate on small/bounded collections (CLI option counts,
  active service lists, failureDetailLimit-bounded arrays). No new O(n^2) opportunities found — this
  codebase's Contains()-in-loop instances are consistently either already deduped via HashSet or
  genuinely small-N (confirms the general pattern noted in the 2026-09-22 run's RetryArgumentsBuilder
  finding: small-N collections in this repo are not worth converting to HashSet).
- No efficiency/performance/green-software issues exist in the repo currently (repo has 0 open issues
  total, confirmed via list_issues and label search). Task 5 (comment on issues) not applicable this run.
- Checked CI status on 4 open efficiency-improver PRs (#7, #9, #12, #16): all still show state "pending"
  with 0 reported statuses via get_status — consistent with every prior run's observation that this is
  infra/CI-configuration related, not a code problem caused by these PRs. Did not push changes to them.

## Backlog cursor (updated 2026-09-23)
All 4 report-merger engines (JUnit/HTML/TRX/CTRF) now have BenchmarkDotNet coverage — Task 6's
report-merger sweep is DONE. Next Task 6 candidates to scope out: ServerMode/IPC JsonRpc serialization
paths (Jsonite is a planned rewrite target per its own code comment, so benchmark it read-only for
before/after evidence rather than touching its internals) — not yet benchmarked. Also consider
CommandLineOptionsValidator (startup/CLI-parse hot path, runs once per process start but on every
`dotnet test`/`dotnet run` invocation across the whole ecosystem) as a Task 6 candidate: no benchmark
exists for option validation/parsing despite it running unconditionally on every invocation.
Task 2: DependsOnShouldBeValidAnalyzer caching still not attempted (flagged high-risk for analyzer
correctness across multiple runs now — deprioritize further unless a maintainer/issue signals interest).
Broad Contains()-in-loop sweep across Platform/Extensions (this run) found nothing new — do not re-scan
the same files again next run; instead pick a different corner of the codebase (e.g. Adapter
TestMethodRunner internals, or MSBuild task code, not yet swept for Contains()-in-loop patterns).
PRs #7, #9, #12, #16 (all efficiency-improver, draft) still show CI "pending" with 0 statuses — this has
been consistent across at least 3 runs now, strongly suggesting infra/CI config issue unrelated to PR
content; consider flagging this pattern explicitly in the Monthly Activity issue for maintainer attention
if it persists past the next run or two.

## Run 2026-09-21
- Task 6: Added `TrxReportEngineMergerBenchmarks` (Merge_SmallReport 2x10, Merge_LargeReport 5x200) to
  test/Performance/MSTest.Performance.Benchmarks. Required InternalsVisibleTo for
  MSTest.Performance.Benchmarks in Microsoft.Testing.Extensions.TrxReport.csproj + ProjectReference (same
  pattern as JUnit/HtmlReportMerger benchmarks). All three report-merger engines (JUnit, HTML, TRX) now
  have benchmark coverage. PR: efficiency/trx-report-merger-benchmark. Baseline numbers (--job short,
  informational): Merge_SmallReport 13.90us/28.95KB, Merge_LargeReport 416.28us/1082.28KB (37.38x alloc
  ratio vs. baseline).
- Note: BenchmarkDotNet's first "generated boilerplate" build attempt can hit its internal 2-minute
  timeout on a cold artifacts/ dir even though `dotnet build` of the same autogenerated csproj succeeds
  in ~15s standalone — if a first `--job short` run reports "failed to build the auto-generated
  boilerplate code", just retry the same `dotnet run ... --job short` command once (warm build cache
  fixes it); don't assume the benchmark itself is broken.
- Backlog cursor update: all three report-merger benchmarks (JUnit/HTML/TRX) are now DONE. Remaining
  Task 6 candidates: none identified yet for report mergers; next Task 6 candidate to scope out is
  possibly ServerMode/IPC serialization paths (see below) once/if a rewrite plan solidifies, or discovering
  entirely new energy-critical paths lacking coverage (Adapter TestMethodRunner discovery, TypeCache) —
  not yet benchmarked despite being reviewed as "already optimized" in Task 2.

## Run 2026-09-24
- Task 2: Delegated a scan of previously-unreviewed Adapter Execution files (TestMethodRunner.cs +
  3 partials, TypeCache.cs + 4 partials) to a sub-agent. Result: all already heavily optimized
  (explicit PERF comments, _classInfoCache/_testAssemblyInfoCache, GetCustomAttributesCached,
  cached ReflectionTestMethodInfo). Three minor LOW-priority notes recorded (FilterDiscovery LINQ
  array projection, IsFrameworkAssemblyName StartsWith chain, per-data-row TestContextImplementation
  clone which is intentional for correctness per issue #7933) — none worth action this run.
- Task 6 (measurement infrastructure): Added `CommandLineOptionsValidatorBenchmarks` to
  test/Performance/MSTest.Performance.Benchmarks, covering CommandLineOptionsValidator.ValidateAsync
  with few (2) vs. many (12) registered extension providers. This was the Task 6 candidate flagged
  in the 2026-09-23 backlog cursor note (startup/CLI-parse hot path, runs unconditionally on every
  dotnet test/dotnet run invocation, had zero benchmark coverage). Required InternalsVisibleTo for
  MSTest.Performance.Benchmarks in Microsoft.Testing.Platform.csproj (CommandLineOptionsValidator is
  `internal static class`) + an explicit ProjectReference to Microsoft.Testing.Platform.csproj in
  MSTest.Performance.Benchmarks.csproj (previously only transitively referenced). PR:
  efficiency/commandline-validator-benchmark. Baseline numbers (--job short, informational):
  ValidateAsync_FewExtensions (2 extensions, 27 options) 83.35us/45.76KB, ValidateAsync_ManyExtensions
  (12 extensions, 52 options) 152.32us/85.18KB (1.83x time, 1.86x alloc vs few-extensions baseline) —
  cost scales roughly linearly with extension/option count, consistent with the ToDictionary/SelectMany
  provider-and-option lookup construction in ValidateAsync. Full ./build.sh (Debug) ran clean (0
  warnings/errors); format check on the new file was clean.
- Repo still has 0 open issues total (confirmed via list_issues state=open and state=all) — no
  Monthly Activity issue exists yet this run (need to create fresh, not just update).
- Checked CI status on the two remaining open efficiency-improver PRs still tracked from prior runs
  (#9 JUnitReportMerger benchmark, #12 HtmlReportMerger benchmark, #16 TrxReportEngine benchmark,
  #21 CtrfReportMerger benchmark - all still open) plus #7 (JUnit single-pass fix): all 5 still show
  CI state "pending" with 0 reported statuses via get_status. This is now consistent across 4+ runs -
  flagged explicitly in this run's Monthly Activity issue for maintainer attention as it strongly
  suggests an infra/CI-configuration gap (e.g. missing workflow trigger for PRs from this bot) rather
  than anything wrong with the PRs' content.

## Run 2026-09-25
- Task 3 (implement): Delegated a fresh Task 2 sweep of MSBuild task code
  (Microsoft.Testing.Platform.MSBuild + Microsoft.Testing.Extensions.MSBuild, per prior backlog
  cursor) to a sub-agent — result: nothing worth pursuing (all cold, once-per-build code paths;
  bounded N; already-optimized hot spots per prior sweeps). Pivoted to the other flagged Task 6/2
  candidate: ServerMode/IPC JsonRpc serialization (`Json.cs`). Found `Json.SerializeAsync` recursed
  through Task-returning async methods (SerializeAsync(obj, writer)) to write a JSON object graph via
  Utf8JsonWriter, even though every write target is an in-memory MemoryStream and every registered
  JsonValueSerializer.Serialize delegate is a plain synchronous Action<Utf8JsonWriter, object> — no
  actual async I/O anywhere in the recursion. This runs on every JSON-RPC message in MTP server mode
  (one call per test-node/test-result update), a genuinely hot path (not the cold-build-time code
  flagged as low-priority in past runs). Converted the recursive helper to synchronous (Task.FromResult
  wrapping only at the public entry point); required a narrowly-scoped
  `#pragma warning disable/restore VSTHRD103` around the now-synchronous `Flush()` call (destination is
  a MemoryStream, so no real I/O to await — analyzer can't know that). PR:
  efficiency/json-serializer-sync-recursion. Measured via a standalone (not committed) BenchmarkDotNet
  microbenchmark mirroring the recursive-write shape (nested Dictionary/List graph, depth 3, fanout 5):
  SerializeAsync_RecursiveAwait (before) 6.421us vs. Serialize_SynchronousRecursion (after) 4.646us —
  ~28% CPU time reduction (ratio 0.72), allocated bytes identical in this synthetic model (9.27 KB both
  — completed-synchronously state-machine overhead here is mostly CPU cycles, not GC-visible heap).
  Full ./build.sh (Debug) clean (0 warnings/errors); built Microsoft.Testing.Platform.csproj across all
  3 TFMs (net8.0/net9.0/netstandard2.0) individually plus the sibling
  Microsoft.Testing.Platform.ServerMode.Client.Sources.csproj (shares the modified file via linked
  Compile item) — all clean. Ran full Microsoft.Testing.Platform.UnitTests suite (net9.0): 2563 total,
  2542 passed, 0 failed, 21 skipped. Format check clean.
- Note for future runs: this is the first efficiency-improver PR of a genuinely *hot* runtime code path
  (not a cold build-time or once-per-build-invocation path, and not just adding benchmark coverage) in
  several runs — the ServerMode/IPC JsonRpc area (flagged since the 2026-09-23/24 backlog cursor notes)
  turned out to have real opportunity once actually read, contrary to the earlier assumption that it was
  only worth a read-only benchmark. Worth revisiting other Json.cs/JsonRpc files
  (SerializerUtilities.TestNodeSerializers.cs, Json.TestNodeSerializer.cs, JsonReflector.cs in the
  netstandard2.0 Jsonite fallback) in a future run for similar unnecessary-async patterns — not yet
  swept this run (ran out of scope after finding and fully validating the Json.cs fix).
- Verified all 6 open efficiency-improver PRs (#7, #9, #12, #16, #21, #24) again via get_status: all
  still show state "pending" with 0 reported statuses — persistent pattern now spans 5+ runs. No new
  efficiency/performance-labeled issues exist (repo has 0 open issues total). Task 5 not applicable.

## Backlog cursor (updated 2026-09-25)
Task 2/3: ServerMode/IPC JsonRpc Json.cs unnecessary-async-recursion fix is DONE (PR
efficiency/json-serializer-sync-recursion). Next candidates in this area, not yet reviewed: other
Json.cs sibling files with similar patterns (SerializerUtilities.TestNodeSerializers.cs — check for
async recursion or per-call reflection; Json.TestNodeSerializer.cs; the netstandard2.0 Jsonite fallback
under Jsonite/ — JsonReflector.cs in particular does per-object-graph reflection and may have caching
opportunities). Also still not benchmarked: TypeCache assembly/class discovery end-to-end (coarser-
grained than already-declined per-method micro-benchmarks).
Task 2: MSBuild task code (Microsoft.Testing.Platform.MSBuild / Microsoft.Testing.Extensions.MSBuild)
now confirmed swept and empty — do not re-scan next run. Remaining unswept corners: VSTestBridge
adapter-shim code (still not reviewed after 2+ runs of being listed as a candidate).
CI "pending with 0 statuses" pattern persists across #7, #9, #12, #16, #21, #24 (6 PRs now, 5+ runs) -
keep flagging in Monthly Activity issue; do not keep retrying pushes to "fix" it.

## Run 2026-09-26
- Task 2/3: Delegated a sub-agent scan of VSTestBridge adapter-shim code (the last unswept corner flagged
  since the 2026-09-25 backlog cursor). Found `SynchronousAwaiter.Await`'s `busyWait: true` default
  (see Completed section above) — implemented and shipped as PR efficiency/synchronous-awaiter-blocking-
  default. Sub-agent also flagged smaller MEDIUM/LOW items not pursued this run: `ObjectModelConverters.
  CopyMSTestDependencies`'s `dependencies.Take(64)` LINQ iterator allocation per test case (small, <64
  items, borderline LOW/MEDIUM — could switch to indexed for-loop but low volume per call); `RunSettings
  Patcher.PatchTestRunParameters`'s O(n*m) FirstOrDefault-in-loop (both n/m always <100, session-level not
  per-test-case, consistent with repo's small-N convention — no action). Nothing else in VSTestBridge
  warranted action (RunSettingsAdapter/RunSettingsPatcher, SynchronizedSingleSessionVSTestAndTestAnywhere
  Adapter, RunContextAdapter/DiscoveryContextAdapter, request factories already reviewed as session-level/
  once-per-request, not hot).
- Verified all 7 open efficiency-improver PRs (#7, #9, #12, #16, #21, #24, #27) again via get_status: all
  still show state "pending" with 0 reported statuses — persistent pattern now spans 6+ runs. Continuing
  to flag in Monthly Activity issue without retrying pushes (per repo's own guidance after 3+ runs of the
  same infra symptom).
- Repo still has 0 open issues total (list_issues state=open and search_issues both returned empty) — no
  efficiency/performance-labeled issues to comment on (Task 5 not applicable), and no existing Monthly
  Activity issue to update (need to create fresh again this run, as in 2026-09-24).

## Backlog cursor (updated 2026-09-26)
Task 2/3: VSTestBridge adapter-shim sweep is DONE (SynchronousAwaiter busy-wait fix shipped). Remaining
VSTestBridge candidates, not worth pursuing yet per sub-agent's LOW/MEDIUM findings: ObjectModelConverters.
CopyMSTestDependencies Take(64) iterator allocation (small volume, borderline). Next genuinely unswept
corners for a future run: Adapter/MSTestAdapter.PlatformServices discovery-service implementations (not
yet reviewed - distinct from the already-swept TestMethodRunner/TypeCache in 2026-09-24), or the
ServerMode/IPC Json.cs sibling files flagged since 2026-09-25 (SerializerUtilities.TestNodeSerializers.cs,
Json.TestNodeSerializer.cs, JsonReflector.cs netstandard2.0 Jsonite fallback) - still not swept for
similar unnecessary-async or caching patterns as the original Json.cs fix (PR #27, still open/unmerged).
CI "pending with 0 statuses" pattern persists across 7 PRs now (#7, #9, #12, #16, #21, #24, #27), 6+ runs -
keep flagging in Monthly Activity, do not retry pushes.

## Backlog cursor (updated 2026-09-24)
Task 6: CommandLineOptionsValidator benchmark is DONE (was the last flagged candidate from
2026-09-23). Next Task 6 candidates to scope out: ServerMode/IPC JsonRpc serialization paths
(Jsonite - still a planned rewrite target per its own code comment, benchmark read-only) - not yet
benchmarked, still the best next candidate. Also consider: TypeCache assembly/class discovery
end-to-end (not individual already-cached helper methods, but the full per-assembly discovery walk)
as a coarser-grained benchmark target distinct from the per-method micro-benchmarks already
declined this run.
Task 2: next run should pick a genuinely unswept corner - candidates not yet reviewed: MSBuild task
code (Microsoft.Testing.Platform.MSBuild / Microsoft.Testing.Extensions.MSBuild), VSTestBridge
adapter-shim code, or ServerMode/IPC message dispatch loop (also relevant to Task 6's Jsonite
benchmark idea - could do both in one pass).
CI "pending with 0 statuses" pattern on efficiency-improver/perf-improver/test-improver PRs is now
persistent across 4+ runs (#7, #9, #12, #16, #21 all affected) - explicitly flagged in Monthly
Activity issue this run; if a maintainer doesn't address it, keep flagging but do not keep retrying
pushes to fix it (confirmed not caused by PR content across multiple content variations already).
