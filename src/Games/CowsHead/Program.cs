using System;
using System.Threading.Tasks;
using NitroSharp.Launcher;

namespace CowsHead
{
    public class Program
    {
        public static Task Main(string[] args)
        {
            return GameLauncher.Launch("Game.json");
        }
    }
}
