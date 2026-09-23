# Efficiency Backlog

## Completed
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
