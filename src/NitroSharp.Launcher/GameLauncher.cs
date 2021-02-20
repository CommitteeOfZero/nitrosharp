using System;
using System.Threading.Tasks;

namespace NitroSharp.Launcher
{
    public static class GameLauncher
    {
        public static async Task Launch(string productName, string configFilePath)
        {
            Configuration config = ConfigurationReader.Read(configFilePath);
            config.ProductName = productName;
            var window = new DesktopWindow(config.WindowTitle, (uint)config.WindowWidth, (uint)config.WindowHeight);
            GameContext ctx = GameContext.Create(window, config).Result;
            {
                try
                {
                    await ctx.Run();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                }
            }
        }
    }
}
