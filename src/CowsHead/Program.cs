using CommitteeOfZero.Nitro;
using System;

namespace CowsHead
{
    public class Program
    {
        private const string ConfigFileName = "CowsHead.json";

        public static void Main(string[] args)
        {
            var config = ConfigurationReader.Read(ConfigFileName);

            var game = new NitroGame(config);
            game.Run();
        }
    }
}
