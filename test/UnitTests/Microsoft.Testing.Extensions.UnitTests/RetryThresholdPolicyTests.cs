#pragma warning disable IDE0073 // The file header does not match the required text
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under dual-license. See LICENSE.PLATFORMTOOLS.txt file in the project root for full license information.
#pragma warning restore IDE0073 // The file header does not match the required text

using System.Reflection;

using Microsoft.Testing.Extensions.Policy;
using Microsoft.Testing.Extensions.UnitTests.Helpers;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions.OutputDevice;
using Microsoft.Testing.Platform.Extensions.RetryFailedTests.Serializers;
using Microsoft.Testing.Platform.Helpers;
using Microsoft.Testing.Platform.Logging;
using Microsoft.Testing.Platform.OutputDevice;
using Microsoft.Testing.Platform.Services;

using Moq;

namespace Microsoft.Testing.Extensions.UnitTests;

[TestClass]
public sealed class RetryThresholdPolicyTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task EvaluateAsync_NoThresholdOptionSet_ReturnsFalseAndDoesNotDisplayAnything()
    {
        using RetryFailedTestsPipeServer server = await CreateServerWithCountsAsync(passed: 1, failed: 0, skipped: 0);
        var commandLineOptions = new TestCommandLineOptions([]);
        Mock<IOutputDevice> outputDevice = new(MockBehavior.Strict);

        bool disabled = await RetryThresholdPolicy.EvaluateAsync(commandLineOptions, Mock.Of<IOutputDeviceDataProducer>(), outputDevice.Object, server, TestContext.CancellationToken);

        Assert.IsFalse(disabled);
        outputDevice.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task EvaluateAsync_MaxPercentage_FailurePercentageAtThreshold_ReturnsFalse()
    {
        // 1 failed out of 4 executed = 25%, exactly at the configured maximum, so the policy must not trip.
        using RetryFailedTestsPipeServer server = await CreateServerWithCountsAsync(passed: 3, failed: 1, skipped: 0);
        var commandLineOptions = new TestCommandLineOptions(new()
        {
            [RetryCommandLineOptionsProvider.RetryFailedTestsMaxPercentageOptionName] = ["25"],
        });
        Mock<IOutputDevice> outputDevice = new(MockBehavior.Strict);

        bool disabled = await RetryThresholdPolicy.EvaluateAsync(commandLineOptions, Mock.Of<IOutputDeviceDataProducer>(), outputDevice.Object, server, TestContext.CancellationToken);

        Assert.IsFalse(disabled);
        outputDevice.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task EvaluateAsync_MaxPercentage_FailurePercentageAboveThreshold_ReturnsTrueAndDisplaysExplanation()
    {
        // 2 failed out of 4 executed = 50%, above the configured 25% maximum.
        using RetryFailedTestsPipeServer server = await CreateServerWithCountsAsync(passed: 2, failed: 2, skipped: 0);
        var commandLineOptions = new TestCommandLineOptions(new()
        {
            [RetryCommandLineOptionsProvider.RetryFailedTestsMaxPercentageOptionName] = ["25"],
        });
        List<string> displayedMessages = [];
        Mock<IOutputDevice> outputDevice = CreateCapturingOutputDevice(displayedMessages);

        bool disabled = await RetryThresholdPolicy.EvaluateAsync(commandLineOptions, Mock.Of<IOutputDeviceDataProducer>(), outputDevice.Object, server, TestContext.CancellationToken);

        Assert.IsTrue(disabled);
        Assert.HasCount(1, displayedMessages);
        Assert.Contains("25", displayedMessages[0]);
        Assert.Contains("50", displayedMessages[0]);
    }

    [TestMethod]
    public async Task EvaluateAsync_MaxTests_FailedTestCountAtThreshold_ReturnsFalse()
    {
        using RetryFailedTestsPipeServer server = await CreateServerWithFailedTestsAsync(["uid1", "uid2"]);
        var commandLineOptions = new TestCommandLineOptions(new()
        {
            [RetryCommandLineOptionsProvider.RetryFailedTestsMaxTestsOptionName] = ["2"],
        });
        Mock<IOutputDevice> outputDevice = new(MockBehavior.Strict);

        bool disabled = await RetryThresholdPolicy.EvaluateAsync(commandLineOptions, Mock.Of<IOutputDeviceDataProducer>(), outputDevice.Object, server, TestContext.CancellationToken);

        Assert.IsFalse(disabled);
        outputDevice.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task EvaluateAsync_MaxTests_FailedTestCountAboveThreshold_ReturnsTrueAndDisplaysExplanation()
    {
        using RetryFailedTestsPipeServer server = await CreateServerWithFailedTestsAsync(["uid1", "uid2", "uid3"]);
        var commandLineOptions = new TestCommandLineOptions(new()
        {
            [RetryCommandLineOptionsProvider.RetryFailedTestsMaxTestsOptionName] = ["2"],
        });
        List<string> displayedMessages = [];
        Mock<IOutputDevice> outputDevice = CreateCapturingOutputDevice(displayedMessages);

        bool disabled = await RetryThresholdPolicy.EvaluateAsync(commandLineOptions, Mock.Of<IOutputDeviceDataProducer>(), outputDevice.Object, server, TestContext.CancellationToken);

        Assert.IsTrue(disabled);
        Assert.HasCount(1, displayedMessages);
        Assert.Contains("2", displayedMessages[0]);
        Assert.Contains("3", displayedMessages[0]);
    }

    [TestMethod]
    public async Task EvaluateAsync_MaxTests_CountsDistinctUidsNotFoldedResults()
    {
        // The count-based threshold counts distinct test uids, so a folded data-driven test reporting several
        // failed results under one uid must contribute exactly one unit, not one per result.
        using RetryFailedTestsPipeServer server = await CreateServerWithFailedTestsAsync(["folded-uid"]);
        var commandLineOptions = new TestCommandLineOptions(new()
        {
            [RetryCommandLineOptionsProvider.RetryFailedTestsMaxTestsOptionName] = ["0"],
        });
        List<string> displayedMessages = [];
        Mock<IOutputDevice> outputDevice = CreateCapturingOutputDevice(displayedMessages);

        bool disabled = await RetryThresholdPolicy.EvaluateAsync(commandLineOptions, Mock.Of<IOutputDeviceDataProducer>(), outputDevice.Object, server, TestContext.CancellationToken);

        Assert.IsTrue(disabled);
        Assert.Contains("1", displayedMessages[0]);
    }

    [TestMethod]
    public async Task EvaluateAsync_MaxPercentageOptionPresentButNotMaxTests_UsesPercentageBranchOnly()
    {
        // When the percentage option is present, the count option (even if it would also be tripped) must not be
        // consulted: the two options are mutually exclusive and the percentage branch returns first.
        using RetryFailedTestsPipeServer server = await CreateServerWithCountsAsync(passed: 10, failed: 0, skipped: 0);
        var commandLineOptions = new TestCommandLineOptions(new()
        {
            [RetryCommandLineOptionsProvider.RetryFailedTestsMaxPercentageOptionName] = ["50"],
        });
        Mock<IOutputDevice> outputDevice = new(MockBehavior.Strict);

        bool disabled = await RetryThresholdPolicy.EvaluateAsync(commandLineOptions, Mock.Of<IOutputDeviceDataProducer>(), outputDevice.Object, server, TestContext.CancellationToken);

        Assert.IsFalse(disabled);
        outputDevice.VerifyNoOtherCalls();
    }

    private static Mock<IOutputDevice> CreateCapturingOutputDevice(List<string> displayedMessages)
    {
        Mock<IOutputDevice> outputDevice = new();
        outputDevice
            .Setup(x => x.DisplayAsync(It.IsAny<IOutputDeviceDataProducer>(), It.IsAny<IOutputDeviceData>(), It.IsAny<CancellationToken>()))
            .Callback<IOutputDeviceDataProducer, IOutputDeviceData, CancellationToken>(
                (_, data, _) => displayedMessages.Add(((ErrorMessageOutputDeviceData)data).Message))
            .Returns(Task.CompletedTask);
        return outputDevice;
    }

    private static ServiceProvider CreateServiceProvider()
    {
        ServiceProvider serviceProvider = new();
        serviceProvider.AddService(new SystemEnvironment());
        serviceProvider.AddService(new SystemTask());
        serviceProvider.AddService(Mock.Of<ITestApplicationCancellationTokenSource>(
            source => source.CancellationToken == CancellationToken.None));
        Mock<ILoggerFactory> loggerFactory = new();
        loggerFactory.Setup(factory => factory.CreateLogger(It.IsAny<string>())).Returns(Mock.Of<ILogger>());
        serviceProvider.AddService(loggerFactory.Object);
        return serviceProvider;
    }

    private static async Task<RetryFailedTestsPipeServer> CreateServerWithCountsAsync(int passed, int failed, int skipped)
    {
        var server = new RetryFailedTestsPipeServer(CreateServiceProvider(), [], Mock.Of<ILogger>());
        await InvokeCallbackAsync(server, new TestRunCountsRequest(passed, failed, skipped, []));
        return server;
    }

    private static async Task<RetryFailedTestsPipeServer> CreateServerWithFailedTestsAsync(string[] failedTestUids)
    {
        var server = new RetryFailedTestsPipeServer(CreateServiceProvider(), [], Mock.Of<ILogger>());
        foreach (string uid in failedTestUids)
        {
            await InvokeCallbackAsync(server, new FailedTestRequest(uid, uid));
        }

        return server;
    }

    private static Task InvokeCallbackAsync(RetryFailedTestsPipeServer server, object request)
    {
        MethodInfo callback = typeof(RetryFailedTestsPipeServer)
            .GetMethod("CallbackAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (Task)callback.Invoke(server, [request])!;
    }
}
