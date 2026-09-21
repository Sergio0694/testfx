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

## Backlog cursor
Next run: continue Task 2 scan (Adapter TestMethodRunner/TypeCache/AssemblyEnumerator + Platform
TerminalTestReporter formatting/AnsiTerminalTestProgressFrame were reviewed and found already heavily
optimized — no action taken; not re-reviewed since). Remaining unreviewed areas: ServerMode JsonRpc
Jsonite JSON reader/writer (vendored third-party-style code, explicitly flagged in
SerializerUtilities.cs comment as "known to suffer boxing/unboxing, to be rewritten with
System.Text.Json" — do not attempt incremental fixes here, it's a planned rewrite) and
DependsOnShouldBeValidAnalyzer caching (still not attempted, still flagged high-risk for analyzer
correctness). Task 6 follow-up candidates: TrxReportEngine merge benchmark still missing (JUnitReportMerger
and HtmlReportMerger now both covered) — TrxReportEngine.Merge.* files are the next Task 6 target.
PRs #6, #7, #9, #11 (perf-improver + efficiency-improver, all draft) still open with CI in "pending/unstable"
state as of this run (likely infra, not code) — re-check next run before assuming they need fixes.

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
