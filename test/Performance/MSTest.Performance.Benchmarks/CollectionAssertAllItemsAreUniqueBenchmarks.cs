// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MSTest.Performance.Benchmarks;

/// <summary>
/// Measures <see cref="CollectionAssert.AllItemsAreUnique(System.Collections.ICollection?, string?)"/> on its
/// success path, which is the hot path exercised by every passing <c>CollectionAssert.AllItemsAreUnique</c>
/// call in a test suite. Internally this builds a <see cref="HashSet{T}"/> per invocation to track seen
/// elements, so this benchmark tracks the cost of that allocation pattern as collection size grows.
/// </summary>
[MemoryDiagnoser]
public class CollectionAssertAllItemsAreUniqueBenchmarks
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
    public void AllItemsAreUnique_SmallCollection()
        => CollectionAssert.AllItemsAreUnique(_small, string.Empty);

    [Benchmark]
    public void AllItemsAreUnique_LargeCollection()
        => CollectionAssert.AllItemsAreUnique(_large, string.Empty);
}
