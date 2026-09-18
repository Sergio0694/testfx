# Efficiency Backlog

## Completed
- HIGH (Code-Level): `JUnitReportMerger.MergeRetryAttempts` did 3x `Elements().Any(...)` scans per
  test case to detect failure/error/skipped. Replaced with single foreach.
  PR: efficiency/junit-merge-single-pass-status. Measured via standalone microbenchmark
  (XElement testcases, various sizes): ~3.2x time speedup, 66.7% allocation reduction at
  1000-10000 testcases. Small collections (100) still ~1.4x / 66.7% alloc reduction.
  Exercised whenever `--report-junit` + retry-merge (CollapseRetryAttempts mode) runs, e.g.
  CI with `--retry-failed-tests` and JUnit report enabled.

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
- MEDIUM: Add a BenchmarkDotNet benchmark for JUnitReportMerger / TrxReportEngine merge / HtmlReportMerger
  in test/Performance/MSTest.Performance.Benchmarks — currently no coverage for report-merge hot paths
  (only assertion/telemetry/node-conversion benchmarks exist). Task 6 candidate.
- MEDIUM: DependsOnShouldBeValidAnalyzer caching (see above) — needs careful review given analyzer
  correctness sensitivity.
- LOW: Review Terminal rendering code (AnsiTerminalTestProgressFrame, TerminalTestReporter) for
  allocation patterns during live progress rendering — not yet scanned in depth.

## Backlog cursor
Next run: continue Task 2 scan in `src/Adapter` execution/discovery hot paths (TestMethodRunner,
TypeCache, AssemblyEnumerator) and `src/Platform` ServerMode JsonRpc serializers (6188 LOC, not yet
reviewed for allocation patterns). Also consider Task 6 (benchmark infra for report mergers).
