// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using AwesomeAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting.Internal;

using TestFramework.ForTestingMSTest;

namespace Microsoft.VisualStudio.TestPlatform.TestFramework.UnitTests.Internal;

public class TestDataSourceUtilitiesTests : TestContainer
{
    public void ComputeDefaultDisplayNameReturnsNullWhenDataIsNull()
    {
        MethodInfo method = GetMethod(nameof(SingleParameter));

        string? displayName = TestDataSourceUtilities.ComputeDefaultDisplayName(method, null);

        displayName.Should().BeNull();
    }

    public void ComputeDefaultDisplayNameFormatsPrimitiveArguments()
    {
        MethodInfo method = GetMethod(nameof(MultipleParameters));

        string? displayName = TestDataSourceUtilities.ComputeDefaultDisplayName(method, [1, "two", 3.0]);

        displayName.Should().Be($"{nameof(MultipleParameters)} (1,\"two\",3)");
    }

    public void ComputeDefaultDisplayNameQuotesStringArguments()
    {
        MethodInfo method = GetMethod(nameof(SingleParameter));

        string? displayName = TestDataSourceUtilities.ComputeDefaultDisplayName(method, ["hello"]);

        displayName.Should().Be($"{nameof(SingleParameter)} (\"hello\")");
    }

    public void ComputeDefaultDisplayNameQuotesCharArgumentsWithSingleQuotes()
    {
        MethodInfo method = GetMethod(nameof(SingleParameter));

        string? displayName = TestDataSourceUtilities.ComputeDefaultDisplayName(method, ['x']);

        displayName.Should().Be($"{nameof(SingleParameter)} ('x')");
    }

    public void ComputeDefaultDisplayNameRendersNullArgumentAsLiteralNull()
    {
        MethodInfo method = GetMethod(nameof(SingleParameter));

        string? displayName = TestDataSourceUtilities.ComputeDefaultDisplayName(method, [null]);

        displayName.Should().Be($"{nameof(SingleParameter)} (null)");
    }

    public void ComputeDefaultDisplayNameHumanizesSingleObjectArrayParameter()
    {
        // When the test method has a single object[] parameter, the data array itself represents
        // that single argument and must be rendered as one bracketed group, not as multiple arguments.
        MethodInfo method = GetMethod(nameof(SingleObjectArrayParameter));

        string? displayName = TestDataSourceUtilities.ComputeDefaultDisplayName(method, [1, "two"]);

        displayName.Should().Be($"{nameof(SingleObjectArrayParameter)} ([1,\"two\"])");
    }

    public void ComputeDefaultDisplayNameRecursivelyHumanizesNestedArrays()
    {
        MethodInfo method = GetMethod(nameof(SingleParameter));
        object?[] nested = [1, new object?[] { "a", 'b' }, null];

        string? displayName = TestDataSourceUtilities.ComputeDefaultDisplayName(method, [nested]);

        displayName.Should().Be($"{nameof(SingleParameter)} ([1,[\"a\",'b'],null])");
    }

    public void ComputeDefaultDisplayNameHumanizesNonObjectArrays()
    {
        MethodInfo method = GetMethod(nameof(SingleParameter));

        string? displayName = TestDataSourceUtilities.ComputeDefaultDisplayName(method, [new[] { 1, 2, 3 }]);

        displayName.Should().Be($"{nameof(SingleParameter)} ([1,2,3])");
    }

    public void ComputeDefaultDisplayNameHandlesEmptyDataArray()
    {
        MethodInfo method = GetMethod(nameof(SingleParameter));

        string? displayName = TestDataSourceUtilities.ComputeDefaultDisplayName(method, []);

        displayName.Should().Be($"{nameof(SingleParameter)} ()");
    }

    public void ComputeDefaultDisplayNameUsesReflectionTestMethodInfoDisplayNameWhenAvailable()
    {
        MethodInfo underlyingMethod = GetMethod(nameof(SingleParameter));
        var reflectionMethod = new ReflectionTestMethodInfo(underlyingMethod, "CustomDisplayName");

        string? displayName = TestDataSourceUtilities.ComputeDefaultDisplayName(reflectionMethod, ["value"]);

        displayName.Should().Be("CustomDisplayName (\"value\")");
    }

    public void ComputeDefaultDisplayNameIsCalledRepeatedlyWithoutCorruptingSharedState()
    {
        // Exercises the thread-static StringBuilder caching/reuse path across multiple invocations.
        MethodInfo method = GetMethod(nameof(SingleParameter));

        string? first = TestDataSourceUtilities.ComputeDefaultDisplayName(method, ["first"]);
        string? second = TestDataSourceUtilities.ComputeDefaultDisplayName(method, ["second"]);
        string? third = TestDataSourceUtilities.ComputeDefaultDisplayName(method, [42]);

        first.Should().Be($"{nameof(SingleParameter)} (\"first\")");
        second.Should().Be($"{nameof(SingleParameter)} (\"second\")");
        third.Should().Be($"{nameof(SingleParameter)} (42)");
    }

    private static MethodInfo GetMethod(string methodName)
        => typeof(TestDataSourceUtilitiesTests).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic)!;

    private static void SingleParameter(object? value)
    {
    }

    private static void MultipleParameters(int number, string text, double value)
    {
    }

    private static void SingleObjectArrayParameter(object?[] values)
    {
    }
}
