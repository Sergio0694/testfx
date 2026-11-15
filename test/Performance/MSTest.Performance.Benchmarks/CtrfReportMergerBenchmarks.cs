// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json.Nodes;

using BenchmarkDotNet.Attributes;

using Microsoft.Testing.Extensions.CtrfReport;

namespace MSTest.Performance.Benchmarks;

/// <summary>
/// Measures <see cref="CtrfReportMerger"/>, which combines several CTRF JSON reports (e.g. one per sharded test
/// module, or one per retry attempt) into a single document. This runs once per test session whenever
/// <c>--report-ctrf</c> is combined with sharding or <c>--retry-failed-tests</c>, so its cost scales with the
/// number of modules/attempts and the total number of tests across them.
/// </summary>
[MemoryDiagnoser]
public class CtrfReportMergerBenchmarks
{
    private List<string> _smallReports = null!;
    private List<string> _largeReports = null!;
    private List<string> _largeRetryReports = null!;

    [GlobalSetup]
    public void Setup()
    {
        _smallReports = [.. Enumerable.Range(0, 2).Select(reportIndex => BuildReport(reportIndex, testCount: 10))];
        _largeReports = [.. Enumerable.Range(0, 5).Select(reportIndex => BuildReport(reportIndex, testCount: 200))];
        _largeRetryReports = [.. Enumerable.Range(0, 3).Select(attemptIndex => BuildReport(0, testCount: 200, sharedIdentities: true))];
    }

    [Benchmark(Baseline = true)]
    public string Merge_SmallReport() => CtrfReportMerger.Merge(_smallReports);

    [Benchmark]
    public string Merge_LargeReport() => CtrfReportMerger.Merge(_largeReports);

    [Benchmark]
    public string Merge_LargeReport_CollapseRetryAttempts() => CtrfReportMerger.Merge(_largeRetryReports, CtrfMergeMode.CollapseRetryAttempts);

    // Builds a synthetic CTRF report. When 'sharedIdentities' is true, tests use the same testId across
    // reports (as successive retry attempts of the same module would), which is what makes
    // CollapseRetryAttempts fold rows together instead of merely concatenating them.
    private static string BuildReport(int reportIndex, int testCount, bool sharedIdentities = false)
    {
        var testArray = new JsonArray();
        for (int i = 0; i < testCount; i++)
        {
            string testId = sharedIdentities ? $"test-{i}" : $"test-{reportIndex}-{i}";
            testArray.Add(new JsonObject
            {
                ["testId"] = testId,
                ["name"] = $"Namespace.ClassA.Test{i}",
                ["status"] = i % 7 == 0 ? "failed" : "passed",
                ["duration"] = 5,
                ["start"] = 1000 + i,
                ["stop"] = 1005 + i,
            });
        }

        var report = new JsonObject
        {
            ["reportFormat"] = "CTRF",
            ["specVersion"] = "0.0.0",
            ["reportId"] = Guid.NewGuid().ToString("D"),
            ["timestamp"] = DateTimeOffset.FromUnixTimeMilliseconds(2000).ToString("O", CultureInfo.InvariantCulture),
            ["generatedBy"] = "Microsoft.Testing.Extensions.CtrfReport",
            ["results"] = new JsonObject
            {
                ["tool"] = new JsonObject { ["name"] = "MSTest" },
                ["summary"] = new JsonObject
                {
                    ["tests"] = testCount,
                    ["passed"] = testCount,
                    ["failed"] = 0,
                    ["skipped"] = 0,
                    ["pending"] = 0,
                    ["other"] = 0,
                    ["flaky"] = 0,
                    ["start"] = 1000,
                    ["stop"] = 2000,
                    ["duration"] = 1000,
                },
                ["environment"] = new JsonObject
                {
                    ["osPlatform"] = "test",
                },
                ["tests"] = testArray,
            },
        };

        return report.ToJsonString();
    }
}
