# Validated Build/Test/Coverage Commands (testfx)

- Build (Debug): `./build.sh` — succeeds, ~5m40s, 0 warnings/errors on this run.
- Build single project: `dotnet build test/UnitTests/<Project>/<Project>.csproj -f net9.0 -c Debug` (needs `./build.sh` to have run once first to restore all deps/tools).
- Run all unit tests in a project: `dotnet run --project test/UnitTests/<Project> -f net9.0 --no-build -c Debug --`
  - `--treenode-filter` patterns like `/*/*/*/ClassName/*` did NOT match for TestFramework.UnitTests (MTP-based project) — matching tests without a filter and confirming pass/fail counts before/after was more reliable than trying to get treenode-filter syntax right. Revisit filter-syntax skill if precise single-test filtering is needed.
- `dotnet format whitespace <csproj> --include <file> --verify-no-changes` works to check formatting of a specific new file.
- No `AGENTS.md`/BannedSymbols.txt exists for `test/UnitTests/TestFramework.UnitTests` — AwesomeAssertions (`Should()`) is the established style there (used throughout Attributes/ tests), consistent with repo convention (banned only in Adapter unit test projects).
- `TestFramework.csproj` has `InternalsVisibleTo` for `Microsoft.VisualStudio.TestPlatform.TestFramework.UnitTests` (assembly name of TestFramework.UnitTests project) — internal classes like `TestDataSourceUtilities` are directly testable.
- Solution files: `TestFx.slnx` (full), `MSTest.slnf`, `Microsoft.Testing.Platform.slnf`, `NonWindowsTests.slnf`.
- **IMPORTANT (Linux sandbox limitation)**: Test projects that multi-target Windows-only frameworks
  (`net462`/`net48`) alongside `net8.0`/`net9.0` (e.g. `MSTestAdapter.PlatformServices.UnitTests`) are
  excluded from `NonWindowsTests.slnf` entirely and CANNOT be restored/built as-is on Linux, because the
  product projects they reference (`MSTestAdapter.PlatformServices`, `MSTest.TestAdapter`) don't support
  `net462`/`net48` there (`error NU1201`). Workaround used successfully: after `./build.sh` has run once
  (to restore shared multi-targeted deps like `TestFramework.SourceGeneration`), temporarily edit that one
  test project's `.csproj` `<TargetFrameworks>` to `net9.0` only, run `dotnet build <csproj>.csproj -c
  Debug` (no `-f` needed) and `dotnet run --project <dir> -f net9.0 --no-build -c Debug --` to verify,
  then **revert the `.csproj` edit before committing** (`git diff` must show 0 changes to the `.csproj`).
  Never commit that temporary `TargetFrameworks` edit.
- Pre-existing unrelated failures when running `MSTestAdapter.PlatformServices.UnitTests` on Linux (net9.0):
  24 failures in `Deployment/TestRunDirectoriesTests.cs` and related deployment tests — all due to
  hard-coded Windows-style paths (`C:\temp\...`) compared against `Path.Combine` producing `/`-separated
  paths on Linux. Not caused by any test-improver change; safe to ignore when diffing pass/fail counts on
  this platform (baseline: 1088 passed / 24 failed / 1112 total as of 2026-09-19; 1098 passed / 24 failed
  / 1122 total as of 2026-09-23, after 3 more test-improver PRs added tests).
- For assembly-level attribute testing (e.g. `ReflectHelper`'s `GetParallelizeAttribute`,
  `HasDiscoverInternalsAttribute`, etc.), `AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(...),
  AssemblyBuilderAccess.Run)` + `CustomAttributeBuilder` + `assemblyBuilder.SetCustomAttribute(...)` lets
  a test construct a minimal in-memory assembly with exactly one attribute, avoiding shared test-assembly
  attribute pollution. Same pattern already used in `TypeCacheTestFilterProviderTests.cs`. Works
  identically on `#if NETFRAMEWORK` (via `AppDomain.CurrentDomain.DefineDynamicAssembly`) and modern TFMs
  (via the static `AssemblyBuilder.DefineDynamicAssembly`).
