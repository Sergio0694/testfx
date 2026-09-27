// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MSTest.Performance.Benchmarks;

/// <summary>
/// Measures the failure-diagnostic path of <c>Assert.That</c> (<c>EvaluateExpression</c>/
/// <c>EvaluateAllSubExpressions</c>), which single-pass-evaluates and caches every sub-expression
/// so a rich failure message can be built. This path compiles multiple throwaway single-invocation
/// lambdas via <c>Expression.Lambda(...).Compile()</c>.
/// </summary>
[MemoryDiagnoser]
public class AssertThatFailurePathBenchmarks
{
    private readonly int _left = 1;
    private readonly int _right = 2;
    private readonly int _factor = 3;

    [Benchmark]
    public string That_FailingNestedExpression()
    {
        try
        {
            Assert.That(() => _left + _factor == _right * _factor && _left != _right);
            return "unreachable";
        }
        catch (AssertFailedException ex)
        {
            return ex.Message;
        }
    }
}
