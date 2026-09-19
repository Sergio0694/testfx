# Perf Improver — Repo Notes (testfx)

## Perf-relevant techniques observed in this codebase
- Assertion hot paths use `TelemetryCollector.TrackAssertionCall` (opt-out via env vars, `[MethodImpl(AggressiveInlining)]`, `Lazy<bool>` for the opt-out check) — good pattern to mirror for new hot-path instrumentation.
- `MSTestTestNodeConverter` caches `BaseTestNodeData` per `UnitTestElement` via `ConditionalWeakTable` to avoid repeated recomputation across discovery/execution phases.
- `TestDataSourceUtilities` uses `[ThreadStatic]` `StringBuilder` reuse (capped at `MaxCachedBuilderCapacity = 360`) plus a `ConditionalWeakTable<MethodInfo, MethodData>` cache for per-data-row display name computation — a solid pattern for hot reflection-adjacent paths.
- `Assert.AreEquivalent` (the newer structural-equivalence comparer, distinct from `CollectionAssert.AreEquivalent`) already caches reflection metadata (`MemberCache`, `IEquatableEqualsCache`, `EnumerableElementTypeCache`, `DictionaryValueTypeCache`, `IsPrimitiveLikeCache`) via `ConcurrentDictionary`-based caches keyed by `Type`. Well optimized already.

## Completed work
### 2026-09-18 (run 35350743060)
- Found & fixed: `CollectionAssert.Helpers.GetElementCounts<T>` (used by `CollectionAssert.AreEquivalent`, `AreNotEquivalent`, `IsSubsetOf`) built its internal `Dictionary<T,int>` with default (0) capacity regardless of known input size, causing avoidable resizes/rehashes. Fixed by sizing the dictionary from `ICollection<T>.Count`/`ICollection.Count` when available.
- Measured via `CollectionAssertEquivalenceBenchmarks` (in-process, ShortRun, net10.0 Release):
  - Small (5 elements): 916ns/856B -> 309ns/632B
  - Large (1000 elements): 69.0µs/146,424B -> 32.7µs/44,472B (~2x faster, ~70% less allocation)
- Verified: `TestFramework.UnitTests` full suite (1560 tests) passes; all 54 `CollectionAssertTests` pass; `dotnet format --verify-no-changes` shows no new warnings.
- PR created via safe-outputs from branch `perf-assist/collectionassert-equivalence-dict-capacity` (title: "[perf-improver] Pre-size dictionaries in CollectionAssert element-count comparisons"). Awaiting PR number from safe-outputs processing — check next run's `session_store_sql`/`github` list_pull_requests for actual number since local `gh` is unauthenticated.

## Optimization backlog (not yet attempted, prioritized)
1. **[Feasible, low-risk]** `CollectionAssert.Helpers.CompareIEnumerable` (used by `CollectionAssert.AreEqual`) uses a `Stack<Tuple<IEnumerator,IEnumerator,int>>` per nested-enumerable comparison — `Tuple` (reference type) allocates per push; could switch to a value-tuple-based stack or a lightweight struct to cut allocations for nested-collection equality checks. Needs its own benchmark (none exists yet) before implementing — good Task 6 candidate (add a `CollectionAssertEqualityBenchmarks` class first).
2. **[Needs investigation]** `IsSubsetOfHelper`/`FindMismatchedElement` still count via boxing dictionaries when `T` is a value type used through `ICollection` (`object`-typed path in `IsSubsetOfHelper` specifically, since it operates on non-generic `ICollection`). Boxing is inherent to the non-generic `ICollection`-based overloads of `CollectionAssert.IsSubsetOf`/`AreEquivalent` and can't be removed without an API/behavior change — not attempted, flagged as a structural limitation rather than a fixable perf bug.
3. **[Build/CI perf]** Not yet investigated: build parallelism, incremental build, or CI duration for this repo. Next run should check `.github/workflows/*.yml` for build/test timing and whether `-m`/graph build flags are already used.
4. Have not yet explored MTP discovery/execution hot paths beyond `MSTestTestNodeConverter` (already fast). Consider profiling actual end-to-end discovery of a large test project next.

### 2026-09-19 (run 35445953270) — attempted, reverted (no PR)
- Attempted backlog item #1: replaced `Stack<Tuple<IEnumerator,IEnumerator,int>>` with `Stack<(IEnumerator,IEnumerator,int)>` (value tuple) in `CollectionAssert.Helpers.CompareIEnumerable` (used by `CollectionAssert.AreEqual`/`AreNotEqual`).
- Added new `CollectionAssertEqualityBenchmarks` (shallow + deep nested-collection cases) to `test/Performance/MSTest.Performance.Benchmarks`.
- **Isolated micro-benchmark confirmed the Tuple→ValueTuple swap itself is a real win**: 1M push/pop cycles on a bare `Stack<Tuple<int,int,int>>` allocate ~32 bytes/op vs ~0 bytes/op for `Stack<(int,int,int)>` (verified with `GC.GetAllocatedBytesForCurrentThread`, .NET 9, Release).
- **However, at the `CollectionAssert.AreEqual` call level (5000 nested arrays), the fix showed no measurable end-to-end improvement**: baseline 847.7 µs/1.62 MB (short) and 859.0 µs/1.62 MB (medium) vs. fixed 1.52 ms/1.74 MB (short) and 1.357 ms/1.74 MB (medium) — i.e. *higher* time and allocation with the fix, dominated by run-to-run noise/GC variance (MultimodalDistribution warning from BenchmarkDotNet) rather than a real regression. The per-push Tuple allocation (~24 bytes each, 3 fields + header) is dwarfed by the far larger per-element `IComparer.Compare`/boxing costs already present in `CompareIEnumerable`'s hot loop, so the theoretical stack-frame allocation savings don't surface as a measurable win at realistic collection sizes.
- **Learning**: Don't trust isolated micro-benchmark wins as proxies for real callsite impact — always benchmark at the actual public-API call site with a realistic workload before creating a PR. Reverted the change (`git checkout --`) and removed the new benchmark file; no PR created for this attempt.
- Backlog item #1 is now considered **not worth pursuing** as a standalone change — deprioritized/removed from backlog.

## Backlog cursor
- Item #1 (CompareIEnumerable Tuple allocation): attempted 2026-09-19, no measurable benefit at realistic scale — closed out, do not re-attempt without a different angle.
- Next run should pick up backlog item #3 (CI/build perf discovery) — checked `.github/workflows/*.yml` and `azure-pipelines*.yml` on 2026-09-19; none reference `-m`/`maxcpucount`/`/graph` explicitly (uses default MSBuild flags via `build.sh`/`build.cmd`), but did not have time this run to fully profile CI job duration or attempt a parallelism change — needs an actual CI timing baseline before proposing anything.
- Backlog item #2 (boxing in non-generic `ICollection` overloads) remains a structural limitation, not actionable without an API change.
- No new user-facing performance issues found this run (`search_issues` for "performance"/"slow"/"benchmark" all returned 0 results — repo has no open issues at all currently, and no `performance` label in use).

## Round-robin task tracking
- 2026-09-18: Ran Task 1 (validated build.sh/build.sh -c Release, discovered dotnet exec pattern for unit tests, discovered benchmark project + `-i` in-process workaround), Task 2 (surveyed benchmark coverage + hot paths), Task 3 (implemented + measured + PR'd the CollectionAssert fix), Task 7 (this note + monthly issue).
- 2026-09-19: Ran Task 1 (re-validated `./build.sh -c Release` full build succeeds, ~6 min), Task 3 (attempted CompareIEnumerable Tuple optimization, measured, reverted — no PR, see notes above), Task 4 (checked PR #6 — no CI failures/comments, nothing to do; PRs #7/#8 belong to other agents, not perf-improver), Task 5 (confirmed no performance-labeled/matching issues exist), Task 7 (this note + monthly issue). Task 2/Task 6 not deeply revisited this run (time spent on Task 3 investigation/revert).
- Tasks not yet run this cycle: Task 6 (measurement infrastructure deep-dive — benchmark project still has good baseline coverage; only transient CollectionAssertEqualityBenchmarks was added then removed since the underlying change didn't pan out).
