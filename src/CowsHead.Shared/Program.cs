using CommitteeOfZero.Nitro;

namespace CowsHead
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var config = ConfigurationReader.Read("Game.json");
            var game = new NitroGame(config);
            game.Run();
        }
    }
}
