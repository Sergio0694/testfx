// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;

using Microsoft.VisualStudio.TestTools.UnitTesting.Combinatorial;

namespace MSTest.Performance.Benchmarks;

/// <summary>
/// Measures <see cref="CombinatorialRandomDataAttribute.GetValues(System.Reflection.ParameterInfo)"/>, which
/// generates <c>Count</c> distinct random offsets via a <see cref="HashSet{T}"/> used to reject duplicates.
/// A fresh attribute instance is created per invocation so the internal <c>Lazy&lt;object[]&gt;</c> cache does
/// not mask the cost of the underlying generation work.
/// </summary>
[MemoryDiagnoser]
public class CombinatorialRandomDataBenchmarks
{
    [Benchmark(Baseline = true)]
    public object[] GenerateValues_SmallCount()
        => new CombinatorialRandomDataAttribute { Count = 5, Minimum = 0, Maximum = 1000, Seed = 42 }.GetValues(null!);

    [Benchmark]
    public object[] GenerateValues_LargeCount()
        => new CombinatorialRandomDataAttribute { Count = 1000, Minimum = 0, Maximum = 1_000_000, Seed = 42 }.GetValues(null!);
}
