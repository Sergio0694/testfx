# Testing Backlog (testfx) — cursor & opportunities

## Completed
- 2026-09-18: Added unit tests for `TestDataSourceUtilities.ComputeDefaultDisplayName`
  (src/TestFramework/TestFramework/Internal/TestDataSourceUtilities.cs) — previously had ZERO direct
  unit test coverage despite being a shipped internal API used by DataRowAttribute, DynamicDataAttribute,
  CombinatorialDataAttribute, TestMethodRunner.DataSource, and AssemblyEnumerator. Covers: null data,
  primitive formatting, string/char quoting, null argument literal, single-object-array-parameter
  special case, recursive nested array humanization, non-object array humanization, empty data array,
  ReflectionTestMethodInfo.DisplayName usage, and repeated-call StringBuilder-cache safety.
  PR branch: test-assist/test-data-source-utilities-display-name.

## Backlog / opportunities not yet actioned
- `src/TestFramework/TestFramework/Internal/StringEx.cs` — check for direct unit test coverage (only
  found a same-named but unrelated file in test infra helpers; verify next run).
- `src/Adapter/MSTestAdapter.PlatformServices/Execution/TestMethodRunner.DataSource.cs` display-name
  fallback logic (`?? TestDataSourceUtilities.ComputeDefaultDisplayName(...)`) — check integration/unit
  coverage of the fallback chain itself (custom GetDisplayName vs. default), not just the utility method.
- Review `src/Analyzers/MSTest.Analyzers` C# rule test coverage gaps (VB.NET tests are explicitly OUT OF
  SCOPE per repo-specific constraint — do not propose VB tests for analyzers).
- No coverage tooling was run this session (time-boxed); consider running the existing coverage pipeline
  (check azure-pipelines.yml / stryker-config.json for mutation testing config) in a future run to find
  quantified gaps rather than relying on git-history heuristics.

## Cursor
- Last task 2 (opportunity discovery) scan: searched for files changed in 60 days (repo only has 1 commit
  of history available in this shallow clone — history-based "bug-prone area" heuristic is NOT usable here;
  rely on architecture/complexity reading instead).
- Next run: consider Task 6 (test infrastructure) or continue Task 3 with the StringEx candidate above.
