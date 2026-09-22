// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using AwesomeAssertions;

using Microsoft.VisualStudio.TestPlatform.MSTestAdapter.PlatformServices.Helpers;

using TestFramework.ForTestingMSTest;

namespace Microsoft.VisualStudio.TestPlatform.MSTestAdapter.UnitTests;

public class ManagedNameHelperTests : TestContainer
{
    public void GetManagedNameAndHierarchyThrowsArgumentNullExceptionForNullMethod()
    {
        Action action = () => ManagedNameHelper.GetManagedNameAndHierarchy(null!, out _, out _, out _);

        action.Should().Throw<ArgumentNullException>();
    }

    public void GetManagedNameAndHierarchyReturnsExpectedNamesForSimpleMethod()
    {
        MethodInfo method = typeof(SampleClass).GetMethod(nameof(SampleClass.SimpleMethod))!;

        ManagedNameHelper.GetManagedNameAndHierarchy(method, out string managedTypeName, out string managedMethodName, out string?[] hierarchyValues);

        managedTypeName.Should().Be(typeof(SampleClass).FullName);
        managedMethodName.Should().Be("SimpleMethod");
        hierarchyValues.Should().HaveCount(4);
        hierarchyValues[HierarchyConstants.Levels.TestGroupIndex].Should().Be("SimpleMethod");
        hierarchyValues[HierarchyConstants.Levels.ClassIndex].Should().Be($"{nameof(ManagedNameHelperTests)}+{nameof(SampleClass)}");
        hierarchyValues[HierarchyConstants.Levels.NamespaceIndex].Should().Be(typeof(SampleClass).Namespace);
        hierarchyValues[HierarchyConstants.Levels.ContainerIndex].Should().BeNull();
    }

    public void GetManagedNameAndHierarchyIncludesParameterTypes()
    {
        MethodInfo method = typeof(SampleClass).GetMethod(nameof(SampleClass.MethodWithParameters))!;

        ManagedNameHelper.GetManagedNameAndHierarchy(method, out _, out string managedMethodName, out _);

        managedMethodName.Should().Be("MethodWithParameters(System.Int32,System.String)");
    }

    public void GetManagedNameAndHierarchyIncludesArrayParameterType()
    {
        MethodInfo method = typeof(SampleClass).GetMethod(nameof(SampleClass.MethodWithArrayParameter))!;

        ManagedNameHelper.GetManagedNameAndHierarchy(method, out _, out string managedMethodName, out _);

        managedMethodName.Should().Be("MethodWithArrayParameter(System.String[])");
    }

    public void GetManagedNameAndHierarchyIncludesArityForGenericMethod()
    {
        MethodInfo method = typeof(SampleClass).GetMethod(nameof(SampleClass.GenericMethod))!;

        ManagedNameHelper.GetManagedNameAndHierarchy(method, out _, out string managedMethodName, out _);

        managedMethodName.Should().Be("GenericMethod`1(!!0)");
    }

    public void GetManagedNameAndHierarchyUsesOpenGenericTypeDefinitionForGenericDeclaringType()
    {
        Type closedGenericType = typeof(SampleGenericClass<>).MakeGenericType(typeof(int));
        MethodInfo method = closedGenericType.GetMethod(nameof(SampleGenericClass<>.MethodOnGenericType))!;

        ManagedNameHelper.GetManagedNameAndHierarchy(method, out string managedTypeName, out string managedMethodName, out _);

        // The managed type name is based on the open generic type definition, stripped of the closed type argument.
        managedTypeName.Should().Be(typeof(SampleGenericClass<>).FullName);
        managedMethodName.Should().Be("MethodOnGenericType(!0)");
    }

    public void GetManagedNameAndHierarchySetsNullNamespaceHierarchyForNoNamespaceType()
    {
        MethodInfo method = typeof(NoNamespaceType).GetMethod(nameof(NoNamespaceType.Method))!;

        ManagedNameHelper.GetManagedNameAndHierarchy(method, out _, out _, out string?[] hierarchyValues);

        hierarchyValues[HierarchyConstants.Levels.NamespaceIndex].Should().BeNull();
    }

    public void GetMethodRoundTripsForSimpleMethod()
    {
        MethodInfo originalMethod = typeof(SampleClass).GetMethod(nameof(SampleClass.SimpleMethod))!;
        ManagedNameHelper.GetManagedNameAndHierarchy(originalMethod, out string managedTypeName, out string managedMethodName, out _);

        MethodInfo resolvedMethod = ManagedNameHelper.GetMethod(typeof(SampleClass).Assembly, managedTypeName, managedMethodName);

        resolvedMethod.Should().BeSameAs(originalMethod);
    }

    public void GetMethodRoundTripsForMethodWithParameters()
    {
        MethodInfo originalMethod = typeof(SampleClass).GetMethod(nameof(SampleClass.MethodWithParameters))!;
        ManagedNameHelper.GetManagedNameAndHierarchy(originalMethod, out string managedTypeName, out string managedMethodName, out _);

        MethodInfo resolvedMethod = ManagedNameHelper.GetMethod(typeof(SampleClass).Assembly, managedTypeName, managedMethodName);

        resolvedMethod.Should().BeSameAs(originalMethod);
    }

    public void GetMethodRoundTripsForGenericMethod()
    {
        MethodInfo originalMethod = typeof(SampleClass).GetMethod(nameof(SampleClass.GenericMethod))!;
        ManagedNameHelper.GetManagedNameAndHierarchy(originalMethod, out string managedTypeName, out string managedMethodName, out _);

        MethodInfo resolvedMethod = ManagedNameHelper.GetMethod(typeof(SampleClass).Assembly, managedTypeName, managedMethodName);

        resolvedMethod.Should().BeSameAs(originalMethod);
    }

    public void GetMethodThrowsInvalidManagedNameExceptionForUnknownType()
    {
        Action action = () => ManagedNameHelper.GetMethod(typeof(SampleClass).Assembly, "This.Type.Does.Not.Exist", "SimpleMethod");

        action.Should().Throw<InvalidManagedNameException>();
    }

    public void GetMethodThrowsInvalidManagedNameExceptionForUnknownMethod()
    {
        Action action = () => ManagedNameHelper.GetMethod(typeof(SampleClass).Assembly, typeof(SampleClass).FullName!, "DoesNotExist()");

        action.Should().Throw<InvalidManagedNameException>();
    }

    public void GetMethodThrowsInvalidManagedNameExceptionForMalformedManagedMethodName()
    {
        Action action = () => ManagedNameHelper.GetMethod(typeof(SampleClass).Assembly, typeof(SampleClass).FullName!, "SimpleMethod(");

        action.Should().Throw<InvalidManagedNameException>();
    }

    public void ParseEscapedStringReturnsInputWhenNoQuotesPresent()
    {
        string result = ManagedNameHelper.ParseEscapedString("PlainName");

        result.Should().Be("PlainName");
    }

    public void ParseEscapedStringUnescapesQuotedSegment()
    {
        string result = ManagedNameHelper.ParseEscapedString("'quoted name'");

        result.Should().Be("quoted name");
    }

    public void ParseEscapedStringUnescapesEscapedQuoteAndBackslash()
    {
        string result = ManagedNameHelper.ParseEscapedString(@"'a\'b\\c'");

        result.Should().Be(@"a'b\c");
    }

    public void ParseEscapedStringUnescapesUnicodeSequence()
    {
        string result = ManagedNameHelper.ParseEscapedString(@"'a\u0062c'");

        result.Should().Be("abc");
    }

    public void ParseEscapedStringThrowsInvalidManagedNameExceptionForMalformedUnicodeSequence()
    {
        Action action = () => ManagedNameHelper.ParseEscapedString(@"'a\uZZZZc'");

        action.Should().Throw<InvalidManagedNameException>();
    }

    public void ParseEscapedStringThrowsInvalidManagedNameExceptionForUnterminatedQuote()
    {
        Action action = () => ManagedNameHelper.ParseEscapedString("'unterminated");

        action.Should().Throw<InvalidManagedNameException>();
    }

    private class SampleClass
    {
        public void SimpleMethod()
        {
        }

        public void MethodWithParameters(int i, string s)
        {
        }

        public void MethodWithArrayParameter(string[] values)
        {
        }

        public void GenericMethod<T>(T value)
        {
        }
    }

    private class SampleGenericClass<T>
    {
        public void MethodOnGenericType(T value)
        {
        }
    }
}
