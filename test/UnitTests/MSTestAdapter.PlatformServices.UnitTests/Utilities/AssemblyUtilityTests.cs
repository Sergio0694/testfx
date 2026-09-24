// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using AwesomeAssertions;

using Microsoft.VisualStudio.TestPlatform.MSTestAdapter.PlatformServices.Utilities;

using TestFramework.ForTestingMSTest;

namespace MSTestAdapter.PlatformServices.UnitTests.Utilities;

public class AssemblyUtilityTests : TestContainer
{
    private readonly AssemblyUtility _assemblyUtility = new();

    public void IsAssemblyExtensionShouldReturnTrueForDllExtension()
        => _assemblyUtility.IsAssemblyExtension(".dll").Should().BeTrue();

    public void IsAssemblyExtensionShouldReturnTrueForExeExtension()
        => _assemblyUtility.IsAssemblyExtension(".exe").Should().BeTrue();

    public void IsAssemblyExtensionShouldReturnTrueRegardlessOfCase()
        => _assemblyUtility.IsAssemblyExtension(".DLL").Should().BeTrue();

    public void IsAssemblyExtensionShouldReturnFalseForNonAssemblyExtension()
        => _assemblyUtility.IsAssemblyExtension(".txt").Should().BeFalse();

    public void IsAssemblyExtensionShouldReturnFalseWhenLeadingDotIsMissing()
        => _assemblyUtility.IsAssemblyExtension("dll").Should().BeFalse();

    public void IsAssemblyExtensionShouldReturnFalseForEmptyString()
        => _assemblyUtility.IsAssemblyExtension(string.Empty).Should().BeFalse();
}
