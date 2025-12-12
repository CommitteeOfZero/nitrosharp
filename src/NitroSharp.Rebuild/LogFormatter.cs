using System;
using ZeroLog.Formatting;

namespace NitroSharp;

internal sealed class LogFormatter(string prefixPattern) : Formatter
{
    private readonly PatternWriter _prefixWriter = new(prefixPattern)
    {
        LogLevels = new PatternWriter.LogLevelNames("TRC", "DBG", "INF", "WRN", "ERR", "CRI")
    };

    protected override void WriteMessage(LoggedMessage message)
    {
        Write(message, _prefixWriter);

        int indent = GetOutput().Length;
        bool isFirstLine = true;
        ReadOnlySpan<char> messageText = message.Message;
        foreach (Range line in messageText.Split('\n'))
        {
            if (isFirstLine)
            {
                isFirstLine = false;
            }
            else
            {
                GetRemainingBuffer()[..indent].Fill(' ');
                AdvanceBy(indent);
            }

            Write(messageText[line]);
            WriteLine();
        }

        if (message.Exception is { } exception)
        {
            WriteLine(exception.ToString().AsSpan());
        }
    }
}
