using System;
using System.Globalization;
using System.Xml;

namespace __RUNTIME__.Xml
{
    /// <summary>
    /// Converts between XSD lexical forms and CLR values.
    /// <para>
    /// Parsing is deliberately more forgiving than the schema: cameras in the field emit empty
    /// elements for optional numbers, booleans as "1"/"0", and timestamps with or without a zone.
    /// Writing always produces the canonical form, so that what the client sends is valid.
    /// </para>
    /// </summary>
    public static class XmlPrimitives
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        // ------------------------------------------------------------------ writing

        public static string ToString(bool value) => value ? "true" : "false";

        public static string ToString(sbyte value) => value.ToString(Invariant);
        public static string ToString(byte value) => value.ToString(Invariant);
        public static string ToString(short value) => value.ToString(Invariant);
        public static string ToString(ushort value) => value.ToString(Invariant);
        public static string ToString(int value) => value.ToString(Invariant);
        public static string ToString(uint value) => value.ToString(Invariant);
        public static string ToString(long value) => value.ToString(Invariant);
        public static string ToString(ulong value) => value.ToString(Invariant);
        public static string ToString(decimal value) => value.ToString(Invariant);

        /// <summary>
        /// xs:float and xs:double use "INF"/"-INF"/"NaN" rather than the CLR spellings, and
        /// round-trip formatting so no precision is lost on the wire.
        /// </summary>
        public static string ToString(float value)
        {
            if (float.IsPositiveInfinity(value)) return "INF";
            if (float.IsNegativeInfinity(value)) return "-INF";
            if (float.IsNaN(value)) return "NaN";
            return value.ToString("R", Invariant);
        }

        public static string ToString(double value)
        {
            if (double.IsPositiveInfinity(value)) return "INF";
            if (double.IsNegativeInfinity(value)) return "-INF";
            if (double.IsNaN(value)) return "NaN";
            return value.ToString("R", Invariant);
        }

        /// <summary>Writes xs:dateTime. Unspecified kinds are treated as UTC, which is what Onvif means.</summary>
        public static string ToString(DateTime value)
        {
            switch (value.Kind)
            {
                case DateTimeKind.Local:
                    return value.ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz", Invariant);
                case DateTimeKind.Utc:
                    return value.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", Invariant);
                default:
                    return value.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", Invariant) + "Z";
            }
        }

        public static string ToDateString(DateTime value) => value.ToString("yyyy-MM-dd", Invariant);

        public static string ToTimeString(DateTime value) => value.ToString("HH:mm:ss.fffffff", Invariant);

        public static string ToString(byte[] value) => value == null ? null : Convert.ToBase64String(value);

        public static string ToHexString(byte[] value)
        {
            if (value == null) return null;
            char[] chars = new char[value.Length * 2];
            for (int i = 0; i < value.Length; i++)
            {
                byte b = value[i];
                chars[i * 2] = HexDigit(b >> 4);
                chars[i * 2 + 1] = HexDigit(b & 0xF);
            }
            return new string(chars);
        }

        private static char HexDigit(int nibble) => (char)(nibble < 10 ? '0' + nibble : 'A' + (nibble - 10));

        // ------------------------------------------------------------------ reading

        public static bool ToBoolean(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            switch (text.Trim())
            {
                case "true":
                case "TRUE":
                case "True":
                case "1":
                    return true;
                default:
                    return false;
            }
        }

        public static sbyte ToSByte(string text) =>
            sbyte.TryParse(Trim(text), NumberStyles.Integer, Invariant, out sbyte value) ? value : (sbyte)0;

        public static byte ToByte(string text) =>
            byte.TryParse(Trim(text), NumberStyles.Integer, Invariant, out byte value) ? value : (byte)0;

        public static short ToInt16(string text) =>
            short.TryParse(Trim(text), NumberStyles.Integer, Invariant, out short value) ? value : (short)0;

        public static ushort ToUInt16(string text) =>
            ushort.TryParse(Trim(text), NumberStyles.Integer, Invariant, out ushort value) ? value : (ushort)0;

        public static int ToInt32(string text) =>
            int.TryParse(Trim(text), NumberStyles.Integer, Invariant, out int value) ? value : 0;

        public static uint ToUInt32(string text) =>
            uint.TryParse(Trim(text), NumberStyles.Integer, Invariant, out uint value) ? value : 0u;

        public static long ToInt64(string text) =>
            long.TryParse(Trim(text), NumberStyles.Integer, Invariant, out long value) ? value : 0L;

        public static ulong ToUInt64(string text) =>
            ulong.TryParse(Trim(text), NumberStyles.Integer, Invariant, out ulong value) ? value : 0UL;

        public static decimal ToDecimal(string text) =>
            decimal.TryParse(Trim(text), NumberStyles.Float, Invariant, out decimal value) ? value : 0m;

        public static float ToSingle(string text)
        {
            string trimmed = Trim(text);
            if (trimmed.Length == 0) return 0f;
            if (trimmed == "INF" || trimmed == "+INF") return float.PositiveInfinity;
            if (trimmed == "-INF") return float.NegativeInfinity;
            if (trimmed == "NaN") return float.NaN;
            return float.TryParse(trimmed, NumberStyles.Float, Invariant, out float value) ? value : 0f;
        }

        public static double ToDouble(string text)
        {
            string trimmed = Trim(text);
            if (trimmed.Length == 0) return 0d;
            if (trimmed == "INF" || trimmed == "+INF") return double.PositiveInfinity;
            if (trimmed == "-INF") return double.NegativeInfinity;
            if (trimmed == "NaN") return double.NaN;
            return double.TryParse(trimmed, NumberStyles.Float, Invariant, out double value) ? value : 0d;
        }

        /// <summary>
        /// Parses xs:dateTime, xs:date, or xs:time. A value carrying a zone is converted to UTC;
        /// one without is taken as UTC, because that is what Onvif devices mean by a bare
        /// timestamp even though the schema leaves it unspecified.
        /// </summary>
        public static DateTime ToDateTime(string text)
        {
            string trimmed = Trim(text);
            if (trimmed.Length == 0) return default(DateTime);

            const DateTimeStyles styles =
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal | DateTimeStyles.AllowWhiteSpaces;

            if (DateTime.TryParse(trimmed, Invariant, styles, out DateTime value))
            {
                return value;
            }

            // Fall back to the XML-specific parser, which accepts forms DateTime.TryParse rejects
            // such as a bare "12:30:00" time or a year-only date.
            try
            {
                return XmlConvert.ToDateTime(trimmed, XmlDateTimeSerializationMode.Utc);
            }
            catch (FormatException)
            {
                return default(DateTime);
            }
            catch (ArgumentOutOfRangeException)
            {
                return default(DateTime);
            }
        }

        public static byte[] ToByteArray(string text)
        {
            string trimmed = Trim(text);
            if (trimmed.Length == 0) return new byte[0];
            try
            {
                return Convert.FromBase64String(trimmed);
            }
            catch (FormatException)
            {
                return new byte[0];
            }
        }

        public static byte[] FromHexString(string text)
        {
            string trimmed = Trim(text);
            if (trimmed.Length == 0 || (trimmed.Length % 2) != 0) return new byte[0];

            byte[] bytes = new byte[trimmed.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                int high = FromHexDigit(trimmed[i * 2]);
                int low = FromHexDigit(trimmed[i * 2 + 1]);
                if (high < 0 || low < 0) return new byte[0];
                bytes[i] = (byte)((high << 4) | low);
            }
            return bytes;
        }

        private static int FromHexDigit(char c)
        {
            if (c >= '0' && c <= '9') return c - '0';
            if (c >= 'a' && c <= 'f') return c - 'a' + 10;
            if (c >= 'A' && c <= 'F') return c - 'A' + 10;
            return -1;
        }

        private static string Trim(string text) => text == null ? string.Empty : text.Trim();
    }
}
