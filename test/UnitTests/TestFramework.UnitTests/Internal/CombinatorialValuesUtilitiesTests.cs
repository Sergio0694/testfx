// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using AwesomeAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting.Combinatorial;
using Microsoft.VisualStudio.TestTools.UnitTesting.Internal;

using TestFramework.ForTestingMSTest;

namespace Microsoft.VisualStudio.TestPlatform.TestFramework.UnitTests.Internal;

/// <summary>
/// Direct unit tests for <see cref="CombinatorialValuesUtilities"/>, exercising code paths that are not
/// reachable through <c>CombinatorialDataAttribute</c>'s public surface alone (e.g. nullable-enum
/// inference and the null-parameter guard).
/// </summary>
public class CombinatorialValuesUtilitiesTests : TestContainer
{
    public void GetValuesForThrowsArgumentNullExceptionForNullParameter()
    {
        Action action = () => _ = CombinatorialValuesUtilities.GetValuesFor(null!).ToArray();

        action.Should().Throw<ArgumentNullException>().WithParameterName("parameter");
    }

    public void GetValuesForInfersNullableEnumAsNullFollowedByAllUnderlyingValues()
    {
        object?[] values = CombinatorialValuesUtilities.GetValuesFor(GetParameter(nameof(NullableEnumParameter))).ToArray();

        values.Should().Equal([null, DateTimeKind.Unspecified, DateTimeKind.Utc, DateTimeKind.Local]);
    }

    public void GetValuesForThrowsNotSupportedExceptionForNullableUnsupportedType()
    {
        Action action = () => _ = CombinatorialValuesUtilities.GetValuesFor(GetParameter(nameof(NullableUnsupportedParameter))).ToArray();

        action.Should().Throw<NotSupportedException>()
            .WithMessage($"*{typeof(Guid)}*{nameof(ICombinatorialValuesProvider)}*");
    }

    public void GetValuesForUsesAttributeValueProviderWhenPresentEvenForOtherwiseInferrableType()
    {
        object?[] values = CombinatorialValuesUtilities.GetValuesFor(GetParameter(nameof(ExplicitlyProvidedBoolParameter))).ToArray();

        // Even though `bool` is normally inferred as [true, false], an explicit provider takes precedence.
        values.Should().Equal([true]);
    }

    private static ParameterInfo GetParameter(string methodName)
        => typeof(CombinatorialValuesUtilitiesTests).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic)!.GetParameters()[0];

    private static void NullableEnumParameter(DateTimeKind? kind)
    {
    }

    private static void NullableUnsupportedParameter(Guid? value)
    {
    }

    private static void ExplicitlyProvidedBoolParameter([CombinatorialValues(true)] bool flag)
    {
    }
}
