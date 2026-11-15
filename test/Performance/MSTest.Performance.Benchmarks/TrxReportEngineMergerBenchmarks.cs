// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.Linq;

using BenchmarkDotNet.Attributes;

using Microsoft.Testing.Extensions.TrxReport.Abstractions;

namespace MSTest.Performance.Benchmarks;

/// <summary>
/// Measures <see cref="TrxReportEngine.Merge(IReadOnlyList{XDocument}, Guid, string)"/>, the merge engine used
/// whenever multiple per-module/per-shard TRX reports are combined into one report (e.g. multi-TFM or sharded
/// CI runs). This is an XElement-cloning- and dictionary-heavy hot path exercised once per merged report, so
/// it is a relevant CPU/energy proxy target.
/// </summary>
[MemoryDiagnoser]
public class TrxReportEngineMergerBenchmarks
{
    private static readonly XNamespace Ns = XNamespace.Get("http://microsoft.com/schemas/VisualStudio/TeamTest/2010");

    private IReadOnlyList<XDocument> _smallReports = null!;
    private IReadOnlyList<XDocument> _largeReports = null!;

    [GlobalSetup]
    public void Setup()
    {
        _smallReports = [.. Enumerable.Range(0, 2).Select(i => BuildReport(testCount: 10, reportIndex: i))];
        _largeReports = [.. Enumerable.Range(0, 5).Select(i => BuildReport(testCount: 200, reportIndex: i))];
    }

    [Benchmark(Baseline = true)]
    public XDocument Merge_SmallReport() => TrxReportEngine.Merge(_smallReports, Guid.Empty, "run");

    [Benchmark]
    public XDocument Merge_LargeReport() => TrxReportEngine.Merge(_largeReports, Guid.Empty, "run");

    // Builds a synthetic TRX report with testCount tests, mirroring the shape produced by a real MSTest/MTP
    // run: a TestDefinitions/UnitTest entry, a Results/UnitTestResult entry and a TestEntries/TestEntry entry
    // per test, plus a populated ResultSummary/Counters block.
    private static XDocument BuildReport(int testCount, int reportIndex)
    {
        var results = new XElement(Ns + "Results");
        var testDefinitions = new XElement(Ns + "TestDefinitions");
        var testEntries = new XElement(Ns + "TestEntries");

        for (int i = 0; i < testCount; i++)
        {
            string testId = $"r{reportIndex}-t{i}";
            string executionId = $"r{reportIndex}-e{i}";
            string testName = $"Test{i}";

            testDefinitions.Add(new XElement(
                Ns + "UnitTest",
                new XAttribute("id", testId),
                new XAttribute("name", testName),
                new XElement(Ns + "Execution", new XAttribute("id", executionId)),
                new XElement(
                    Ns + "TestMethod",
                    new XAttribute("className", $"Namespace.Class{reportIndex}"),
                    new XAttribute("name", testName))));

            results.Add(new XElement(
                Ns + "UnitTestResult",
                new XAttribute("executionId", executionId),
                new XAttribute("testId", testId),
                new XAttribute("testName", testName),
                new XAttribute("outcome", "Passed"),
                new XAttribute("duration", "00:00:00.0100000")));

            testEntries.Add(new XElement(
                Ns + "TestEntry",
                new XAttribute("testId", testId),
                new XAttribute("executionId", executionId)));
        }

        var resultSummary = new XElement(
            Ns + "ResultSummary",
            new XAttribute("outcome", "Completed"),
            new XElement(
                Ns + "Counters",
                new XAttribute("total", testCount),
                new XAttribute("executed", testCount),
                new XAttribute("passed", testCount),
                new XAttribute("failed", 0)));

        var testRun = new XElement(
            Ns + "TestRun",
            new XAttribute("id", Guid.NewGuid()),
            new XAttribute("name", $"run{reportIndex}"),
            new XElement(
                Ns + "Times",
                new XAttribute("creation", "2020-01-01T10:00:00.0000000+00:00"),
                new XAttribute("queuing", "2020-01-01T10:00:00.0000000+00:00"),
                new XAttribute("start", "2020-01-01T10:00:00.0000000+00:00"),
                new XAttribute("finish", "2020-01-01T11:00:00.0000000+00:00")),
            results,
            testDefinitions,
            testEntries,
            new XElement(Ns + "TestLists"),
            resultSummary);

        return new XDocument(testRun);
    }
}
