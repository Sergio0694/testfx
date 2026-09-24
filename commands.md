# Validated Build/Test/Perf Commands (testfx)

- Build (Debug): `./build.sh`
- Build (Release): `./build.sh -c Release` — validated 2026-09-18, succeeds (~a few min).
- Pack: `./build.sh -pack` (needed before acceptance/integration tests).
- Unit tests: `./build.sh -test` (or run a specific test DLL directly, see below).
- Integration + acceptance tests: `./build.sh -pack -test -integrationTest`.
- Run a single unit test project after building (faster iteration), e.g.:
  `dotnet build test/UnitTests/TestFramework.UnitTests/TestFramework.UnitTests.csproj -c Release -f net9.0`
  then `dotnet exec artifacts/bin/TestFramework.UnitTests/Release/net9.0/Microsoft.VisualStudio.TestPlatform.TestFramework.UnitTests.dll --treenode-filter "/*/*/<TestClass>/*"`
  - NOTE: net10.0 TFM build fails standalone ("doesn't have a target for 'net10.0'") unless a full `./build.sh` restore/build has run first for that TFM; net9.0 works fine as a quick single-project build.
- Format check: `dotnet format <project>.csproj --no-restore --verify-no-changes` (use `.dotnet` on PATH: `export PATH="$PWD/.dotnet:$PATH"`).
- global.json pins .NET SDK 11.0.100-rc.2 (prerelease, rollForward latestFeature); runtimes 8.0/9.0/10.0 installed at `.dotnet/`.

## Test filter syntax
- `Microsoft.Testing.Platform.UnitTests.dll` does NOT support `--treenode-filter` (prints unrelated help text silently instead of an error) — use VSTest-style `--filter "FullyQualifiedName~ClassName"` instead (confirmed working 2026-09-24). Other unit test projects may differ; check `--help` output for which filter flag is supported before assuming either works.

## Benchmarks
- Existing benchmark project: `test/Performance/MSTest.Performance.Benchmarks` (BenchmarkDotNet 0.15.8, TFM net10.0 only — BenchmarkDotNet doesn't yet recognize the repo's preview net11.0 runtime).
- Build it directly (don't rely on top-level solution build): `dotnet build test/Performance/MSTest.Performance.Benchmarks/MSTest.Performance.Benchmarks.csproj -c Release`.
- **IMPORTANT**: running the built exe with the *default* out-of-process toolchain times out in this sandbox (BenchmarkDotNet's own internal `dotnet restore`/`build` step for the generated harness project exceeds ~2 min and gets killed). Always pass `-i` (in-process, `InProcessEmitToolchain`) to avoid this, e.g.:
  `dotnet artifacts/bin/MSTest.Performance.Benchmarks/Release/net10.0/MSTest.Performance.Benchmarks.dll --filter "*ClassName*" -j short -i`
- Existing benchmark classes (as of 2026-09): `CollectionAssertEquivalenceBenchmarks`, `MSTestTestNodeConverterBenchmarks`, `TelemetryCollectorBenchmarks`, `TestDataSourceUtilitiesBenchmarks`. All already have good baseline coverage of common hot paths; several (TelemetryCollector, TestDataSourceUtilities, MSTestTestNodeConverter) were already well-optimized (ConditionalWeakTable caching, ThreadStatic StringBuilder reuse, etc.) when reviewed 2026-09-18.
