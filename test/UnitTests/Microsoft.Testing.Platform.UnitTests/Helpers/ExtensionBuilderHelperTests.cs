// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Helpers;
using Microsoft.Testing.Platform.Services;
using Microsoft.Testing.Platform.UnitTests.Helpers;

namespace Microsoft.Testing.Platform.UnitTests;

[TestClass]
public sealed class ExtensionBuilderHelperTests
{
    [TestMethod]
    public async Task BuildAndRegisterExtensionsAsync_Simple_WhenEnabled_AddsToResult()
    {
        List<TestExtension> result = [];
        TestExtension extension = new("uid1");

        await ExtensionBuilderHelper.BuildAndRegisterExtensionsAsync(
            [_ => extension],
            new ServiceProvider(),
            result);

        Assert.HasCount(1, result);
        Assert.AreSame(extension, result[0]);
    }

    [TestMethod]
    public async Task BuildAndRegisterExtensionsAsync_Simple_WhenDisabled_DoesNotAddToResult()
    {
        List<DisabledTestExtension> result = [];
        DisabledTestExtension extension = new("uid1");

        await ExtensionBuilderHelper.BuildAndRegisterExtensionsAsync(
            [_ => extension],
            new ServiceProvider(),
            result);

        Assert.IsEmpty(result);
    }

    [TestMethod]
    public async Task BuildAndRegisterExtensionsAsync_Simple_WhenEnabled_InvokesInitializeAsync()
    {
        List<InitializableTestExtension> result = [];
        InitializableTestExtension extension = new("uid1");

        await ExtensionBuilderHelper.BuildAndRegisterExtensionsAsync(
            [_ => extension],
            new ServiceProvider(),
            result);

        Assert.IsTrue(extension.Initialized);
    }

    [TestMethod]
    public async Task BuildAndRegisterExtensionsAsync_Simple_WithDuplicateUid_ThrowsInvalidOperationException()
    {
        TestExtension first = new("dup");
        TestExtension second = new("dup");
        List<TestExtension> result = [];

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => ExtensionBuilderHelper.BuildAndRegisterExtensionsAsync(
                [_ => first, _ => second],
                new ServiceProvider(),
                result));
    }

    [TestMethod]
    public async Task BuildAndRegisterExtensionsAsync_Simple_WhenRegisterInServiceProviderTrue_RegistersService()
    {
        List<TestExtension> result = [];
        TestExtension extension = new("uid1");
        ServiceProvider serviceProvider = new();

        await ExtensionBuilderHelper.BuildAndRegisterExtensionsAsync(
            [_ => extension],
            serviceProvider,
            result,
            registerInServiceProvider: true);

        Assert.Contains(extension, serviceProvider.Services);
    }

    [TestMethod]
    public async Task BuildAndRegisterExtensionsAsync_Simple_WhenRegisterInServiceProviderFalse_DoesNotRegisterService()
    {
        List<TestExtension> result = [];
        TestExtension extension = new("uid1");
        ServiceProvider serviceProvider = new();

        await ExtensionBuilderHelper.BuildAndRegisterExtensionsAsync(
            [_ => extension],
            serviceProvider,
            result);

        Assert.DoesNotContain(extension, serviceProvider.Services);
    }

    [TestMethod]
    public async Task BuildAndRegisterExtensionsAsync_Ordered_WhenEnabled_AddsToResultWithRegistrationOrder()
    {
        TestExtension first = new("uid1");
        TestExtension second = new("uid2");
        Func<IServiceProvider, TestExtension> firstFactory = _ => first;
        Func<IServiceProvider, TestExtension> secondFactory = _ => second;
        List<object> factoryOrdering = [firstFactory, secondFactory];
        List<(IExtension Extension, int RegistrationOrder)> result = [];

        await ExtensionBuilderHelper.BuildAndRegisterExtensionsAsync(
            [firstFactory, secondFactory],
            new ServiceProvider(),
            result,
            factoryOrdering);

        Assert.HasCount(2, result);
        Assert.AreSame(first, result[0].Extension);
        Assert.AreEqual(0, result[0].RegistrationOrder);
        Assert.AreSame(second, result[1].Extension);
        Assert.AreEqual(1, result[1].RegistrationOrder);
    }

    [TestMethod]
    public async Task BuildAndRegisterExtensionsAsync_Ordered_WhenDisabled_DoesNotAddToResult()
    {
        DisabledTestExtension extension = new("uid1");
        Func<IServiceProvider, DisabledTestExtension> factory = _ => extension;
        List<(IExtension Extension, int RegistrationOrder)> result = [];

        await ExtensionBuilderHelper.BuildAndRegisterExtensionsAsync(
            [factory],
            new ServiceProvider(),
            result,
            [factory]);

        Assert.IsEmpty(result);
    }

    [TestMethod]
    public async Task BuildAndRegisterExtensionsAsync_Ordered_WithDuplicateUid_ThrowsInvalidOperationException()
    {
        TestExtension first = new("dup");
        TestExtension second = new("dup");
        Func<IServiceProvider, TestExtension> firstFactory = _ => first;
        Func<IServiceProvider, TestExtension> secondFactory = _ => second;
        List<(IExtension Extension, int RegistrationOrder)> result = [];

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => ExtensionBuilderHelper.BuildAndRegisterExtensionsAsync(
                [firstFactory, secondFactory],
                new ServiceProvider(),
                result,
                [firstFactory, secondFactory]));
    }

    [TestMethod]
    public async Task BuildAndRegisterCompositeExtensionsAsync_WhenEnabledAndImplementsInterface_AddsToResult()
    {
        CompositeTestExtension extension = new("uid1");
        CompositeExtensionFactory<CompositeTestExtension> factory = new(() => extension);
        List<(IExtension Extension, int RegistrationOrder)> result = [];
        List<ICompositeExtensionFactory> alreadyBuilt = [];

        await ExtensionBuilderHelper.BuildAndRegisterCompositeExtensionsAsync<ICompositeMarker>(
            [factory],
            new ServiceProvider(),
            result,
            alreadyBuilt,
            [factory]);

        Assert.HasCount(1, result);
        Assert.AreSame(extension, result[0].Extension);
        Assert.HasCount(1, alreadyBuilt);
    }

    [TestMethod]
    public async Task BuildAndRegisterCompositeExtensionsAsync_WhenDoesNotImplementInterface_ThrowsInvalidOperationException()
    {
        CompositeTestExtension extension = new("uid1");
        CompositeExtensionFactory<CompositeTestExtension> factory = new(() => extension);
        List<(IExtension Extension, int RegistrationOrder)> result = [];
        List<ICompositeExtensionFactory> alreadyBuilt = [];

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => ExtensionBuilderHelper.BuildAndRegisterCompositeExtensionsAsync<IUnrelatedMarker>(
                [factory],
                new ServiceProvider(),
                result,
                alreadyBuilt,
                [factory]));
    }

    [TestMethod]
    public async Task BuildAndRegisterCompositeExtensionsAsync_WhenAlreadyBuilt_ReusesSharedInstanceWithoutCallingFactoryAgain()
    {
        int callCount = 0;
        CompositeExtensionFactory<CompositeTestExtension> factory = new(() =>
        {
            callCount++;
            return new CompositeTestExtension("uid1");
        });
        List<(IExtension Extension, int RegistrationOrder)> firstResult = [];
        List<(IExtension Extension, int RegistrationOrder)> secondResult = [];
        List<ICompositeExtensionFactory> alreadyBuilt = [];

        await ExtensionBuilderHelper.BuildAndRegisterCompositeExtensionsAsync<ICompositeMarker>(
            [factory], new ServiceProvider(), firstResult, alreadyBuilt, [factory]);
        await ExtensionBuilderHelper.BuildAndRegisterCompositeExtensionsAsync<ICompositeMarker>(
            [factory], new ServiceProvider(), secondResult, alreadyBuilt, [factory]);

        Assert.AreEqual(1, callCount);
        Assert.AreSame(firstResult[0].Extension, secondResult[0].Extension);
    }

    [TestMethod]
    public async Task BuildAndRegisterCompositeExtensionsAsync_WhenDisabled_DoesNotAddToResult()
    {
        DisabledCompositeTestExtension extension = new("uid1");
        CompositeExtensionFactory<DisabledCompositeTestExtension> factory = new(() => extension);
        List<(IExtension Extension, int RegistrationOrder)> result = [];
        List<ICompositeExtensionFactory> alreadyBuilt = [];

        await ExtensionBuilderHelper.BuildAndRegisterCompositeExtensionsAsync<ICompositeMarker>(
            [factory],
            new ServiceProvider(),
            result,
            alreadyBuilt,
            [factory]);

        Assert.IsEmpty(result);
        // Even disabled extensions are considered "already built" so we don't re-invoke the factory.
        Assert.HasCount(1, alreadyBuilt);
    }

    [TestMethod]
    public async Task BuildAndRegisterCompositeExtensionsInPlaceAsync_WhenEnabledAndImplementsInterface_AddsToResultAndRegistersService()
    {
        CompositeTestExtension extension = new("uid1");
        CompositeExtensionFactory<CompositeTestExtension> factory = new(() => extension);
        List<(IExtension Extension, int RegistrationOrder)> result = [];
        List<ICompositeExtensionFactory> alreadyBuilt = [];
        ServiceProvider serviceProvider = new();

        await ExtensionBuilderHelper.BuildAndRegisterCompositeExtensionsInPlaceAsync<ICompositeMarker>(
            [factory],
            serviceProvider,
            result,
            alreadyBuilt,
            [factory]);

        Assert.HasCount(1, result);
        Assert.AreSame(extension, result[0].Extension);
        Assert.Contains(extension, serviceProvider.Services);
    }

    [TestMethod]
    public async Task BuildAndRegisterCompositeExtensionsInPlaceAsync_WhenDoesNotImplementInterface_ThrowsInvalidOperationException()
    {
        CompositeTestExtension extension = new("uid1");
        CompositeExtensionFactory<CompositeTestExtension> factory = new(() => extension);
        List<(IExtension Extension, int RegistrationOrder)> result = [];
        List<ICompositeExtensionFactory> alreadyBuilt = [];

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => ExtensionBuilderHelper.BuildAndRegisterCompositeExtensionsInPlaceAsync<IUnrelatedMarker>(
                [factory],
                new ServiceProvider(),
                result,
                alreadyBuilt,
                [factory]));
    }

    [TestMethod]
    public async Task BuildAndRegisterCompositeExtensionsInPlaceAsync_WhenCalledTwiceForSameFactory_DoesNotReinitializeOrReValidate()
    {
        int initializeCount = 0;
        InPlaceInitializableExtension extension = new("uid1", () => initializeCount++);
        CompositeExtensionFactory<InPlaceInitializableExtension> factory = new(() => extension);
        List<(IExtension Extension, int RegistrationOrder)> firstResult = [];
        List<(IExtension Extension, int RegistrationOrder)> secondResult = [];
        List<ICompositeExtensionFactory> alreadyBuilt = [];

        await ExtensionBuilderHelper.BuildAndRegisterCompositeExtensionsInPlaceAsync<ICompositeMarker>(
            [factory], new ServiceProvider(), firstResult, alreadyBuilt, [factory]);
        await ExtensionBuilderHelper.BuildAndRegisterCompositeExtensionsInPlaceAsync<ICompositeMarker>(
            [factory], new ServiceProvider(), secondResult, alreadyBuilt, [factory]);

        Assert.AreEqual(1, initializeCount);
        Assert.HasCount(1, firstResult);
        Assert.HasCount(1, secondResult);
    }

    [TestMethod]
    public async Task BuildAndRegisterCompositeExtensionsInPlaceAsync_WhenDisabled_DoesNotAddToResultOrRegisterService()
    {
        DisabledCompositeTestExtension extension = new("uid1");
        CompositeExtensionFactory<DisabledCompositeTestExtension> factory = new(() => extension);
        List<(IExtension Extension, int RegistrationOrder)> result = [];
        List<ICompositeExtensionFactory> alreadyBuilt = [];
        ServiceProvider serviceProvider = new();

        await ExtensionBuilderHelper.BuildAndRegisterCompositeExtensionsInPlaceAsync<ICompositeMarker>(
            [factory],
            serviceProvider,
            result,
            alreadyBuilt,
            [factory]);

        Assert.IsEmpty(result);
        Assert.DoesNotContain(extension, serviceProvider.Services);
    }

    private interface ICompositeMarker : IExtension;

    private interface IUnrelatedMarker : IExtension;

    private sealed class DisabledTestExtension(string uid) : IExtension
    {
        public string Uid { get; } = uid;

        public string Version => "1.0.0";

        public string DisplayName => "DisplayName";

        public string Description => "Description";

        public Task<bool> IsEnabledAsync() => Task.FromResult(false);
    }

    private sealed class InitializableTestExtension(string uid) : IExtension, IAsyncInitializableExtension
    {
        public string Uid { get; } = uid;

        public string Version => "1.0.0";

        public string DisplayName => "DisplayName";

        public string Description => "Description";

        public bool Initialized { get; private set; }

        public Task<bool> IsEnabledAsync() => Task.FromResult(true);

        public Task InitializeAsync()
        {
            Initialized = true;
            return Task.CompletedTask;
        }
    }

    private sealed class CompositeTestExtension(string uid) : ICompositeMarker
    {
        public string Uid { get; } = uid;

        public string Version => "1.0.0";

        public string DisplayName => "DisplayName";

        public string Description => "Description";

        public Task<bool> IsEnabledAsync() => Task.FromResult(true);
    }

    private sealed class DisabledCompositeTestExtension(string uid) : ICompositeMarker
    {
        public string Uid { get; } = uid;

        public string Version => "1.0.0";

        public string DisplayName => "DisplayName";

        public string Description => "Description";

        public Task<bool> IsEnabledAsync() => Task.FromResult(false);
    }

    private sealed class InPlaceInitializableExtension(string uid, Action onInitialize) : ICompositeMarker, IAsyncInitializableExtension
    {
        public string Uid { get; } = uid;

        public string Version => "1.0.0";

        public string DisplayName => "DisplayName";

        public string Description => "Description";

        public Task<bool> IsEnabledAsync() => Task.FromResult(true);

        public Task InitializeAsync()
        {
            onInitialize();
            return Task.CompletedTask;
        }
    }
}
