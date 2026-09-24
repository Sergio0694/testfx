# Validated Commands (testfx)

- Build (Debug, restores repo-local .NET SDK to `.dotnet/`): `./build.sh` — took ~6 min cold.
- After first `./build.sh`, `export PATH=/home/runner/work/testfx/testfx/.dotnet:$PATH` gives a working `dotnet`.
- Build single project: `dotnet build src/<Area>/<Project>/<Project>.csproj -c Debug`
- Build unit test project for one TFM: `dotnet build test/UnitTests/<Project>.UnitTests/<Project>.UnitTests.csproj -c Debug -f net9.0`
- Run all tests in a unit test project (MTP host): `dotnet run --project test/UnitTests/<Project>.UnitTests -f net9.0 --no-build -c Debug -- --minimum-expected-tests 1`
  - `--treenode-filter` and `--filter-uid` exist but MTP treenode filter syntax needs the exact node path; simplest is running the whole assembly for small/medium projects (~8s for 1876 tests in Microsoft.Testing.Extensions.UnitTests).
- Format check (no changes needed if clean): `dotnet format whitespace <csproj> --include <file> --verify-no-changes`
- Acceptance/integration tests require `./build.sh -pack` first (not yet exercised this run).
- Benchmark project exists: `test/Performance/MSTest.Performance.Benchmarks` (BenchmarkDotNet). Has benchmarks for CollectionAssert.AreEquivalent, TestDataSourceUtilities display names, MSTestTestNodeConverter, TelemetryCollector, JUnitReportMerger, HtmlReportMerger. TrxReportEngine merge still lacks a benchmark (Task 6 candidate for a future run).
- Adding a benchmark for an `internal` merge type (JUnitReportMerger/HtmlReportMerger) requires: (1) `<InternalsVisibleTo Include="MSTest.Performance.Benchmarks" Key="$(VsPublicKey)" />` in the target project's csproj, alongside the existing `Microsoft.Testing.Extensions.UnitTests`/`DynamicProxyGenAssembly2` entries, and (2) a `<ProjectReference>` to that project added to `MSTest.Performance.Benchmarks.csproj`. Run `dotnet run --project test/Performance/MSTest.Performance.Benchmarks -c Release --no-restore -- --filter '*Name*' --job short` for a quick sanity check (full run without `--job short` takes much longer). Always `rm -rf BenchmarkDotNet.Artifacts/` before committing — it's generated output, not tracked.
- Benchmark for CommandLineOptionsValidator (internal static class in Microsoft.Testing.Platform):
  needed InternalsVisibleTo grant for MSTest.Performance.Benchmarks in
  src/Platform/Microsoft.Testing.Platform/Microsoft.Testing.Platform.csproj, plus an explicit
  ProjectReference to Microsoft.Testing.Platform.csproj added to MSTest.Performance.Benchmarks.csproj
  (previously only transitively referenced via the adapter projects - explicit reference needed for
  IVT to resolve reliably). CommandLineOption's public ctor (name, description, ArgumentArity, isHidden)
  and CommandLineParseResult's public ctor (toolName, options, errors) are usable without IVT; only
  the CommandLineOptionsValidator class itself needs the grant.
