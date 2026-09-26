// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Testing.Platform.Helpers;

using Moq;

namespace Microsoft.Testing.Platform.UnitTests;

// Mutates the process-wide CultureInfo.DefaultThreadCurrentUICulture, so tests in this class must not
// run concurrently with each other or corrupt other tests' culture-dependent assertions.
[TestClass]
[DoNotParallelize]
public sealed class UILanguageOverrideTests
{
    private CultureInfo? _originalCulture;

    [TestInitialize]
    public void TestInitialize()
        => _originalCulture = CultureInfo.DefaultThreadCurrentUICulture;

    [TestCleanup]
    public void TestCleanup()
        => CultureInfo.DefaultThreadCurrentUICulture = _originalCulture;

    [TestMethod]
    public void SetCultureSpecifiedByUser_WhenNoEnvironmentVariablesSet_DoesNotChangeCulture()
    {
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        Mock<IEnvironment> environmentMock = CreateEnvironmentMock();

        UILanguageOverride.SetCultureSpecifiedByUser(environmentMock.Object);

        Assert.AreEqual(CultureInfo.InvariantCulture, CultureInfo.DefaultThreadCurrentUICulture);
        environmentMock.Verify(x => x.SetEnvironmentVariable(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public void SetCultureSpecifiedByUser_WhenTestingPlatformUILanguageSet_TakesHighestPrecedence()
    {
        Mock<IEnvironment> environmentMock = CreateEnvironmentMock(
            testingPlatformUILanguage: "fr-FR",
            dotnetCliUILanguage: "de-DE",
            vsLang: "1041"); // ja-JP

        UILanguageOverride.SetCultureSpecifiedByUser(environmentMock.Object);

        Assert.AreEqual("fr-FR", CultureInfo.DefaultThreadCurrentUICulture!.Name);
    }

    [TestMethod]
    public void SetCultureSpecifiedByUser_WhenOnlyDotnetCliUILanguageSet_UsesIt()
    {
        Mock<IEnvironment> environmentMock = CreateEnvironmentMock(dotnetCliUILanguage: "de-DE", vsLang: "1041");

        UILanguageOverride.SetCultureSpecifiedByUser(environmentMock.Object);

        Assert.AreEqual("de-DE", CultureInfo.DefaultThreadCurrentUICulture!.Name);
    }

    [TestMethod]
    public void SetCultureSpecifiedByUser_WhenOnlyVsLangSet_UsesLcid()
    {
        // 1036 is the LCID for fr-FR.
        Mock<IEnvironment> environmentMock = CreateEnvironmentMock(vsLang: "1036");

        UILanguageOverride.SetCultureSpecifiedByUser(environmentMock.Object);

        Assert.AreEqual("fr-FR", CultureInfo.DefaultThreadCurrentUICulture!.Name);
    }

    [TestMethod]
    public void SetCultureSpecifiedByUser_WhenTestingPlatformUILanguageIsInvalid_FallsBackToDotnetCliUILanguage()
    {
        // "!!invalid!!" contains characters that CultureInfo.GetCultureInfo rejects with
        // CultureNotFoundException, unlike an unrecognized-but-well-formed tag (e.g. "not-a-real-culture"),
        // which .NET happily accepts as a custom culture name.
        Mock<IEnvironment> environmentMock = CreateEnvironmentMock(
            testingPlatformUILanguage: "!!invalid!!",
            dotnetCliUILanguage: "de-DE");

        UILanguageOverride.SetCultureSpecifiedByUser(environmentMock.Object);

        Assert.AreEqual("de-DE", CultureInfo.DefaultThreadCurrentUICulture!.Name);
    }

    [TestMethod]
    public void SetCultureSpecifiedByUser_WhenVsLangIsNotAnInteger_IsIgnored()
    {
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        Mock<IEnvironment> environmentMock = CreateEnvironmentMock(vsLang: "not-an-lcid");

        UILanguageOverride.SetCultureSpecifiedByUser(environmentMock.Object);

        Assert.AreEqual(CultureInfo.InvariantCulture, CultureInfo.DefaultThreadCurrentUICulture);
    }

    [TestMethod]
    public void SetCultureSpecifiedByUser_FlowsOverrideToChildProcessEnvironmentVariables()
    {
        // Select the culture via TESTINGPLATFORM_UI_LANGUAGE itself, which is already "set" for the purposes
        // of the flow-to-children step (its GetEnvironmentVariable value is non-null), so that the other three
        // (initially unset) child-process variables can be asserted to be freshly written with the resolved value.
        Mock<IEnvironment> environmentMock = CreateEnvironmentMock(testingPlatformUILanguage: "fr-FR");

        UILanguageOverride.SetCultureSpecifiedByUser(environmentMock.Object);

        environmentMock.Verify(x => x.SetEnvironmentVariable("TESTINGPLATFORM_UI_LANGUAGE", It.IsAny<string>()), Times.Never);
        environmentMock.Verify(x => x.SetEnvironmentVariable("DOTNET_CLI_UI_LANGUAGE", "fr-FR"), Times.Once);
        environmentMock.Verify(x => x.SetEnvironmentVariable("VSLANG", It.IsAny<string>()), Times.Once);
        environmentMock.Verify(x => x.SetEnvironmentVariable("PreferredUILang", "fr-FR"), Times.Once);
    }

    [TestMethod]
    public void SetCultureSpecifiedByUser_DoesNotClobberAlreadySetChildProcessEnvironmentVariable()
    {
        // PreferredUILang plays no role in culture *selection* (unlike TESTINGPLATFORM_UI_LANGUAGE,
        // DOTNET_CLI_UI_LANGUAGE, and VSLANG, which are consulted in that precedence order), so pre-setting it
        // isolates the "do not clobber" flow-to-children behavior from the selection logic. Select the culture
        // via VSLANG since it is the lowest-precedence selection input.
        Mock<IEnvironment> environmentMock = CreateEnvironmentMock(vsLang: "1036"); // 1036 is the LCID for fr-FR.
        environmentMock.Setup(x => x.GetEnvironmentVariable("PreferredUILang")).Returns("already-set");

        UILanguageOverride.SetCultureSpecifiedByUser(environmentMock.Object);

        environmentMock.Verify(x => x.SetEnvironmentVariable("PreferredUILang", It.IsAny<string>()), Times.Never);
        environmentMock.Verify(x => x.SetEnvironmentVariable("DOTNET_CLI_UI_LANGUAGE", "fr-FR"), Times.Once);
    }

    private static Mock<IEnvironment> CreateEnvironmentMock(
        string? testingPlatformUILanguage = null,
        string? dotnetCliUILanguage = null,
        string? vsLang = null)
    {
        Mock<IEnvironment> environmentMock = new();
        environmentMock.Setup(x => x.GetEnvironmentVariable("TESTINGPLATFORM_UI_LANGUAGE")).Returns(testingPlatformUILanguage);
        environmentMock.Setup(x => x.GetEnvironmentVariable("DOTNET_CLI_UI_LANGUAGE")).Returns(dotnetCliUILanguage);
        environmentMock.Setup(x => x.GetEnvironmentVariable("VSLANG")).Returns(vsLang);
        environmentMock.Setup(x => x.GetEnvironmentVariable("PreferredUILang")).Returns((string?)null);
        return environmentMock;
    }
}
