using System;
using ZeroLog;
using ZeroLog.Appenders;
using ZeroLog.Formatting;

namespace NitroSharp;

internal sealed class LogConsoleAppender : StreamAppender
{
    private LogLevel _lastLoggedLevel = LogLevel.None;

    public LogConsoleAppender()
    {
        Stream = Console.OpenStandardOutput();
        Encoding = Console.OutputEncoding;
    }

    public override void WriteMessage(LoggedMessage message)
    {
        UpdateConsoleColor(message);
        base.WriteMessage(message);
    }

    private void UpdateConsoleColor(LoggedMessage message)
    {
        if (message.Level == _lastLoggedLevel) { return; }

        if (_lastLoggedLevel != LogLevel.None)
        {
            Flush();
        }

        _lastLoggedLevel = message.Level;
        Console.ForegroundColor = message.Level switch
        {
            LogLevel.Debug or LogLevel.Trace => ConsoleColor.Gray,
            LogLevel.Info => ConsoleColor.Green,
            LogLevel.Warn => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
            LogLevel.Fatal => ConsoleColor.Magenta,
            _ => ConsoleColor.White
        };
    }
}
