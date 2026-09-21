// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using AwesomeAssertions;

using Microsoft.VisualStudio.TestPlatform.MSTestAdapter.PlatformServices.Helpers;

using TestFramework.ForTestingMSTest;

namespace Microsoft.VisualStudio.TestPlatform.MSTestAdapter.PlatformServices.UnitTests.Helpers;

public class ManagedNameParserTests : TestContainer
{
    public void ParseManagedMethodNameShouldParseSimpleMethodNameWithNoParameters()
    {
        ManagedNameParser.ParseManagedMethodName("MyMethod", out string methodName, out int arity, out string[]? parameterTypes);

        methodName.Should().Be("MyMethod");
        arity.Should().Be(0);
        parameterTypes.Should().BeNull();
    }

    public void ParseManagedMethodNameShouldParseMethodNameWithParameters()
    {
        ManagedNameParser.ParseManagedMethodName("MyMethod(System.String,System.Int32)", out string methodName, out int arity, out string[]? parameterTypes);

        methodName.Should().Be("MyMethod");
        arity.Should().Be(0);
        parameterTypes.Should().Equal("System.String", "System.Int32");
    }

    public void ParseManagedMethodNameShouldParseGenericMethodArity()
    {
        ManagedNameParser.ParseManagedMethodName("MyGenericMethod`1(!!0)", out string methodName, out int arity, out string[]? parameterTypes);

        methodName.Should().Be("MyGenericMethod");
        arity.Should().Be(1);
        parameterTypes.Should().Equal("!!0");
    }

    public void ParseManagedMethodNameShouldParseEmptyParameterList()
    {
        ManagedNameParser.ParseManagedMethodName("MyMethod()", out string methodName, out int arity, out string[]? parameterTypes);

        methodName.Should().Be("MyMethod");
        arity.Should().Be(0);
        parameterTypes.Should().BeNull();
    }

    public void ParseManagedMethodNameShouldParseGenericParameterType()
    {
        ManagedNameParser.ParseManagedMethodName(
            "MyMethod(System.Collections.Generic.List<System.String>)",
            out string methodName,
            out int arity,
            out string[]? parameterTypes);

        methodName.Should().Be("MyMethod");
        parameterTypes.Should().Equal("System.Collections.Generic.List<System.String>");
    }

    public void ParseManagedMethodNameShouldParseNestedGenericParameterType()
    {
        ManagedNameParser.ParseManagedMethodName(
            "MyMethod(System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>)",
            out string methodName,
            out int arity,
            out string[]? parameterTypes);

        methodName.Should().Be("MyMethod");
        parameterTypes.Should().Equal("System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>");
    }

    public void ParseManagedMethodNameShouldParseArrayParameterType()
    {
        ManagedNameParser.ParseManagedMethodName("MyMethod(System.String[])", out string methodName, out int arity, out string[]? parameterTypes);

        methodName.Should().Be("MyMethod");
        parameterTypes.Should().Equal("System.String[]");
    }

    public void ParseManagedMethodNameShouldParseMultiDimensionalArrayParameterType()
    {
        ManagedNameParser.ParseManagedMethodName("MyMethod(System.String[,])", out string methodName, out int arity, out string[]? parameterTypes);

        methodName.Should().Be("MyMethod");
        parameterTypes.Should().Equal("System.String[,]");
    }

    public void ParseManagedMethodNameShouldUnescapeQuotedMethodNameContainingSpaces()
    {
        // F# methods with spaces are wrapped in backticks in source and emitted as single-quoted
        // segments in the managed name, e.g. ``my method`` becomes 'my method'.
        ManagedNameParser.ParseManagedMethodName("'my method'", out string methodName, out int arity, out string[]? parameterTypes);

        methodName.Should().Be("my method");
        arity.Should().Be(0);
        parameterTypes.Should().BeNull();
    }

    public void ParseManagedMethodNameShouldThrowWhenMethodNameContainsUnquotedWhitespace()
    {
        Action action = () => ManagedNameParser.ParseManagedMethodName("My Method", out _, out _, out _);

        action.Should().Throw<InvalidManagedNameException>();
    }

    public void ParseManagedMethodNameShouldThrowWhenArityIsNotNumeric()
    {
        Action action = () => ManagedNameParser.ParseManagedMethodName("MyMethod`NotANumber(System.String)", out _, out _, out _);

        action.Should().Throw<InvalidManagedNameException>();
    }

    public void ParseManagedMethodNameShouldThrowWhenParameterListIsUnterminated()
    {
        Action action = () => ManagedNameParser.ParseManagedMethodName("MyMethod(System.String", out _, out _, out _);

        action.Should().Throw<InvalidManagedNameException>();
    }

    public void ParseManagedMethodNameShouldThrowWhenTrailingCharactersFollowParameterList()
    {
        Action action = () => ManagedNameParser.ParseManagedMethodName("MyMethod(System.String)garbage", out _, out _, out _);

        action.Should().Throw<InvalidManagedNameException>();
    }

    public void ParseManagedMethodNameShouldThrowWhenParameterTypeContainsUnquotedWhitespace()
    {
        Action action = () => ManagedNameParser.ParseManagedMethodName("MyMethod(System. String)", out _, out _, out _);

        action.Should().Throw<InvalidManagedNameException>();
    }
}
