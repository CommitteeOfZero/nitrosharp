using System;
using System.Threading.Tasks;

namespace NitroSharp.Launcher
{
    public class GameLauncher
    {
        public static async Task Launch(string configFilePath)
        {
            var config = ConfigurationReader.Read(configFilePath);
            var window = new DesktopWindow(config.WindowTitle, (uint)config.WindowWidth, (uint)config.WindowHeight);
            using (var game = new Game(window, config))
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
