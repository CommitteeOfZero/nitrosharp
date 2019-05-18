// (C) Richard Prinz
// Licensed under The MIT License.
// Contains modifications by @SomeAnonDev.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace NitroSharp.Utilities
{
    public static class CRuntime
    {
        private static bool IsNumericType(object o)
        {
            return (o is byte ||
                o is sbyte ||
                o is short ||
                o is ushort ||
                o is int ||
                o is uint ||
                o is long ||
                o is ulong ||
                o is float ||
                o is double ||
                o is decimal);
        }

        private static bool IsPositive(object value, bool zeroIsPositive)
        {
            switch (Type.GetTypeCode(value.GetType()))
            {
                case TypeCode.SByte:
                    return (zeroIsPositive ? (sbyte)value >= 0 : (sbyte)value > 0);
                case TypeCode.Int16:
                    return (zeroIsPositive ? (short)value >= 0 : (short)value > 0);
                case TypeCode.Int32:
                    return (zeroIsPositive ? (int)value >= 0 : (int)value > 0);
                case TypeCode.Int64:
                    return (zeroIsPositive ? (long)value >= 0 : (long)value > 0);
                case TypeCode.Single:
                    return (zeroIsPositive ? (float)value >= 0 : (float)value > 0);
                case TypeCode.Double:
                    return (zeroIsPositive ? (double)value >= 0 : (double)value > 0);
                case TypeCode.Decimal:
                    return (zeroIsPositive ? (decimal)value >= 0 : (decimal)value > 0);
                case TypeCode.Byte:
                    return (zeroIsPositive ? true : (byte)value > 0);
                case TypeCode.UInt16:
                    return (zeroIsPositive ? true : (ushort)value > 0);
                case TypeCode.UInt32:
                    return (zeroIsPositive ? true : (uint)value > 0);
                case TypeCode.UInt64:
                    return (zeroIsPositive ? true : (ulong)value > 0);
                case TypeCode.Char:
                    return (zeroIsPositive ? true : (char)value != '\0');
                default:
                    return false;
            }
        }

        private static object ToUnsigned(object value)
        {
            switch (Type.GetTypeCode(value.GetType()))
            {
                case TypeCode.SByte:
                    return (byte)((sbyte)value);
                case TypeCode.Int16:
                    return (ushort)((short)value);
                case TypeCode.Int32:
                    return (uint)((int)value);
                case TypeCode.Int64:
                    return (ulong)((long)value);

                case TypeCode.Byte:
                    return value;
                case TypeCode.UInt16:
                    return value;
                case TypeCode.UInt32:
                    return value;
                case TypeCode.UInt64:
                    return value;

                case TypeCode.Single:
                    return (uint)((float)value);
                case TypeCode.Double:
                    return (ulong)((double)value);
                case TypeCode.Decimal:
                    return (ulong)((decimal)value);

                default:
                    return null;
            }
        }

        private static long UnboxToLong(object value, bool round)
        {
            switch (Type.GetTypeCode(value.GetType()))
            {
                case TypeCode.SByte:
                    return (sbyte)value;
                case TypeCode.Int16:
                    return (short)value;
                case TypeCode.Int32:
                    return (int)value;
                case TypeCode.Int64:
                    return (long)value;

                case TypeCode.Byte:
                    return (byte)value;
                case TypeCode.UInt16:
                    return (ushort)value;
                case TypeCode.UInt32:
                    return (uint)value;
                case TypeCode.UInt64:
                    return (long)((ulong)value);

                case TypeCode.Single:
                    return (round ? (long)Math.Round((float)value) : (long)((float)value));
                case TypeCode.Double:
                    return (round ? (long)Math.Round((double)value) : (long)((double)value));
                case TypeCode.Decimal:
                    return (round ? (long)Math.Round((decimal)value) : (long)((decimal)value));

                default:
                    return 0;
            }
        }

        public static void printf(string format, params object[] args)
        {
            Console.Write(sprintf(format, args));
        }

        public static void fprintf(TextWriter destination, string format, params object[] args)
        {
            destination.Write(sprintf(format, args));
        }

        public static string sprintf(string format, params object[] args)
        {
            var f = new StringBuilder();
            var r = new Regex(@"\%([\'\#\-\+ ]*)(\d*)(?:\.(\d+))?([hl])?([dioxXucsfeEgGpn%])");

            // find all format parameters in format string
            f.Append(format);
            Match m = r.Match(f.ToString());
            while (m.Success)
            {
                // extract format flags
                bool flagAlternate = false;
                bool flagLeft2Right = false;
                bool flagPositiveSign = false;
                bool flagPositiveSpace = false;
                bool flagZeroPadding = false;
                bool flagGroupThousands = false;
                if (m.Groups[1] != null && m.Groups[1].Value.Length > 0)
                {
                    string flags = m.Groups[1].Value;

                    flagAlternate = (flags.IndexOf('#') >= 0);
                    flagLeft2Right = (flags.IndexOf('-') >= 0);
                    flagPositiveSign = (flags.IndexOf('+') >= 0);
                    flagPositiveSpace = (flags.IndexOf(' ') >= 0);
                    flagGroupThousands = (flags.IndexOf('\'') >= 0);

                    // positive + indicator overrides a
                    // positive space character
                    if (flagPositiveSign && flagPositiveSpace)
                    {
                        flagPositiveSpace = false;
                    }
                }

                // extract field length and 
                // pading character
                char paddingCharacter = ' ';
                int fieldLength = int.MinValue;
                if (m.Groups[2] != null && m.Groups[2].Value.Length > 0)
                {
                    fieldLength = Convert.ToInt32(m.Groups[2].Value);
                    flagZeroPadding = (m.Groups[2].Value[0] == '0');
                }

                if (flagZeroPadding)
                {
                    paddingCharacter = '0';
                }

                // left2right allignment overrides zero padding
                if (flagLeft2Right && flagZeroPadding)
                {
                    flagZeroPadding = false;
                    paddingCharacter = ' ';
                }

                // extract field precision
                int fieldPrecision = int.MinValue;
                if (m.Groups[3] != null && m.Groups[3].Value.Length > 0)
                {
                    fieldPrecision = Convert.ToInt32(m.Groups[3].Value);
                }

                // extract short / long indicator
                char shortLongIndicator = char.MinValue;
                if (m.Groups[4] != null && m.Groups[4].Value.Length > 0)
                {
                    shortLongIndicator = m.Groups[4].Value[0];
                }

                // extract format
                char formatSpecifier = char.MinValue;
                if (m.Groups[5] != null && m.Groups[5].Value.Length > 0)
                {
                    formatSpecifier = m.Groups[5].Value[0];
                }

                // default precision is 6 digits if none is specified
                if (fieldPrecision == int.MinValue && formatSpecifier != 's' && formatSpecifier != 'c')
                {
                    fieldPrecision = 6;
                }

                long i = 0;
                object o;
                // get next value parameter and convert value parameter depending on short / long indicator
                if (args == null || i >= args.Length)
                {
                    o = null;
                }
                else
                {
                    o = args[i];

                    if (shortLongIndicator == 'h')
                    {
                        if (o is int)
                        {
                            o = (short)((int)o);
                        }
                        else if (o is long)
                        {
                            o = (short)((long)o);
                        }
                        else if (o is uint)
                        {
                            o = (ushort)((uint)o);
                        }
                        else if (o is ulong)
                        {
                            o = (ushort)((ulong)o);
                        }
                    }
                    else if (shortLongIndicator == 'l')
                    {
                        if (o is short)
                        {
                            o = (long)((short)o);
                        }
                        else if (o is int)
                        {
                            o = (long)((int)o);
                        }
                        else if (o is ushort)
                        {
                            o = (ulong)((ushort)o);
                        }
                        else if (o is uint)
                        {
                            o = (ulong)((uint)o);
                        }
                    }
                }

                // convert value parameters to a string depending on the formatSpecifier
                string w = string.Empty;
                switch (formatSpecifier)
                {
                    case '%':   // % character
                        w = "%";
                        break;
                    case 'd':   // integer
                        w = FormatNumber((flagGroupThousands ? "n" : "d"), flagAlternate,
                                        fieldLength, int.MinValue, flagLeft2Right,
                                        flagPositiveSign, flagPositiveSpace,
                                        paddingCharacter, o);
                        i++;
                        break;
                    case 'i':   // integer
                        goto case 'd';
                    case 'o':   // octal integer - no leading zero
                        w = FormatOct("o", flagAlternate,
                                        fieldLength, int.MinValue, flagLeft2Right,
                                        paddingCharacter, o);
                        i++;
                        break;
                    case 'x':   // hex integer - no leading zero
                        w = FormatHex("x", flagAlternate,
                                        fieldLength, int.MinValue, flagLeft2Right,
                                        paddingCharacter, o);
                        i++;
                        break;
                    case 'X':   // same as x but with capital hex characters
                        w = FormatHex("X", flagAlternate,
                                        fieldLength, int.MinValue, flagLeft2Right,
                                        paddingCharacter, o);
                        i++;
                        break;
                    case 'u':   // unsigned integer
                        w = FormatNumber((flagGroupThousands ? "n" : "d"), flagAlternate,
                                        fieldLength, int.MinValue, flagLeft2Right,
                                        false, false,
                                        paddingCharacter, ToUnsigned(o));
                        i++;
                        break;
                    case 'c':   // character
                        if (IsNumericType(o))
                        {
                            w = Convert.ToChar(o).ToString();
                        }
                        else if (o is char)
                        {
                            w = ((char)o).ToString();
                        }
                        else if (o is string && ((string)o).Length > 0)
                        {
                            w = ((string)o)[0].ToString();
                        }

                        i++;
                        break;
                    case 's':   // string
                        string t = "{0" + (fieldLength != int.MinValue
                            ? "," + (flagLeft2Right ? "-" : string.Empty) + fieldLength.ToString()
                            : string.Empty) + ":s}";

                        w = o.ToString();
                        if (fieldPrecision >= 0)
                        {
                            w = w.Substring(0, fieldPrecision);
                        }

                        if (fieldLength != int.MinValue)
                        {
                            if (flagLeft2Right)
                            {
                                w = w.PadRight(fieldLength, paddingCharacter);
                            }
                            else
                            {
                                w = w.PadLeft(fieldLength, paddingCharacter);
                            }
                        }

                        i++;
                        break;
                    case 'f':   // double
                        w = FormatNumber((flagGroupThousands ? "n" : "f"), flagAlternate,
                                        fieldLength, fieldPrecision, flagLeft2Right,
                                        flagPositiveSign, flagPositiveSpace,
                                        paddingCharacter, o);
                        i++;
                        break;
                    case 'e':   // double / exponent
                        w = FormatNumber("e", flagAlternate,
                                        fieldLength, fieldPrecision, flagLeft2Right,
                                        flagPositiveSign, flagPositiveSpace,
                                        paddingCharacter, o);
                        i++;
                        break;
                    case 'E':   // double / exponent
                        w = FormatNumber("E", flagAlternate,
                                        fieldLength, fieldPrecision, flagLeft2Right,
                                        flagPositiveSign, flagPositiveSpace,
                                        paddingCharacter, o);
                        i++;
                        break;
                    case 'g':   // double / exponent
                        w = FormatNumber("g", flagAlternate,
                                        fieldLength, fieldPrecision, flagLeft2Right,
                                        flagPositiveSign, flagPositiveSpace,
                                        paddingCharacter, o);
                        i++;
                        break;
                    case 'G':   // double / exponent
                        w = FormatNumber("G", flagAlternate,
                                        fieldLength, fieldPrecision, flagLeft2Right,
                                        flagPositiveSign, flagPositiveSpace,
                                        paddingCharacter, o);
                        i++;
                        break;
                    case 'p':   // pointer
                        if (o is IntPtr)
                        {
                            w = "0x" + ((IntPtr)o).ToString("x");
                        }

                        i++;
                        break;
                    case 'n':   // number of characters so far
                        w = FormatNumber("d", flagAlternate,
                                        fieldLength, int.MinValue, flagLeft2Right,
                                        flagPositiveSign, flagPositiveSpace,
                                        paddingCharacter, m.Index);
                        break;
                    default:
                        w = string.Empty;
                        i++;
                        break;
                }

                // replace format parameter with parameter value
                // and start searching for the next format parameter
                // AFTER the position of the current inserted value
                // to prohibit recursive matches if the value also
                // includes a format specifier
                f.Remove(m.Index, m.Length);
                f.Insert(m.Index, w);
                m = r.Match(f.ToString(), m.Index + w.Length);
            }

            return f.ToString();
        }

        private static string FormatOct(string nativeFormat, bool alternate,
                                            int fieldLength, int fieldPrecision,
                                            bool left2Right,
                                            char padding, object value)
        {
            string w = string.Empty;
            string lengthFormat = "{0" + (fieldLength != int.MinValue ?
                                            "," + (left2Right ?
                                                    "-" :
                                                    string.Empty) + fieldLength.ToString() :
                                            string.Empty) + "}";

            if (IsNumericType(value))
            {
                w = Convert.ToString(UnboxToLong(value, true), 8);

                if (left2Right || padding == ' ')
                {
                    if (alternate && w != "0")
                    {
                        w = "0" + w;
                    }

                    w = string.Format(lengthFormat, w);
                }
                else
                {
                    if (fieldLength != int.MinValue)
                    {
                        w = w.PadLeft(fieldLength - (alternate && w != "0" ? 1 : 0), padding);
                    }

                    if (alternate && w != "0")
                    {
                        w = "0" + w;
                    }
                }
            }

            return w;
        }

        private static string FormatHex(string nativeFormat, bool alternate,
                                            int fieldLength, int fieldPrecision,
                                            bool left2Right,
                                            char padding, object value)
        {
            string w = string.Empty;
            string lengthFormat = "{0" + (fieldLength != int.MinValue ?
                                            "," + (left2Right ?
                                                    "-" :
                                                    string.Empty) + fieldLength.ToString() :
                                            string.Empty) + "}";
            string numberFormat = "{0:" + nativeFormat + (fieldPrecision != int.MinValue ?
                                            fieldPrecision.ToString() :
                                            string.Empty) + "}";

            if (IsNumericType(value))
            {
                w = string.Format(numberFormat, value);

                if (left2Right || padding == ' ')
                {
                    if (alternate)
                    {
                        w = (nativeFormat == "x" ? "0x" : "0X") + w;
                    }

                    w = string.Format(lengthFormat, w);
                }
                else
                {
                    if (fieldLength != int.MinValue)
                    {
                        w = w.PadLeft(fieldLength - (alternate ? 2 : 0), padding);
                    }

                    if (alternate)
                    {
                        w = (nativeFormat == "x" ? "0x" : "0X") + w;
                    }
                }
            }

            return w;
        }

        private static string FormatNumber(string nativeFormat, bool alternate,
                                            int fieldLength, int fieldPrecision,
                                            bool left2Right,
                                            bool positiveSign, bool positiveSpace,
                                            char padding, object value)
        {
            string w = string.Empty;
            string lengthFormat = "{0" + (fieldLength != int.MinValue ?
                                            "," + (left2Right ?
                                                    "-" :
                                                    string.Empty) + fieldLength.ToString() :
                                            string.Empty) + "}";
            string numberFormat = "{0:" + nativeFormat + (fieldPrecision != int.MinValue ?
                                            fieldPrecision.ToString() :
                                            "0") + "}";

            if (IsNumericType(value))
            {
                w = string.Format(numberFormat, value);

                if (left2Right || padding == ' ')
                {
                    if (IsPositive(value, true))
                    {
                        w = (positiveSign ?
                                "+" : (positiveSpace ? " " : string.Empty)) + w;
                    }

                    w = string.Format(lengthFormat, w);
                }
                else
                {
                    if (w.StartsWith("-"))
                    {
                        w = w.Substring(1);
                    }

                    if (fieldLength != int.MinValue)
                    {
                        w = w.PadLeft(fieldLength - 1, padding);
                    }

                    if (IsPositive(value, true))
                    {
                        w = (positiveSign ?
                                "+" : (positiveSpace ?
                                        " " : (fieldLength != int.MinValue && w.Length < fieldLength ?
                                                padding.ToString() : string.Empty))) + w;
                    }
                    else
                    {
                        w = "-" + w;
                    }
                }
            }

            return w;
        }
    }
}
