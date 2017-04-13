using CommitteeOfZero.Nitro;

namespace CowsHead
{
    public class Program
    {
        private const string ConfigFileName = "cowshead.json";

        public static void Main(string[] args)
        {
            var config = ConfigurationReader.Read(ConfigFileName);

            var noah = new NitroGame(config);
            noah.Run();
        }
    }
}