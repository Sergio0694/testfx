// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

using BenchmarkDotNet.Attributes;

using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.CommandLine;

namespace MSTest.Performance.Benchmarks;

/// <summary>
/// Measures <see cref="CommandLineOptionsValidator.ValidateAsync"/>, the command-line
/// validation pass that runs once, unconditionally, on every <c>dotnet test</c>/<c>dotnet run</c>
/// invocation across the whole Microsoft.Testing.Platform ecosystem, before any test executes.
/// The scenario mirrors a realistic host with several extensions registered (report generators,
/// retry, telemetry, etc.), each contributing a handful of options - the shape most consumers hit
/// in practice - to see how validation cost scales with extension/option count.
/// </summary>
[MemoryDiagnoser]
public class CommandLineOptionsValidatorBenchmarks
{
    private ICommandLineOptionsProvider[] _systemProviders = null!;
    private ICommandLineOptionsProvider[] _fewExtensionProviders = null!;
    private ICommandLineOptionsProvider[] _manyExtensionProviders = null!;
    private CommandLineParseResult _parseResult = null!;

    [GlobalSetup]
    public void Setup()
    {
        _systemProviders = [new FakeOptionsProvider("PlatformCommandLineProvider", 20)];

        // Mirrors a "small" host: a couple of extensions registered (e.g. TRX report + retry).
        _fewExtensionProviders =
        [
            new FakeOptionsProvider("TrxReport", 3),
            new FakeOptionsProvider("Retry", 4),
        ];

        // Mirrors a "large" host with many extensions registered at once (report formats,
        // telemetry, crash/hang dump, retry, hot reload, etc. - a realistic upper bound for
        // Microsoft.Testing.Platform.Acceptance.IntegrationTests' "all extensions" scenario).
        _manyExtensionProviders =
        [
            new FakeOptionsProvider("TrxReport", 3),
            new FakeOptionsProvider("JUnitReport", 3),
            new FakeOptionsProvider("HtmlReport", 3),
            new FakeOptionsProvider("CtrfReport", 3),
            new FakeOptionsProvider("AzureDevOpsReport", 2),
            new FakeOptionsProvider("GitHubActionsReport", 2),
            new FakeOptionsProvider("Retry", 4),
            new FakeOptionsProvider("Telemetry", 2),
            new FakeOptionsProvider("CrashDump", 3),
            new FakeOptionsProvider("HangDump", 3),
            new FakeOptionsProvider("HotReload", 2),
            new FakeOptionsProvider("VideoRecorder", 2),
        ];

        // A typical invocation: a couple of options set on the command line (e.g. "--results-directory out --report-trx").
        _parseResult = new CommandLineParseResult(
            toolName: null,
            options:
            [
                new CommandLineParseOption("results-directory", ["out"]),
                new CommandLineParseOption("report-trx", []),
            ],
            errors: []);
    }

    [Benchmark(Baseline = true)]
    public Task<ValidationResult> ValidateAsync_FewExtensions()
        => CommandLineOptionsValidator.ValidateAsync(
            _parseResult,
            _systemProviders,
            _fewExtensionProviders,
            FakeCommandLineOptions.Instance);

    [Benchmark]
    public Task<ValidationResult> ValidateAsync_ManyExtensions()
        => CommandLineOptionsValidator.ValidateAsync(
            _parseResult,
            _systemProviders,
            _manyExtensionProviders,
            FakeCommandLineOptions.Instance);

    private sealed class FakeOptionsProvider : ICommandLineOptionsProvider
    {
        private readonly IReadOnlyCollection<CommandLineOption> _options;

        public FakeOptionsProvider(string namePrefix, int optionCount)
        {
            Uid = namePrefix;
            DisplayName = namePrefix;

            var options = new CommandLineOption[optionCount];
            for (int i = 0; i < optionCount; i++)
            {
                options[i] = new CommandLineOption($"{namePrefix.ToLowerInvariant()}-option-{i}", $"Description for option {i}.", ArgumentArity.ZeroOrOne, false);
            }

            _options = options;
        }

        public string Uid { get; }

        public string Version => "1.0.0";

        public string DisplayName { get; }

        public string Description => $"{DisplayName} command line provider.";

        public Task<bool> IsEnabledAsync() => Task.FromResult(true);

        public IReadOnlyCollection<CommandLineOption> GetCommandLineOptions() => _options;

        public Task<ValidationResult> ValidateCommandLineOptionsAsync(ICommandLineOptions commandLineOptions) => ValidationResult.ValidTask;

        public Task<ValidationResult> ValidateOptionArgumentsAsync(CommandLineOption commandOption, string[] arguments) => ValidationResult.ValidTask;
    }

    private sealed class FakeCommandLineOptions : ICommandLineOptions
    {
        public static readonly FakeCommandLineOptions Instance = new();

        public bool IsOptionSet(string optionName) => false;

        public bool TryGetOptionArgumentList(string optionName, [NotNullWhen(true)] out string[]? arguments)
        {
            arguments = null;
            return false;
        }
    }
}
