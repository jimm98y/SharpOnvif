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
using SharpOnvifCommon.Soap;
using SharpOnvifClient;

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

        [TestMethod]
        public void ReportsThroughTheLoggerItWasGiven()
        {
            var recorder = new Recorder();

            recorder.Error("an error");
            recorder.Warning("a warning");
            recorder.Info("something");
            recorder.Debug("detail");
            recorder.Trace("more detail");

            CollectionAssert.AreEqual(
                new[] { "error: an error", "warning: a warning", "info: something", "debug: detail", "trace: more detail" },
                recorder.Lines);
        }

        [TestMethod]
        public void SaysWhatWentWrongAlongsideTheMessage()
        {
            var recorder = new Recorder();

            recorder.Warning("could not probe", new InvalidOperationException("No route to host"));

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

            recorder.Debug("detail");

            Assert.AreEqual(0, recorder.Lines.Count);
        }

        [TestMethod]
        public void ReportsNowhereWhenThereIsNoLogger()
        {
            // The default everywhere: an object that was given no logger must not need one.
            IOnvifLogger none = null;

            none.Error("an error");
            none.Warning("a warning");
            none.Info("something");
            none.Debug("detail");
            none.Trace("more detail");
        }

        [TestMethod]
        public void LetsTwoClientsReportToDifferentPlaces()
        {
            // The point of hanging the logger off the object rather than the process: an
            // application watching several cameras can tell which one is complaining.
            var first = new Recorder();
            var second = new Recorder();

            using (var a = new SharpOnvifClient.DeviceMgmt.DeviceClient(
                "http://192.168.1.10/onvif/device_service",
                new OnvifClientSettings { Logger = first }))
            using (var b = new SharpOnvifClient.DeviceMgmt.DeviceClient(
                "http://192.168.1.11/onvif/device_service",
                new OnvifClientSettings { Logger = second }))
            {
                Assert.AreNotSame(a, b);
            }

            first.Warning("only the first");

            Assert.AreEqual(1, first.Lines.Count);
            Assert.AreEqual(0, second.Lines.Count, "the other client's logger heard nothing of it");
        }

        [TestMethod]
        public void LetsTwoDiscoveryClientsReportToDifferentPlaces()
        {
            // Discovery is an instance for the same reason a client is: an application looking at
            // more than one thing can tell which of them is complaining.
            var first = new Recorder();
            var second = new Recorder();

            var a = new OnvifDiscoveryClient(first);
            var b = new OnvifDiscoveryClient { Logger = second };

            Assert.AreSame(first, a.Logger);
            Assert.AreSame(second, b.Logger);

            a.Logger.Warning("only the first");

            Assert.AreEqual(1, first.Lines.Count);
            Assert.AreEqual(0, second.Lines.Count, "the other discovery client heard nothing of it");
        }

        [TestMethod]
        public void SaysNothingUntilItIsSwitchedOn()
        {
            // The default logger: quiet until asked for.
            var logger = new DefaultOnvifLogger();

            Assert.IsFalse(logger.IsLoggingEnabled, "logging is opt-in");
            Assert.IsTrue(logger.IsErrorEnabled, "the levels are on, so switching logging on is enough");
        }

        [TestMethod]
        public void CanBeTurnedOffAltogether()
        {
            IOnvifLogger nowhere = NullOnvifLogger.Instance;

            Assert.IsFalse(nowhere.IsErrorEnabled);
            nowhere.Error("this goes nowhere");
        }
    }
}
