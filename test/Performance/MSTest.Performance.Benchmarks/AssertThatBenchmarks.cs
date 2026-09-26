// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MSTest.Performance.Benchmarks;

/// <summary>
/// Measures <see cref="Assert.That"/> on its success path for a side-effect-free condition, which takes the
/// fast-path branch that compiles the expression tree's body via <see cref="System.Linq.Expressions.Expression{TDelegate}.Compile(bool)"/>
/// and invokes the resulting delegate exactly once before discarding it. Since the delegate is never reused,
/// this benchmark tracks the cost of the underlying compilation strategy (Reflection.Emit vs. tree
/// interpretation) rather than the cost of running the condition itself.
/// </summary>
[MemoryDiagnoser]
public class AssertThatBenchmarks
{
    private int _left;
    private int _right;

    [GlobalSetup]
    public void Setup()
    {
        _left = 1;
        _right = 1;
    }

    [Benchmark(Baseline = true)]
    public void That_SimpleComparison()
        => Assert.That(() => _left == _right);
}
