// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;

using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Services;
using Microsoft.Testing.Platform.TestHost;

namespace MSTest.Performance.Benchmarks;

/// <summary>
/// Measures <see cref="TestCoverageResult.Scopes"/>, which groups the accumulated coverage measurements by
/// (session, scope). This is invoked whenever the terminal output device or exit-code policy needs the
/// aggregated view, so it can run repeatedly as measurements stream in during test execution.
/// </summary>
[MemoryDiagnoser]
public class TestCoverageResultBenchmarks
{
    private const int SmallMeasurementCount = 5;
    private const int LargeMeasurementCount = 1000;

    private TestCoverageResult _small = null!;
    private TestCoverageResult _large = null!;

    [GlobalSetup]
    public void Setup()
    {
        _small = CreatePopulated(SmallMeasurementCount);
        _large = CreatePopulated(LargeMeasurementCount);
    }

    [Benchmark]
    public int Scopes_SmallCollection() => _small.Scopes.Count;

    [Benchmark]
    public int Scopes_LargeCollection() => _large.Scopes.Count;

    private static TestCoverageResult CreatePopulated(int measurementCount)
    {
        var result = new TestCoverageResult();
        var sessionUid = new SessionUid("benchmark-session");

        for (int i = 0; i < measurementCount; i++)
        {
            var message = new TestCoverageMessage(
                sessionUid,
                new CoverageScope(CoverageScopeLevel.File, $"File{i}.cs"),
                CoverageMetric.Line,
                coveredCount: i,
                coverableCount: i + 1,
                producerId: "benchmark-producer");

            // Every measurement uses a distinct scope, so groups grow to measurementCount entries -
            // the case the pre-sizing fix targets.
            result.ConsumeAsync(dataProducer: null!, message, CancellationToken.None).GetAwaiter().GetResult();
        }

        return result;
    }
}
