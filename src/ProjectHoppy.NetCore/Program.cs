using System;
using System.Threading.Tasks;

namespace ProjectHoppy.NetCore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //Task.Run(() =>
            //{
            //    var noah = new TypewriterTest();
            //    noah.Run();
            //}).Wait();
            var noah = new TypewriterTest();
            noah.Run().Wait();
        }
    }
}