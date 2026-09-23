// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;

using AwesomeAssertions;

using Microsoft.VisualStudio.TestPlatform.MSTest.TestAdapter.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using TestFramework.ForTestingMSTest;

namespace MSTestAdapter.PlatformServices.UnitTests.Helpers;

/// <summary>
/// Direct tests for the assembly-level and method-level static helpers on <see cref="ReflectHelper"/> that
/// previously had no direct unit test coverage (only incidental exercise via <c>AssemblyEnumerator</c>).
/// Assembly-level attribute scenarios use a dynamically emitted assembly so each test can control exactly
/// which attributes (if any) are present, without depending on shared test-assembly state.
/// </summary>
public sealed class ReflectHelperTests : TestContainer
{
    public void MatchReturnTypeReturnsTrueWhenReturnTypeMatches()
    {
        MethodInfo method = typeof(ReflectHelperTests).GetMethod(nameof(SampleVoidMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;

        ReflectHelper.MatchReturnType(method, typeof(void)).Should().BeTrue();
    }

    public void MatchReturnTypeReturnsFalseWhenReturnTypeDoesNotMatch()
    {
        MethodInfo method = typeof(ReflectHelperTests).GetMethod(nameof(SampleVoidMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;

        ReflectHelper.MatchReturnType(method, typeof(int)).Should().BeFalse();
    }

    public void GetParallelizeAttributeReturnsNullWhenNotPresent()
    {
        Assembly assembly = CreateDynamicAssembly();

        ReflectHelper.GetParallelizeAttribute(assembly).Should().BeNull();
    }

    public void GetParallelizeAttributeReturnsAttributeWhenPresent()
    {
        Assembly assembly = CreateDynamicAssemblyWithAttribute(
            typeof(ParallelizeAttribute).GetConstructor(Type.EmptyTypes)!,
            constructorArgs: [],
            namedProperties: [(typeof(ParallelizeAttribute).GetProperty(nameof(ParallelizeAttribute.Workers))!, 4)]);

        ParallelizeAttribute? attribute = ReflectHelper.GetParallelizeAttribute(assembly);

        attribute.Should().NotBeNull();
        attribute!.Workers.Should().Be(4);
    }

    public void HasDiscoverInternalsAttributeReturnsFalseWhenNotPresent()
    {
        Assembly assembly = CreateDynamicAssembly();

        ReflectHelper.HasDiscoverInternalsAttribute(assembly).Should().BeFalse();
    }

    public void HasDiscoverInternalsAttributeReturnsTrueWhenPresent()
    {
        Assembly assembly = CreateDynamicAssemblyWithAttribute(
            typeof(DiscoverInternalsAttribute).GetConstructor(Type.EmptyTypes)!,
            constructorArgs: []);

        ReflectHelper.HasDiscoverInternalsAttribute(assembly).Should().BeTrue();
    }

    public void IsDoNotParallelizeSetReturnsFalseWhenNotPresent()
    {
        Assembly assembly = CreateDynamicAssembly();

        ReflectHelper.IsDoNotParallelizeSet(assembly).Should().BeFalse();
    }

    public void IsDoNotParallelizeSetReturnsTrueWhenPresent()
    {
        Assembly assembly = CreateDynamicAssemblyWithAttribute(
            typeof(DoNotParallelizeAttribute).GetConstructor(Type.EmptyTypes)!,
            constructorArgs: []);

        ReflectHelper.IsDoNotParallelizeSet(assembly).Should().BeTrue();
    }

    public void GetTestDataSourceDiscoveryOptionReturnsNullWhenNotPresent()
    {
        Assembly assembly = CreateDynamicAssembly();

        ReflectHelper.GetTestDataSourceDiscoveryOption(assembly).Should().BeNull();
    }

    public void GetTestDataSourceDiscoveryOptionReturnsConfiguredValueWhenPresent()
    {
        Assembly assembly = CreateDynamicAssemblyWithAttribute(
            typeof(TestDataSourceDiscoveryAttribute).GetConstructor([typeof(TestDataSourceDiscoveryOption)])!,
            constructorArgs: [TestDataSourceDiscoveryOption.DuringDiscovery]);

        ReflectHelper.GetTestDataSourceDiscoveryOption(assembly).Should().Be(TestDataSourceDiscoveryOption.DuringDiscovery);
    }

    public void GetTestDataSourceOptionsReturnsNullWhenNotPresent()
    {
        Assembly assembly = CreateDynamicAssembly();

        ReflectHelper.GetTestDataSourceOptions(assembly).Should().BeNull();
    }

    public void GetTestDataSourceOptionsReturnsConfiguredValueWhenPresent()
    {
        Assembly assembly = CreateDynamicAssemblyWithAttribute(
            typeof(TestDataSourceOptionsAttribute).GetConstructor([typeof(TestDataSourceUnfoldingStrategy)])!,
            constructorArgs: [TestDataSourceUnfoldingStrategy.Unfold]);

        TestDataSourceOptionsAttribute? options = ReflectHelper.GetTestDataSourceOptions(assembly);

        options.Should().NotBeNull();
        options!.UnfoldingStrategy.Should().Be(TestDataSourceUnfoldingStrategy.Unfold);
    }

    private void SampleVoidMethod()
    {
    }

    private static Assembly CreateDynamicAssembly()
        => DefineDynamicAssembly();

    private static Assembly CreateDynamicAssemblyWithAttribute(
        ConstructorInfo constructor,
        object?[] constructorArgs,
        (PropertyInfo Property, object? Value)[]? namedProperties = null)
    {
        AssemblyBuilder assemblyBuilder = DefineDynamicAssembly();
        CustomAttributeBuilder attributeBuilder = namedProperties is { Length: > 0 }
            ? new CustomAttributeBuilder(
                constructor,
                constructorArgs,
                [.. namedProperties.Select(p => p.Property)],
                [.. namedProperties.Select(p => p.Value)])
            : new CustomAttributeBuilder(constructor, constructorArgs);
        assemblyBuilder.SetCustomAttribute(attributeBuilder);

        return assemblyBuilder;
    }

#if NETFRAMEWORK
    private static AssemblyBuilder DefineDynamicAssembly()
        => AppDomain.CurrentDomain.DefineDynamicAssembly(
            new AssemblyName("ReflectHelperTests" + Guid.NewGuid().ToString("N")),
            AssemblyBuilderAccess.Run);
#else
    private static AssemblyBuilder DefineDynamicAssembly()
        => AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("ReflectHelperTests" + Guid.NewGuid().ToString("N")),
            AssemblyBuilderAccess.Run);
#endif
}
