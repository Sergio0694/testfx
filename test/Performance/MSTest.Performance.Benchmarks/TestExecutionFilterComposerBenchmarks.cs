// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;

using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Requests;

namespace MSTest.Performance.Benchmarks;

/// <summary>
/// Measures <c>TestExecutionFilterComposer.ComposeAsync</c>'s uid-intersection path, which runs whenever
/// multiple <see cref="ITestExecutionFilterProvider"/> extensions each contribute a test-node-uid filter
/// for the same request (e.g. combining a "retry failed tests" provider with an explicit uid filter).
/// </summary>
[MemoryDiagnoser]
public class TestExecutionFilterComposerBenchmarks
{
    private static readonly TestExecutionFilterContext Context = new(TestExecutionRequestKind.Run, TestExecutionRequestOrigin.Console);

    private ITestExecutionFilter _smallRequestFilter = null!;
    private IReadOnlyList<ITestExecutionFilterProvider> _smallProviders = null!;

    private ITestExecutionFilter _largeRequestFilter = null!;
    private IReadOnlyList<ITestExecutionFilterProvider> _largeProviders = null!;

    [GlobalSetup]
    public void Setup()
    {
        (_smallRequestFilter, _smallProviders) = BuildScenario(uidCount: 5, providerCount: 2);
        (_largeRequestFilter, _largeProviders) = BuildScenario(uidCount: 1000, providerCount: 2);
    }

    [Benchmark(Baseline = true)]
    public Task<ITestExecutionFilter> ComposeAsync_SmallUidSet()
        => TestExecutionFilterComposer.ComposeAsync(_smallRequestFilter, _smallProviders, Context, allowProviderContributions: true, CancellationToken.None);

    [Benchmark]
    public Task<ITestExecutionFilter> ComposeAsync_LargeUidSet()
        => TestExecutionFilterComposer.ComposeAsync(_largeRequestFilter, _largeProviders, Context, allowProviderContributions: true, CancellationToken.None);

    private static (ITestExecutionFilter RequestFilter, IReadOnlyList<ITestExecutionFilterProvider> Providers) BuildScenario(int uidCount, int providerCount)
    {
        var requestUids = new TestNodeUid[uidCount];
        for (int i = 0; i < uidCount; i++)
        {
            requestUids[i] = new TestNodeUid($"uid-{i}");
        }

        var requestFilter = new TestNodeUidListFilter(requestUids);

        var providers = new ITestExecutionFilterProvider[providerCount];
        for (int p = 0; p < providerCount; p++)
        {
            // Each provider contributes a filter that overlaps with the request uids so the intersection
            // logic has real work to do, rather than degenerating to an early empty-set break.
            var providerUids = new TestNodeUid[uidCount];
            for (int i = 0; i < uidCount; i++)
            {
                providerUids[i] = new TestNodeUid($"uid-{i}");
            }

            providers[p] = new StaticFilterProvider($"provider-{p}", new TestNodeUidListFilter(providerUids));
        }

        return (requestFilter, providers);
    }

    private sealed class StaticFilterProvider(string uid, ITestExecutionFilter filter) : ITestExecutionFilterProvider
    {
        public string Uid { get; } = uid;

        public string Version => "1.0.0";

        public string DisplayName => Uid;

        public string Description => Uid;

        public Task<bool> IsEnabledAsync() => Task.FromResult(true);

        public Task<ITestExecutionFilter?> GetFilterAsync(TestExecutionFilterContext context, CancellationToken cancellationToken)
            => Task.FromResult<ITestExecutionFilter?>(filter);
    }
}
