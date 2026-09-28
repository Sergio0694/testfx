// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;

using Microsoft.VisualStudio.TestTools.UnitTesting.Internal;

namespace Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Enum to specify whether the data is stored as property, in method, or in field.
/// </summary>
public enum DynamicDataSourceType
{
    /// <summary>
    /// Data is declared as property.
    /// </summary>
    Property = 0,

    /// <summary>
    /// Data is declared in method.
    /// </summary>
    Method = 1,

    /// <summary>
    /// The data source type is auto-detected.
    /// </summary>
    AutoDetect = 2,

    /// <summary>
    /// Data is declared as field.
    /// </summary>
    Field = 3,
}

/// <summary>
/// Attribute to define dynamic data for a test method.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class DynamicDataAttribute : Attribute, ITestDataSource, ITestDataSourceEmptyDataSourceExceptionInfo, ITestDataSourceIgnoreCapability
{
    private readonly string _dynamicDataSourceName;
    private readonly DynamicDataSourceType _dynamicDataSourceType;
    private readonly object?[] _dynamicDataSourceArguments = [];
    [DynamicallyAccessedMembers(DynamicDataOperations.RequiredMemberTypes)]
    private readonly Type? _dynamicDataDeclaringType;

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicDataAttribute"/> class.
    /// </summary>
    /// <param name="dynamicDataSourceName">
    /// The name of method, property, or field having test data.
    /// </param>
    /// <param name="dynamicDataSourceType">
    /// Specifies whether the data is stored as property, in method, or in field.
    /// </param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public DynamicDataAttribute(string dynamicDataSourceName, DynamicDataSourceType dynamicDataSourceType)
    {
        _dynamicDataSourceName = dynamicDataSourceName;
        _dynamicDataSourceType = dynamicDataSourceType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicDataAttribute"/> class.
    /// </summary>
    /// <param name="dynamicDataSourceName">
    /// The name of method, property, or field having test data.
    /// </param>
    public DynamicDataAttribute(string dynamicDataSourceName)
    {
        _dynamicDataSourceName = dynamicDataSourceName;
        _dynamicDataSourceType = DynamicDataSourceType.AutoDetect;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicDataAttribute"/> class.
    /// </summary>
    /// <param name="dynamicDataSourceName">
    /// The name of method, property, or field having test data.
    /// </param>
    /// <param name="dynamicDataSourceArguments">
    /// Arguments to be passed to method referred to by <paramref name="dynamicDataSourceName"/>.
    /// </param>
    public DynamicDataAttribute(string dynamicDataSourceName, params object?[] dynamicDataSourceArguments)
    {
        _dynamicDataSourceName = dynamicDataSourceName;
        _dynamicDataSourceType = DynamicDataSourceType.AutoDetect;
        _dynamicDataSourceArguments = dynamicDataSourceArguments;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicDataAttribute"/> class when the test data is present in a class different
    /// from test method's class.
    /// </summary>
    /// <param name="dynamicDataSourceName">
    /// The name of method, property, or field having test data.
    /// </param>
    /// <param name="dynamicDataDeclaringType">
    /// The declaring type of property, method, or field having data. Useful in cases when declaring type is present in a class different from
    /// test method's class. If null, declaring type defaults to test method's class type.
    /// </param>
    /// <param name="dynamicDataSourceType">
    /// Specifies whether the data is stored as property, in method, or in field.
    /// </param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public DynamicDataAttribute(string dynamicDataSourceName, [DynamicallyAccessedMembers(DynamicDataOperations.RequiredMemberTypes)] Type dynamicDataDeclaringType, DynamicDataSourceType dynamicDataSourceType)
        : this(dynamicDataSourceName, dynamicDataSourceType) => _dynamicDataDeclaringType = dynamicDataDeclaringType;

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicDataAttribute"/> class when the test data is present in a class different
    /// from test method's class.
    /// </summary>
    /// <param name="dynamicDataSourceName">
    /// The name of method, property, or field having test data.
    /// </param>
    /// <param name="dynamicDataDeclaringType">
    /// The declaring type of property, method, or field having data. Useful in cases when declaring type is present in a class different from
    /// test method's class. If null, declaring type defaults to test method's class type.
    /// </param>
    public DynamicDataAttribute(string dynamicDataSourceName, [DynamicallyAccessedMembers(DynamicDataOperations.RequiredMemberTypes)] Type dynamicDataDeclaringType)
        : this(dynamicDataSourceName) => _dynamicDataDeclaringType = dynamicDataDeclaringType;

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicDataAttribute"/> class when the test data is present in a class different
    /// from test method's class.
    /// </summary>
    /// <param name="dynamicDataSourceName">
    /// The name of method, property, or field having test data.
    /// </param>
    /// <param name="dynamicDataDeclaringType">
    /// The declaring type of property, method, or field having data. Useful in cases when declaring type is present in a class different from
    /// test method's class. If null, declaring type defaults to test method's class type.
    /// </param>
    /// <param name="dynamicDataSourceArguments">
    /// Arguments to be passed to method referred to by <paramref name="dynamicDataSourceName"/>.
    /// </param>
    public DynamicDataAttribute(string dynamicDataSourceName, [DynamicallyAccessedMembers(DynamicDataOperations.RequiredMemberTypes)] Type dynamicDataDeclaringType, params object?[] dynamicDataSourceArguments)
        : this(dynamicDataSourceName)
    {
        _dynamicDataDeclaringType = dynamicDataDeclaringType;
        _dynamicDataSourceArguments = dynamicDataSourceArguments;
    }

    /// <summary>
    /// Gets or sets the name of method used to customize the display name in test results.
    /// </summary>
    public string? DynamicDataDisplayName { get; set; }

    /// <summary>
    /// Gets or sets the declaring type used to customize the display name in test results.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicDataOperations.RequiredMemberTypes)]
    public Type? DynamicDataDisplayNameDeclaringType { get; set; }

    /// <summary>
    /// Gets or sets a reason to ignore this dynamic data source. Setting the property to non-null value will ignore the dynamic data source.
    /// </summary>
    public string? IgnoreMessage { get; set; }

    /// <inheritdoc />
    public IEnumerable<object[]> GetData(MethodInfo methodInfo)
        => DynamicDataOperations.GetData(_dynamicDataDeclaringType, _dynamicDataSourceType, _dynamicDataSourceName, _dynamicDataSourceArguments, methodInfo);

    /// <inheritdoc />
    public string? GetDisplayName(MethodInfo methodInfo, object?[]? data)
    {
        if (DynamicDataDisplayName == null)
        {
            return TestDataSourceUtilities.ComputeDefaultDisplayName(methodInfo, data);
        }

        Type? dynamicDisplayNameDeclaringType = DynamicDataDisplayNameDeclaringType ?? DynamicDataOperations.GetTestMethodDeclaringType(methodInfo);
        DebugEx.Assert(dynamicDisplayNameDeclaringType is not null, "Declaring type of test data cannot be null.");

        // Prefer the source-generated accessor when available, so the display-name method is invoked without
        // reflecting over the declaring type (trim / Native AOT safe). Falls back to reflection otherwise.
        return DynamicDataSourceResolver.TryGetDisplayName(dynamicDisplayNameDeclaringType, DynamicDataDisplayName, methodInfo, data, out string? displayName)
            ? displayName
            : GetDisplayNameByReflection(dynamicDisplayNameDeclaringType, DynamicDataDisplayName, methodInfo, data);
    }

    // Caches the resolved and shape-validated display-name method per (declaring type, method name) pair.
    // Without this, every data row of a [DynamicData(..., DynamicDataDisplayName = "...")] test in
    // reflection mode (no source generator registration) would redo GetDeclaredMethod + GetParameters +
    // validation for the exact same method, once per row.
    private static readonly ConcurrentDictionary<DisplayNameMethodKey, MethodInfo> DisplayNameMethodCache = new();

    private static string? GetDisplayNameByReflection([DynamicallyAccessedMembers(DynamicDataOperations.RequiredMemberTypes)] Type dynamicDisplayNameDeclaringType, string displayNameMethodName, MethodInfo methodInfo, object?[]? data)
    {
        MethodInfo method = DisplayNameMethodCache.GetOrAdd(
            new DisplayNameMethodKey(dynamicDisplayNameDeclaringType, displayNameMethodName),
            static key => ResolveDisplayNameMethod(key.DeclaringType, key.MethodName));

        // Try to get the display name from the method.
        return method.Invoke(null, [methodInfo, data]) as string;
    }

    private static MethodInfo ResolveDisplayNameMethod([DynamicallyAccessedMembers(DynamicDataOperations.RequiredMemberTypes)] Type dynamicDisplayNameDeclaringType, string displayNameMethodName)
    {
        MethodInfo method = dynamicDisplayNameDeclaringType.GetTypeInfo().GetDeclaredMethod(displayNameMethodName)
            ?? throw new ArgumentNullException($"{DynamicDataSourceType.Method} {displayNameMethodName}");
        ParameterInfo[] parameters = method.GetParameters();
        return parameters.Length != 2
            || parameters[0].ParameterType != typeof(MethodInfo)
            || parameters[1].ParameterType != typeof(object[])
            || method.ReturnType != typeof(string)
            || !method.IsStatic
            || !method.IsPublic
            ? throw new ArgumentNullException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    FrameworkMessages.DynamicDataDisplayName,
                    displayNameMethodName,
                    nameof(String),
                    string.Join(", ", nameof(MethodInfo), typeof(object[]).Name)))
            : method;
    }

    private readonly struct DisplayNameMethodKey : IEquatable<DisplayNameMethodKey>
    {
        public DisplayNameMethodKey([DynamicallyAccessedMembers(DynamicDataOperations.RequiredMemberTypes)] Type declaringType, string methodName)
        {
            DeclaringType = declaringType;
            MethodName = methodName;
        }

        [DynamicallyAccessedMembers(DynamicDataOperations.RequiredMemberTypes)]
        public Type DeclaringType { get; }

        public string MethodName { get; }

        public bool Equals(DisplayNameMethodKey other)
            => DeclaringType == other.DeclaringType && string.Equals(MethodName, other.MethodName, StringComparison.Ordinal);

        public override bool Equals(object? obj)
            => obj is DisplayNameMethodKey other && Equals(other);

        public override int GetHashCode()
            => (DeclaringType.GetHashCode() * 397) ^ MethodName.GetHashCode();
    }

    /// <inheritdoc />
    string? ITestDataSourceEmptyDataSourceExceptionInfo.GetPropertyOrMethodNameForEmptyDataSourceException()
        => _dynamicDataSourceName;

    /// <inheritdoc />
    string? ITestDataSourceEmptyDataSourceExceptionInfo.GetPropertyOrMethodContainerTypeNameForEmptyDataSourceException(MethodInfo testMethodInfo)
    {
        Type? type = _dynamicDataDeclaringType ?? testMethodInfo.DeclaringType;
        DebugEx.Assert(type is not null, "Declaring type of test data cannot be null.");
        return type.FullName;
    }
}
