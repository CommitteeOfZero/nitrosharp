using CommitteeOfZero.NitroSharp;
using System;
using System.IO;
using System.Linq;

namespace CowsHead
{
    public class Program
    {
        public static void Main(string[] args)
        {
#if NETCOREAPP2_0
            var currentDirInfo = new DirectoryInfo(Directory.GetCurrentDirectory());
            var gameRootDir = currentDirInfo.EnumerateFiles("game.json", SearchOption.AllDirectories).First().Directory;
            Directory.SetCurrentDirectory(gameRootDir.FullName);
#endif
            FFmpegLibraries.Locate();

            var config = ConfigurationReader.Read("Game.json");
            var game = new NitroGame(config);
            game.Run();
        }
    }
}
