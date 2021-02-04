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
            await using (var game = new Game(window, config))
            {
                try
                {
                    await game.Run();
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
