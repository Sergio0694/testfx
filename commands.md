# Validated Build/Test/Coverage Commands (testfx)

- Build (Debug): `./build.sh` — succeeds, ~5m40s, 0 warnings/errors on this run.
- Build single project: `dotnet build test/UnitTests/<Project>/<Project>.csproj -f net9.0 -c Debug` (needs `./build.sh` to have run once first to restore all deps/tools).
- Run all unit tests in a project: `dotnet run --project test/UnitTests/<Project> -f net9.0 --no-build -c Debug --`
  - `--treenode-filter` patterns like `/*/*/*/ClassName/*` did NOT match for TestFramework.UnitTests (MTP-based project) — matching tests without a filter and confirming pass/fail counts before/after was more reliable than trying to get treenode-filter syntax right. Revisit filter-syntax skill if precise single-test filtering is needed.
- `dotnet format whitespace <csproj> --include <file> --verify-no-changes` works to check formatting of a specific new file.
- No `AGENTS.md`/BannedSymbols.txt exists for `test/UnitTests/TestFramework.UnitTests` — AwesomeAssertions (`Should()`) is the established style there (used throughout Attributes/ tests), consistent with repo convention (banned only in Adapter unit test projects).
- `TestFramework.csproj` has `InternalsVisibleTo` for `Microsoft.VisualStudio.TestPlatform.TestFramework.UnitTests` (assembly name of TestFramework.UnitTests project) — internal classes like `TestDataSourceUtilities` are directly testable.
- Solution files: `TestFx.slnx` (full), `MSTest.slnf`, `Microsoft.Testing.Platform.slnf`, `NonWindowsTests.slnf`.
