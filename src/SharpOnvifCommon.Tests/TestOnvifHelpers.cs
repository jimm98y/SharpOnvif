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
using System.Globalization;
using System.Threading;
using SharpOnvifCommon;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// The durations and times Onvif carries. Every event subscription's lifetime is decided by
    /// these, so a value read as the wrong unit keeps a subscription - and the event source behind
    /// it - alive long after the client that asked for it is gone.
    /// </summary>
    [TestClass]
    public sealed class TestOnvifHelpers
    {
        [DataRow("PT60S", 0, 1, 0, DisplayName = "sixty seconds is a minute, not an hour")]
        [DataRow("PT1S", 0, 0, 1, DisplayName = "one second")]
        [DataRow("PT1M", 0, 1, 0, DisplayName = "one minute")]
        [DataRow("PT90M", 1, 30, 0, DisplayName = "ninety minutes")]
        [DataRow("PT1H", 1, 0, 0, DisplayName = "an hour, which Onvif clients do send")]
        [DataRow("PT1M30S", 0, 1, 30, DisplayName = "minutes and seconds together")]
        [DataRow("PT1H2M3S", 1, 2, 3, DisplayName = "the whole clock")]
        [TestMethod]
        public void ReadsADurationInTheUnitsItNames(string duration, int hours, int minutes, int seconds)
        {
            Assert.AreEqual(new TimeSpan(hours, minutes, seconds), OnvifHelpers.FromTimeout(duration));
        }

        [TestMethod]
        public void ReadsTheDurationsThisLibraryWrites()
        {
            // What GetTimeoutInSeconds and GetTimeoutInMinutes produce has to come back unchanged.
            Assert.AreEqual(TimeSpan.FromSeconds(45), OnvifHelpers.FromTimeout(OnvifHelpers.GetTimeoutInSeconds(45)));
            Assert.AreEqual(TimeSpan.FromMinutes(45), OnvifHelpers.FromTimeout(OnvifHelpers.GetTimeoutInMinutes(45)));
        }

        [TestMethod]
        public void TreatsNothingAsNoTime()
        {
            Assert.AreEqual(TimeSpan.Zero, OnvifHelpers.FromTimeout(null));
            Assert.AreEqual(TimeSpan.Zero, OnvifHelpers.FromTimeout(""));
        }

        [DataRow("60", DisplayName = "a bare number")]
        [DataRow("PTS", DisplayName = "no amount")]
        [DataRow("later", DisplayName = "not a duration at all")]
        [TestMethod]
        public void RefusesWhatIsNotADuration(string value)
        {
            Assert.ThrowsExactly<NotSupportedException>(() => OnvifHelpers.FromTimeout(value));
        }

        [TestMethod]
        public void TakesARelativeTerminationTimeFromNow()
        {
            var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            Assert.AreEqual(
                new DateTime(2026, 1, 1, 12, 1, 0, DateTimeKind.Utc),
                OnvifHelpers.FromAbsoluteOrRelativeDateTimeUTC(now, "PT60S", now));

            Assert.AreEqual(
                new DateTime(2026, 1, 1, 13, 0, 0, DateTimeKind.Utc),
                OnvifHelpers.FromAbsoluteOrRelativeDateTimeUTC(now, "PT1H", now));

            Assert.AreEqual(now, OnvifHelpers.FromAbsoluteOrRelativeDateTimeUTC(now, null, now),
                "no termination time means the default");
        }

        [TestMethod]
        public void TakesAnAbsoluteTerminationTimeAsUtc()
        {
            // The expiry is compared against DateTime.UtcNow, so a value that comes back as local
            // time expires the subscription early or late by the server's offset from UTC.
            var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            DateTime fromZulu = OnvifHelpers.FromAbsoluteOrRelativeDateTimeUTC(now, "2026-01-01T12:05:00Z", now);
            Assert.AreEqual(DateTimeKind.Utc, fromZulu.Kind);
            Assert.AreEqual(new DateTime(2026, 1, 1, 12, 5, 0, DateTimeKind.Utc), fromZulu);

            // An offset has to be applied, not ignored: 14:05+02:00 is 12:05 UTC.
            DateTime fromOffset = OnvifHelpers.FromAbsoluteOrRelativeDateTimeUTC(now, "2026-01-01T14:05:00+02:00", now);
            Assert.AreEqual(DateTimeKind.Utc, fromOffset.Kind);
            Assert.AreEqual(new DateTime(2026, 1, 1, 12, 5, 0, DateTimeKind.Utc), fromOffset);
        }

        [TestMethod]
        public void WritesATimeAsTheUtcItClaimsToBe()
        {
            // The format ends in Z, so the value has to be UTC before it is written - a local time
            // stamped Z tells the reader an instant that is hours out.
            var utc = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            Assert.AreEqual("2026-01-01T12:00:00.000Z", OnvifHelpers.DateTimeToString(utc));

            DateTime local = utc.ToLocalTime();
            Assert.AreEqual("2026-01-01T12:00:00.000Z", OnvifHelpers.DateTimeToString(local),
                "a local time has to be converted, not relabelled");

            Assert.AreEqual(utc, OnvifHelpers.StringToDateTime(OnvifHelpers.DateTimeToString(utc)),
                "writing and reading have to be inverses");
        }

        [TestMethod]
        public void ReadsAndWritesTheSameWhateverTheCultureIs()
        {
            // A device's clock is not written in the server's culture, and a non-Gregorian
            // calendar would otherwise put a different year on the wire.
            CultureInfo original = CultureInfo.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("th-TH");

                var utc = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
                Assert.AreEqual("2026-01-01T12:00:00.000Z", OnvifHelpers.DateTimeToString(utc));
                Assert.AreEqual(utc, OnvifHelpers.StringToDateTime("2026-01-01T12:00:00Z"));
                Assert.AreEqual(TimeSpan.FromSeconds(60), OnvifHelpers.FromTimeout(OnvifHelpers.GetTimeoutInSeconds(60)));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = original;
            }
        }
    }
}
