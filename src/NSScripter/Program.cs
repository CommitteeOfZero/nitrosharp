using SciAdvNet.NSScript;
using System;
using System.IO;
using System.Linq;

namespace NSScripter
{
    class Program
    {
        static void Main(string[] args)
        {
            string noahFolder = "S:/HoppyContent/Noah/nss";
            foreach (string fileName in Directory.EnumerateFiles(noahFolder).Where(x => x.Contains("ch")))
            {
                using (var stream = File.OpenRead(fileName))
                {
                    try
                    {
                        var tree = NSScript.ParseScript(fileName, stream);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }
        }
    }
}