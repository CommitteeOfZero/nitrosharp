using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace NitroSharp.Launcher
{
    public class GameLauncher
    {
        public static async Task Launch(string configFilePath)
        {
            // Workaround for https://github.com/dotnet/project-system/issues/589
            //string gameRootDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            //Directory.SetCurrentDirectory(gameRootDir);

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
