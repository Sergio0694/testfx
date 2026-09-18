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

## Backlog cursor
- Last worked item: #1 above (CollectionAssertEquivalenceBenchmarks / GetElementCounts) — DONE this run.
- Next run should pick up backlog item #1 (CompareIEnumerable Tuple allocation) or #3 (CI/build perf discovery), whichever hasn't been touched recently.

## Round-robin task tracking
- 2026-09-18: Ran Task 1 (validated build.sh/build.sh -c Release, discovered dotnet exec pattern for unit tests, discovered benchmark project + `-i` in-process workaround), Task 2 (surveyed benchmark coverage + hot paths), Task 3 (implemented + measured + PR'd the CollectionAssert fix), Task 7 (this note + monthly issue).
- Tasks not yet run this cycle: Task 4 (maintain existing perf-improver PRs — none existed yet before this run), Task 5 (comment on performance-labeled issues — none found: `search_issues` for "performance"/"perf-improver" returned 0 results, and there is no `performance` label in use yet), Task 6 (measurement infrastructure — benchmark project already exists and is reasonably comprehensive; no new benchmarks added this run, flagged CollectionAssert.AreEqual as missing coverage for next time).
