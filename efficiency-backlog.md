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
- MEDIUM: Add a BenchmarkDotNet benchmark for TrxReportEngine merge / HtmlReportMerger in
  test/Performance/MSTest.Performance.Benchmarks — JUnitReportMerger now has coverage (see Completed);
  TRX and HTML merge paths still have none. Task 6 candidate.
- MEDIUM: DependsOnShouldBeValidAnalyzer caching (see above) — needs careful review given analyzer
  correctness sensitivity.
- LOW: Terminal rendering code (AnsiTerminalTestProgressFrame, TerminalTestReporter.Formatting.cs)
  reviewed this run — both already heavily pre-optimized (pooled buffers, cached ANSI escape strings,
  reused comparer). No further action identified.

## Backlog cursor
Next run: continue Task 2 scan (Adapter TestMethodRunner/TypeCache/AssemblyEnumerator + Platform
TerminalTestReporter formatting/AnsiTerminalTestProgressFrame were reviewed this run and found already
heavily optimized — no action taken). Remaining unreviewed areas: ServerMode JsonRpc Jsonite JSON
reader/writer (vendored third-party-style code, explicitly flagged in SerializerUtilities.cs comment as
"known to suffer boxing/unboxing, to be rewritten with System.Text.Json" — do not attempt incremental
fixes here, it's a planned rewrite) and DependsOnShouldBeValidAnalyzer caching (still not attempted,
still flagged high-risk for analyzer correctness). Also consider Task 6 follow-up: TrxReportEngine /
HtmlReportMerger benchmarks (JUnitReportMerger now covered).
