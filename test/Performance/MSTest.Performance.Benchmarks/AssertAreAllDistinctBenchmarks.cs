// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MSTest.Performance.Benchmarks;

/// <summary>
/// Measures <see cref="Assert.AreAllDistinct{T}(System.Collections.Generic.IEnumerable{T}?, string?, string)"/>
/// on its success path, which builds an internal <see cref="HashSet{T}"/> sized from the snapshot's element
/// count. This benchmark tracks the cost of that allocation pattern as collection size grows.
/// </summary>
[MemoryDiagnoser]
public class AssertAreAllDistinctBenchmarks
{
    private int[] _small = null!;
    private int[] _large = null!;

    [GlobalSetup]
    public void Setup()
    {
        _small = [1, 2, 3, 4, 5];
        _large = Enumerable.Range(0, 1000).ToArray();
    }

    [Benchmark(Baseline = true)]
    public void AreAllDistinct_SmallCollection()
        => Assert.AreAllDistinct(_small, string.Empty);

    [Benchmark]
    public void AreAllDistinct_LargeCollection()
        => Assert.AreAllDistinct(_large, string.Empty);
}
