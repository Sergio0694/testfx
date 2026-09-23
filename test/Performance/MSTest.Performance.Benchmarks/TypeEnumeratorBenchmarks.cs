// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;

using Microsoft.VisualStudio.TestPlatform.MSTest.TestAdapter.Discovery;
using Microsoft.VisualStudio.TestPlatform.MSTest.TestAdapter.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MSTest.Performance.Benchmarks;

/// <summary>
/// Benchmarks <see cref="TypeEnumerator.GetTests(List{string})"/> (invoked via <c>Enumerate</c> during
/// test discovery) against a class with many test methods, to measure the cost of pre-sizing the internal
/// "found tests" HashSet/List from the known upfront method count instead of growing them from default capacity.
/// </summary>
[MemoryDiagnoser]
public class TypeEnumeratorBenchmarks
{
    private ReflectHelper _reflectHelper = null!;
    private TypeValidator _typeValidator = null!;
    private TestMethodValidator _testMethodValidator = null!;

    [GlobalSetup]
    public void Setup()
    {
        _reflectHelper = new ReflectHelper();
        _typeValidator = new TypeValidator(_reflectHelper);
        _testMethodValidator = new TestMethodValidator(_reflectHelper, discoverInternals: false);
    }

    [Benchmark(Baseline = true)]
    public int EnumerateManyTestMethods()
    {
        var enumerator = new TypeEnumerator(typeof(ManyTestMethodsClass), "Benchmarks.dll", _reflectHelper, _typeValidator, _testMethodValidator);
        return enumerator.Enumerate([])?.Count ?? 0;
    }

    [TestClass]
    public class ManyTestMethodsClass
    {
        [TestMethod]
        public void Test001()
        {
        }

        [TestMethod]
        public void Test002()
        {
        }

        [TestMethod]
        public void Test003()
        {
        }

        [TestMethod]
        public void Test004()
        {
        }

        [TestMethod]
        public void Test005()
        {
        }

        [TestMethod]
        public void Test006()
        {
        }

        [TestMethod]
        public void Test007()
        {
        }

        [TestMethod]
        public void Test008()
        {
        }

        [TestMethod]
        public void Test009()
        {
        }

        [TestMethod]
        public void Test010()
        {
        }

        [TestMethod]
        public void Test011()
        {
        }

        [TestMethod]
        public void Test012()
        {
        }

        [TestMethod]
        public void Test013()
        {
        }

        [TestMethod]
        public void Test014()
        {
        }

        [TestMethod]
        public void Test015()
        {
        }

        [TestMethod]
        public void Test016()
        {
        }

        [TestMethod]
        public void Test017()
        {
        }

        [TestMethod]
        public void Test018()
        {
        }

        [TestMethod]
        public void Test019()
        {
        }

        [TestMethod]
        public void Test020()
        {
        }

        [TestMethod]
        public void Test021()
        {
        }

        [TestMethod]
        public void Test022()
        {
        }

        [TestMethod]
        public void Test023()
        {
        }

        [TestMethod]
        public void Test024()
        {
        }

        [TestMethod]
        public void Test025()
        {
        }

        [TestMethod]
        public void Test026()
        {
        }

        [TestMethod]
        public void Test027()
        {
        }

        [TestMethod]
        public void Test028()
        {
        }

        [TestMethod]
        public void Test029()
        {
        }

        [TestMethod]
        public void Test030()
        {
        }

        [TestMethod]
        public void Test031()
        {
        }

        [TestMethod]
        public void Test032()
        {
        }

        [TestMethod]
        public void Test033()
        {
        }

        [TestMethod]
        public void Test034()
        {
        }

        [TestMethod]
        public void Test035()
        {
        }

        [TestMethod]
        public void Test036()
        {
        }

        [TestMethod]
        public void Test037()
        {
        }

        [TestMethod]
        public void Test038()
        {
        }

        [TestMethod]
        public void Test039()
        {
        }

        [TestMethod]
        public void Test040()
        {
        }

        [TestMethod]
        public void Test041()
        {
        }

        [TestMethod]
        public void Test042()
        {
        }

        [TestMethod]
        public void Test043()
        {
        }

        [TestMethod]
        public void Test044()
        {
        }

        [TestMethod]
        public void Test045()
        {
        }

        [TestMethod]
        public void Test046()
        {
        }

        [TestMethod]
        public void Test047()
        {
        }

        [TestMethod]
        public void Test048()
        {
        }

        [TestMethod]
        public void Test049()
        {
        }

        [TestMethod]
        public void Test050()
        {
        }
    }
}
