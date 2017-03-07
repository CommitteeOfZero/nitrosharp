using ProjectHoppy.Content;
using SciAdvNet.NSScript.Execution;
using System;
using System.IO;

namespace ProjectHoppy
{
    public class Noah : Game
    {
        public override ContentManager Content { get; }
        private readonly NSScriptInterpreter _nssInterpreter;
        private readonly HoppyNssImplementation _nssBuiltIns;

        public Noah()
        {
            Window.Title = "Chaos;Hoppy";

            Content = new ZipContentManager(this, "S:/ProjectHoppy/Content.zip");
            _nssBuiltIns = new HoppyNssImplementation(this);
            _nssInterpreter = new NSScriptInterpreter(new ScriptLocator(), _nssBuiltIns);
        }

        public override void Run()
        {
            //_nssInterpreter.CreateMicrothread("nss/boot-logo");
            _nssInterpreter.CreateMicrothread("nss/ch01_007_円山町殺人現場");
            while (_nssInterpreter.Status != NSScriptInterpreterStatus.Idle)
            {
                _nssInterpreter.Run(HaltCondition.PendingBuiltInCall);

                LogBuiltInCalls();
                _nssInterpreter.DispatchPendingBuiltInCalls();
            }

            Interact(TimeSpan.MaxValue);
            Graphics.Dispose();
        }

        private void LogBuiltInCalls()
        {
#if DEBUG
            foreach (var call in _nssInterpreter.PendingBuiltInCalls)
            {
                Console.WriteLine($"Built-in call: {call}");
            }
#endif
        }

        private class ScriptLocator : IScriptLocator
        {
            public Stream Locate(string fileName)
            {
                return File.OpenRead("S:/ProjectHoppy/" + fileName);
            }
        }
    }
}
