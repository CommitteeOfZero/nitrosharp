using System;
using System.Threading.Tasks;
using NitroSharp.Launcher;

namespace CowsHead
{
    class Program
    {
        static Task Main()
        {
            return GameLauncher.Launch("Game.json");
        }
    }
}
