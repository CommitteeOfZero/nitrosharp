using System.IO;
using System.Reflection;

namespace NitroSharp.Launcher
{
    public class GameLauncher
    {
        public static void Launch(string configFilePath)
        {
            // Workaround for https://github.com/dotnet/project-system/issues/589
            string gameRootDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            Directory.SetCurrentDirectory(gameRootDir);

            var config = ConfigurationReader.Read(configFilePath);
            var game = new Game(config);
            game.Run();
        }
    }
}
