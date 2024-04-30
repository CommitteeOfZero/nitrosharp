using System;
using System.Globalization;
using NitroSharp.NsScript.Utilities;
using ZeroLog;
using ZeroLog.Formatting;

namespace NitroSharp;

internal sealed class LogFormatter : Formatter
{
    protected override void WriteMessage(LoggedMessage message)
    {
        Write("[");
        Span<char> buffer = GetRemainingBuffer();
        DateTime timestamp = message.Timestamp.ToLocalTime();
        if (timestamp.TryFormat(buffer, out int charsWritten, "HH:mm:ss", CultureInfo.InvariantCulture))
        {
            AdvanceBy(charsWritten);
            Write(" ");
        }

        string level = message.Level switch
        {
            LogLevel.Trace => "TRC",
            LogLevel.Debug => "DBG",
            LogLevel.Info => "INF",
            LogLevel.Warn => "WRN",
            LogLevel.Error => "ERR",
            LogLevel.Fatal => "FATAL",
            LogLevel.None => "NONE",
            _ => throw new ArgumentOutOfRangeException()
        };

        Write(level);
        Write("] ");

        int indent = 12 + level.Length;
        bool isFirstLine = true;
        foreach (ReadOnlySpan<char> line in message.Message.Split('\n'))
        {
            if (isFirstLine)
            {
                Write(line);
                isFirstLine = false;
            }
            else
            {
                for (int i = 0; i < indent; i++)
                {
                    Write(" ");
                }
                Write(line);
            }

            WriteLine();
        }
    }
}
