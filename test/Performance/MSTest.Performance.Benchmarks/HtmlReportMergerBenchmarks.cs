// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json.Nodes;

using BenchmarkDotNet.Attributes;

using Microsoft.Testing.Extensions.HtmlReport;

namespace MSTest.Performance.Benchmarks;

/// <summary>
/// Measures <see cref="HtmlReportMerger"/>, the merge engine used whenever multiple per-module/per-shard HTML
/// reports are combined (<see cref="HtmlMergeMode.Concatenate"/>) or when successive retry attempts of the same
/// module are folded together (<see cref="HtmlMergeMode.CollapseRetryAttempts"/>, used with
/// <c>--retry-failed-tests</c>). This is a JSON-parsing- and object-allocation-heavy hot path exercised once per
/// merged report, so it is a relevant CPU/energy proxy target.
/// </summary>
[MemoryDiagnoser]
public class HtmlReportMergerBenchmarks
{
    private IReadOnlyList<string> _smallReports = null!;
    private IReadOnlyList<string> _largeReports = null!;
    private IReadOnlyList<string> _retryReports = null!;

    [GlobalSetup]
    public void Setup()
    {
        _smallReports = [.. Enumerable.Range(0, 2).Select(i => BuildReport(testCount: 10, reportIndex: i))];
        _largeReports = [.. Enumerable.Range(0, 5).Select(i => BuildReport(testCount: 200, reportIndex: i))];
        _retryReports = [.. Enumerable.Range(0, 3).Select(i => BuildReport(testCount: 200, reportIndex: 0, attempt: i))];
    }

    [Benchmark(Baseline = true)]
    public string Merge_Concatenate_SmallReport() => HtmlReportMerger.Merge(_smallReports, HtmlMergeMode.Concatenate);

    [Benchmark]
    public string Merge_Concatenate_LargeReport() => HtmlReportMerger.Merge(_largeReports, HtmlMergeMode.Concatenate);

    [Benchmark]
    public string Merge_CollapseRetryAttempts_LargeReport() => HtmlReportMerger.Merge(_retryReports, HtmlMergeMode.CollapseRetryAttempts);

    // Builds a synthetic HTML report whose embedded JSON mixes passed/failed/skipped tests with error messages
    // and stack traces, matching the shape MSTest/MTP actually emits per test. "attempt" offsets the start/end
    // timestamps and per-test outcomes so successive retry reports look like true re-execution attempts.
    private static string BuildReport(int testCount, int reportIndex, int attempt = 0)
    {
        var tests = new JsonArray();
        for (int i = 0; i < testCount; i++)
        {
            string outcome = attempt == 0
                ? i % 10 == 0 ? "failed" : i % 25 == 0 ? "skipped" : "passed"
                : i % 10 == 0 && attempt < 2 ? "failed" : "passed";

            var test = new JsonObject
            {
                ["uid"] = FormattableString.Invariant($"Namespace.Class.Test{i}"),
                ["displayName"] = FormattableString.Invariant($"Test{i}"),
                ["outcome"] = outcome,
                ["durationMs"] = 12.5 + i,
            };

            if (outcome is "failed")
            {
                test["errorMessage"] = "Assert.AreEqual failed. Expected:<1>. Actual:<2>.";
                test["exceptionType"] = "Microsoft.VisualStudio.TestTools.UnitTesting.AssertFailedException";
                test["stackTrace"] = "   at Namespace.Class.Test(String[] args) in /src/Class.cs:line 42";
            }
            else if (outcome is "skipped")
            {
                test["errorMessage"] = "Test skipped by [Ignore] attribute.";
            }

            tests.Add((JsonNode)test);
        }

        var report = new JsonObject
        {
            ["schemaVersion"] = "1",
            ["generator"] = "Microsoft.Testing.Extensions.HtmlReport",
            ["generatorVersion"] = "1.0.0",
            ["testApplication"] = FormattableString.Invariant($"App{reportIndex}"),
            ["machineName"] = "benchmark-machine",
            ["userName"] = "benchmark-user",
            ["framework"] = "MSTestFramework",
            ["frameworkUid"] = "mstest",
            ["frameworkVersion"] = "4.0.0",
            ["startTime"] = new DateTimeOffset(2026, 1, 1, 8, attempt, 0, TimeSpan.Zero).ToString("O", CultureInfo.InvariantCulture),
            ["endTime"] = new DateTimeOffset(2026, 1, 1, 8, attempt, 30, TimeSpan.Zero).ToString("O", CultureInfo.InvariantCulture),
            ["tests"] = tests,
            ["summary"] = new JsonObject(),
        };

        return HtmlReportEngine.RenderReport(report.ToJsonString());
    }
}
