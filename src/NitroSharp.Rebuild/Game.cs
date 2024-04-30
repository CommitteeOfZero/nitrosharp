using System.Threading.Tasks;
using ZeroLog;

namespace NitroSharp;

public static class Game
{
    public static Task Run(GameWindow window, Config config, GameProfile profile)
    {
        GameContext context = GameContext.Create(window, config, profile).Result;
        context.Run();
        LogManager.Shutdown();
        return Task.CompletedTask;
    }
}
