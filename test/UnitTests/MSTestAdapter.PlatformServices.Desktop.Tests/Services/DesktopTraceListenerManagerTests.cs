﻿// Copyright (c) Microsoft. All rights reserved.

namespace MSTestAdapter.PlatformServices.Desktop.UnitTests.Services
{
    extern alias FrameworkV1;
    using Assert = FrameworkV1::Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
    using Microsoft.VisualStudio.TestPlatform.MSTestAdapter.PlatformServices;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using TestClass = FrameworkV1::Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute;
    using TestMethod = FrameworkV1::Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute;
    using TestUtilities;

    [TestClass]
    public class DesktopTraceListenerManagerTests
    {

        [TestMethod]
        public void AddShouldAddTraceListenerToListOfTraceListeners()
        {
            var stringWriter = new StringWriter();
            var traceListenerManager = new TraceListenerManager(stringWriter, stringWriter);
            var traceListener = new TraceListenerWrapper(stringWriter);
            var originalCount = Trace.Listeners.Count;

            traceListenerManager.Add(traceListener);
            var newCount = Trace.Listeners.Count;

            Assert.AreEqual(originalCount + 1, newCount);
            Assert.IsTrue(Trace.Listeners.Contains(traceListener));
        }

        [TestMethod]
        public void RemoveShouldRemoveTraceListenerFromListOfTraceListeners()
        {
            var stringWriter = new StringWriter();
            var traceListenerManager = new TraceListenerManager(stringWriter, stringWriter);
            var traceListener = new TraceListenerWrapper(stringWriter);
            var originalCount = Trace.Listeners.Count;

            traceListenerManager.Add(traceListener);
            var countAfterAdding = Trace.Listeners.Count;

            traceListenerManager.Remove(traceListener);
            var countAfterRemoving = Trace.Listeners.Count;

            Assert.AreEqual(originalCount + 1, countAfterAdding);
            Assert.AreEqual(countAfterAdding - 1, countAfterRemoving);
            Assert.IsFalse(Trace.Listeners.Contains(traceListener));
        }

        [TestMethod]
        public void CloseShouldCallCloseOnCorrespondingTraceListener()
        {
            var stringWriter = new StringWriter();
            var traceListenerManager = new TraceListenerManager(stringWriter, stringWriter);

            StringWriter writer = new StringWriter(new StringBuilder("DummyTrace"));
            var traceListener = new TraceListenerWrapper(writer);
            traceListenerManager.Add(traceListener);
            traceListenerManager.Close(traceListener);

            //Tring to write after closing textWriter should throw exception
            Action ShouldThrowException = () => writer.WriteLine("Try to write something");
            ActionUtility.ActionShouldThrowExceptionOfType(ShouldThrowException, typeof(ObjectDisposedException));
        }

        [TestMethod]
        public void DisposeShouldCallDisposeOnCorrespondingTraceListener()
        {
            var stringWriter = new StringWriter();
            var traceListenerManager = new TraceListenerManager(stringWriter, stringWriter);

            StringWriter writer = new StringWriter(new StringBuilder("DummyTrace"));
            var traceListener = new TraceListenerWrapper(writer);
            traceListenerManager.Add(traceListener);
            traceListenerManager.Dispose(traceListener);

            //Tring to write after closing textWriter should throw exception
            Action ShouldThrowException = () => writer.WriteLine("Try to write something");
            ActionUtility.ActionShouldThrowExceptionOfType(ShouldThrowException, typeof(ObjectDisposedException));
        }
    }
}
