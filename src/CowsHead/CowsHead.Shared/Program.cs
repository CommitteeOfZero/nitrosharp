using NitroSharp;
using System.IO;
using System.Reflection;

namespace CowsHead
{
    public class Program
    {
        public static void Main(string[] args)
        {
#if NETCOREAPP2_0
            string gameRootDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            Directory.SetCurrentDirectory(gameRootDir);
#endif
            FFmpegLibraries.Locate();

            var config = ConfigurationReader.Read("Game.json");
            var game = new NitroGame(config);
            game.Run();
        }
    }
}
