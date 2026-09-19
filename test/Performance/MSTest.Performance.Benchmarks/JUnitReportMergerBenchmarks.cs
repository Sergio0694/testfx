// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.Linq;

using BenchmarkDotNet.Attributes;

using Microsoft.Testing.Extensions.JUnitReport;

namespace MSTest.Performance.Benchmarks;

/// <summary>
/// Measures <c>JUnitReportMerger.Merge</c>, the XML-level merge used to combine per-module JUnit reports
/// (<see cref="JUnitMergeMode.Concatenate"/>) and to collapse retry attempts across reports produced by
/// <c>--retry-failed-tests</c> (<see cref="JUnitMergeMode.CollapseRetryAttempts"/>). Both modes clone every
/// merged <c>&lt;testcase&gt;</c> element into a new <see cref="XElement"/> tree, so this benchmark tracks
/// how merge cost scales with the number of test cases contributed across input reports.
/// </summary>
[MemoryDiagnoser]
public class JUnitReportMergerBenchmarks
{
    private IReadOnlyList<XDocument> _smallReports = null!;
    private IReadOnlyList<XDocument> _largeReports = null!;
    private IReadOnlyList<XDocument> _largeRetryReports = null!;

    [GlobalSetup]
    public void Setup()
    {
        _smallReports = [BuildReport(suiteCount: 2, testsPerSuite: 10)];
        _largeReports = [BuildReport(suiteCount: 5, testsPerSuite: 200)];

        // Three retry attempts of the same suites/tests, as produced by --retry-failed-tests: the merger
        // must recognize repeated (suite, test) identities across reports and collapse them to the latest
        // occurrence, which is the code path this benchmark isolates.
        _largeRetryReports =
        [
            BuildReport(suiteCount: 5, testsPerSuite: 200),
            BuildReport(suiteCount: 5, testsPerSuite: 200),
            BuildReport(suiteCount: 5, testsPerSuite: 200),
        ];
    }

    [Benchmark(Baseline = true)]
    public XDocument Merge_Concatenate_SmallReport()
        => JUnitReportMerger.Merge(_smallReports, "run", JUnitMergeMode.Concatenate);

    [Benchmark]
    public XDocument Merge_Concatenate_LargeReport()
        => JUnitReportMerger.Merge(_largeReports, "run", JUnitMergeMode.Concatenate);

    [Benchmark]
    public XDocument Merge_CollapseRetryAttempts_LargeReport()
        => JUnitReportMerger.Merge(_largeRetryReports, "run", JUnitMergeMode.CollapseRetryAttempts);

    private static XDocument BuildReport(int suiteCount, int testsPerSuite)
    {
        var root = new XElement("testsuites");
        for (int s = 0; s < suiteCount; s++)
        {
            var suite = new XElement(
                "testsuite",
                new XAttribute("name", $"Suite{s}"),
                new XAttribute("tests", testsPerSuite),
                new XAttribute("failures", 0),
                new XAttribute("errors", 0),
                new XAttribute("skipped", 0),
                new XAttribute("time", "1.234"),
                new XAttribute("timestamp", "2024-01-01T00:00:00.000"));

            for (int t = 0; t < testsPerSuite; t++)
            {
                // A realistic mix of outcomes and children, mirroring what MSTest/MTP actually emits per
                // test case (system-out/system-err are present on most tests regardless of outcome).
                var testCase = new XElement(
                    "testcase",
                    new XAttribute("classname", $"Suite{s}.TestClass"),
                    new XAttribute("name", $"Test{t}"),
                    new XAttribute("time", "0.010"),
                    new XElement("system-out", "output"),
                    new XElement("system-err", string.Empty));

                if (t % 10 == 0)
                {
                    testCase.Add(new XElement("failure", new XAttribute("message", "assertion failed")));
                }
                else if (t % 17 == 0)
                {
                    testCase.Add(new XElement("skipped"));
                }

                suite.Add(testCase);
            }

            root.Add(suite);
        }

        return new XDocument(new XDeclaration("1.0", "UTF-8", null), root);
    }
}
