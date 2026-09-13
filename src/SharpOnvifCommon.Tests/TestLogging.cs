// SharpOnvif
// Copyright (C) 2026 Lukas Volf
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.Generic;
using SharpOnvifCommon;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// Where the library reports what it could not do.
    /// <para>
    /// These reports used to be Debug.WriteLine, which a release build removes - so a Probe that
    /// never left the machine said nothing anyone could see, and IPv6 discovery finding nothing
    /// was indistinguishable from a network with nothing on it.
    /// </para>
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public sealed class TestLogging
    {
        private sealed class Recorder : IOnvifLogger
        {
            public readonly List<string> Lines = new List<string>();

            public void LogError(string error) { Lines.Add("error: " + error); }
            public void LogWarning(string warning) { Lines.Add("warning: " + warning); }
            public void LogInfo(string info) { Lines.Add("info: " + info); }
            public void LogDebug(string debug) { Lines.Add("debug: " + debug); }
            public void LogTrace(string trace) { Lines.Add("trace: " + trace); }

            public bool IsErrorEnabled { get; set; } = true;
            public bool IsWarningEnabled { get; set; } = true;
            public bool IsInfoEnabled { get; set; } = true;
            public bool IsDebugEnabled { get; set; } = true;
            public bool IsTraceEnabled { get; set; } = true;
        }

        private IOnvifLogger _original;

        [TestInitialize]
        public void Remember() { _original = Log.Logger; }

        [TestCleanup]
        public void Restore() { Log.Logger = _original; }

        [TestMethod]
        public void ReportsThroughTheLoggerItWasGiven()
        {
            var recorder = new Recorder();
            Log.Logger = recorder;

            Log.Error("an error");
            Log.Warning("a warning");
            Log.Info("something");
            Log.Debug("detail");
            Log.Trace("more detail");

            CollectionAssert.AreEqual(
                new[] { "error: an error", "warning: a warning", "info: something", "debug: detail", "trace: more detail" },
                recorder.Lines);
        }

        [TestMethod]
        public void SaysWhatWentWrongAlongsideTheMessage()
        {
            var recorder = new Recorder();
            Log.Logger = recorder;

            Log.Warning("could not probe", new InvalidOperationException("No route to host"));

            Assert.AreEqual(1, recorder.Lines.Count);
            StringAssert.Contains(recorder.Lines[0], "could not probe");
            StringAssert.Contains(recorder.Lines[0], "No route to host");
        }

        [TestMethod]
        public void AsksBeforeItComposes()
        {
            // A device that answers badly does so on every request, so a message nobody will read
            // must not be built.
            var recorder = new Recorder { IsDebugEnabled = false };
            Log.Logger = recorder;

            Log.Debug("detail");

            Assert.AreEqual(0, recorder.Lines.Count);
        }

        [TestMethod]
        public void SaysNothingUntilItIsSwitchedOn()
        {
            // The default: a library that is working is quiet.
            var logger = new DefaultOnvifLogger();

            Assert.IsFalse(logger.IsLoggingEnabled, "logging is opt-in");
            Assert.IsTrue(logger.IsErrorEnabled, "the levels are on, so switching logging on is enough");
        }

        [TestMethod]
        public void CanBeTurnedOffAltogether()
        {
            Log.Logger = NullOnvifLogger.Instance;

            Assert.IsFalse(Log.Logger.IsErrorEnabled);
            Log.Error("this goes nowhere");
        }

        [TestMethod]
        public void AlwaysHasSomewhereToReportTo()
        {
            Log.Logger = null;

            Assert.IsNotNull(Log.Logger, "a null logger would be a null reference on the next report");
            Log.Error("this has somewhere to go");
        }
    }
}
