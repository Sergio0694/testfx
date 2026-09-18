# Validated Commands (testfx)

- Build (Debug, restores repo-local .NET SDK to `.dotnet/`): `./build.sh` — took ~6 min cold.
- After first `./build.sh`, `export PATH=/home/runner/work/testfx/testfx/.dotnet:$PATH` gives a working `dotnet`.
- Build single project: `dotnet build src/<Area>/<Project>/<Project>.csproj -c Debug`
- Build unit test project for one TFM: `dotnet build test/UnitTests/<Project>.UnitTests/<Project>.UnitTests.csproj -c Debug -f net9.0`
- Run all tests in a unit test project (MTP host): `dotnet run --project test/UnitTests/<Project>.UnitTests -f net9.0 --no-build -c Debug -- --minimum-expected-tests 1`
  - `--treenode-filter` and `--filter-uid` exist but MTP treenode filter syntax needs the exact node path; simplest is running the whole assembly for small/medium projects (~8s for 1876 tests in Microsoft.Testing.Extensions.UnitTests).
- Format check (no changes needed if clean): `dotnet format whitespace <csproj> --include <file> --verify-no-changes`
- Acceptance/integration tests require `./build.sh -pack` first (not yet exercised this run).
- Benchmark project exists: `test/Performance/MSTest.Performance.Benchmarks` (BenchmarkDotNet). Has benchmarks for CollectionAssert.AreEquivalent, TestDataSourceUtilities display names, MSTestTestNodeConverter, TelemetryCollector. No benchmark yet for report mergers (JUnit/TRX/HTML merge engines).
