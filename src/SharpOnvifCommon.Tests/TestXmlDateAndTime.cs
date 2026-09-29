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
using SharpOnvifCommon.Xml;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// xs:time and xs:date, which say less than xs:dateTime and are easy to shift by accident.
    /// <para>
    /// Every value used to be read as a UTC xs:dateTime and written back without a zone, so a
    /// time with an offset came back as a different clock time a device took for its own, and a
    /// date with an offset came back as the day before. WCF got the offset cases right but wrote a
    /// bare time with the offset of whatever machine it ran on; a bare time is the device's own
    /// clock, and stays bare.
    /// </para>
    /// </summary>
    [TestClass]
    public sealed class TestXmlDateAndTime
    {
        [DataRow("08:00:00", "08:00:00.0000000", DisplayName = "a bare time stays bare")]
        [DataRow("08:00:00Z", "08:00:00.0000000Z", DisplayName = "UTC stays UTC")]
        [DataRow("08:00:00+02:00", "06:00:00.0000000Z", DisplayName = "an offset becomes the same instant in UTC")]
        [DataRow("08:00:00-05:00", "13:00:00.0000000Z", DisplayName = "a negative offset")]
        [DataRow("00:30:00+02:00", "22:30:00.0000000Z", DisplayName = "back across midnight")]
        [DataRow("23:30:00-02:00", "01:30:00.0000000Z", DisplayName = "forward across midnight")]
        [DataRow("08:00:00.5Z", "08:00:00.5000000Z", DisplayName = "a fraction of a second")]
        [DataRow(" 08:00:00 ", "08:00:00.0000000", DisplayName = "surrounding whitespace")]
        [TestMethod]
        public void KeepsWhatATimeSaysAboutItsZone(string read, string written)
        {
            Assert.AreEqual(written, XmlPrimitives.ToTimeString(XmlPrimitives.ToTime(read)));
        }

        [TestMethod]
        public void ReadsABareTimeAsUnspecifiedAndAZonedOneAsUtc()
        {
            Assert.AreEqual(DateTimeKind.Unspecified, XmlPrimitives.ToTime("08:00:00").Kind);
            Assert.AreEqual(DateTimeKind.Utc, XmlPrimitives.ToTime("08:00:00+02:00").Kind);

            // The date part is fixed, so a conversion across midnight cannot leave the calendar.
            Assert.AreEqual(new DateTime(1, 1, 1, 22, 30, 0), XmlPrimitives.ToTime("00:30:00+02:00"));
        }

        [TestMethod]
        public void WritesALocalTimeWithItsOffset()
        {
            var local = new DateTime(1, 1, 1, 8, 0, 0, DateTimeKind.Local);
            string written = XmlPrimitives.ToTimeString(local);

            StringAssert.StartsWith(written, "08:00:00.0000000");
            StringAssert.Matches(written, new System.Text.RegularExpressions.Regex(@"[+-]\d{2}:\d{2}$"));
        }

        [DataRow("2026-01-02", DisplayName = "a bare date")]
        [DataRow("2026-01-02Z", DisplayName = "a UTC date")]
        [DataRow("2026-01-02+02:00", DisplayName = "east of UTC, which read as the day before")]
        [DataRow("2026-01-02-05:00", DisplayName = "west of UTC")]
        [DataRow("2026-01-02+14:00", DisplayName = "the furthest offset there is")]
        [TestMethod]
        public void KeepsTheDayADateNames(string read)
        {
            Assert.AreEqual("2026-01-02", XmlPrimitives.ToDateString(XmlPrimitives.ToDate(read)));
        }

        [DataRow("", DisplayName = "empty")]
        [DataRow("8:00", DisplayName = "not a time")]
        [DataRow("25:00:00", DisplayName = "no such hour")]
        [DataRow("08:61:00", DisplayName = "no such minute")]
        [DataRow("24:30:00", DisplayName = "past the end of the day")]
        [TestMethod]
        public void ReadsATimeItCannotUnderstandAsNothing(string read)
        {
            Assert.AreEqual(default(DateTime), XmlPrimitives.ToTime(read));
        }

        [DataRow("2026-02-30", DisplayName = "no such day")]
        [DataRow("2026-13-01", DisplayName = "no such month")]
        [DataRow("yesterday", DisplayName = "not a date")]
        [TestMethod]
        public void ReadsADateItCannotUnderstandAsNothing(string read)
        {
            Assert.AreEqual(default(DateTime), XmlPrimitives.ToDate(read));
        }
    }
}
