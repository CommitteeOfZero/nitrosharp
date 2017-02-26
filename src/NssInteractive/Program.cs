using SciAdvNet.NSScript;
using SciAdvNet.NSScript.Execution;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace NssInteractive
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //foreach (string file in Directory.EnumerateFiles("S:\\C;H\\func"))
            //{
            //    using (var stream = File.OpenRead(file))
            //    {
            //        NSScript.ParseScript(file, stream);
            //        Console.WriteLine($"{file} ok");
            //    }
            //    Console.ReadKey();
            //}

            var locator = new HoppyScriptLocator();
            var interpreter = new NSScriptInterpreter(locator, new HoppyBuiltIns());
            //ch01_007_円山町殺人現場
            //interpreter.Run("nss/ch01_007_円山町殺人現場");

            interpreter.CreateMicrothread("nss/ch01_023_１０月６日月");
            var calls = interpreter.PendingBuiltInCalls;

            foreach (var call in calls)
            {
                Console.WriteLine(call.MethodName + " " + string.Join(", ", call.Arguments));
            }
            Console.ReadKey();
        }
    }
}
