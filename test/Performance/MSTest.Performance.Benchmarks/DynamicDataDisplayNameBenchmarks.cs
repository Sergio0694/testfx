// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MSTest.Performance.Benchmarks;

/// <summary>
/// Measures <see cref="DynamicDataAttribute.GetDisplayName"/> on its reflection fallback path (no source
/// generator registration for the custom display-name method), which is invoked once per data row for
/// <c>[DynamicData(..., DynamicDataDisplayName = "...")]</c> tests. Tracks the cost of resolving and
/// validating the display-name <see cref="MethodInfo"/> for repeated calls against the same method.
/// </summary>
[MemoryDiagnoser]
public class DynamicDataDisplayNameBenchmarks
{
    private DynamicDataAttribute _attribute = null!;
    private MethodInfo _testMethodInfo = null!;
    private object?[] _data = null!;

    [GlobalSetup]
    public void Setup()
    {
        _attribute = new DynamicDataAttribute(nameof(SampleData))
        {
            DynamicDataDisplayName = nameof(GetCustomDisplayName),
            DynamicDataDisplayNameDeclaringType = typeof(DynamicDataDisplayNameBenchmarks),
        };
        _testMethodInfo = typeof(DynamicDataDisplayNameBenchmarks).GetMethod(
            nameof(SampleTestMethod), BindingFlags.NonPublic | BindingFlags.Static)!;
        _data = [1, "two"];
    }

    // Simulates many data rows re-invoking GetDisplayName for the same declaring type/method name pair,
    // which is the realistic shape for a data-driven test with several rows.
    [Benchmark(Baseline = true)]
    public string? GetDisplayName_RepeatedRows()
    {
        string? result = null;
        for (int i = 0; i < 100; i++)
        {
            result = _attribute.GetDisplayName(_testMethodInfo, _data);
        }

        return result;
    }

    public static IEnumerable<object[]> SampleData()
    {
        yield return [1, "two"];
    }

    public static string GetCustomDisplayName(MethodInfo methodInfo, object[] data)
        => $"{methodInfo.Name} ({string.Join(", ", data)})";

    private static void SampleTestMethod(int a, string b)
    {
    }
}
