using System;
using System.Threading.Tasks;
using ZeroLog;
using ZeroLog.Configuration;

namespace NitroSharp;

public static class Game
{
    public static Task Run(GameWindow window, Config config, GameProfile profile)
    {
        using (InitLogManager())
        {
            GameContext context = GameContext.Create(window, config, profile).Result;
            context.Run();
            return Task.CompletedTask;
        }
    }

    private static IDisposable InitLogManager()
    {
        const string prefixPattern = @"[%{localTime:hh\:mm\:ss} %{level}] %logger: ";
        var consoleAppender = new LogConsoleAppender { Formatter = new LogFormatter(prefixPattern) };
        return LogManager.Initialize(new ZeroLogConfiguration { RootLogger = { Appenders = { consoleAppender } } });
    }
}
