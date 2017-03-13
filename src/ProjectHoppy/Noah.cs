using ProjectHoppy.Content;
using ProjectHoppy.Graphics;
using SciAdvNet.NSScript.Execution;
using System;
using System.IO;

namespace ProjectHoppy
{
    public class Noah : Game
    {
        private ZipContentManager _content;
        private readonly NSScriptInterpreter _nssInterpreter;
        private readonly HoppyNssImplementation _nssBuiltIns;

        public Noah()
        {
            Window.Title = "Chaos;Hoppy";

            _content = new ZipContentManager("S:/ProjectHoppy/Content.zip");
            _nssBuiltIns = new HoppyNssImplementation(Entities);
            _nssInterpreter = new NSScriptInterpreter(new ScriptLocator(), _nssBuiltIns);

            var renderSystem = new RenderSystem(RenderContext, _content);
            Systems.RegisterSystem(renderSystem);
        }

        //public override ContentManager CreateContentManager()
        //{
        //    return new ZipContentManager(RenderContext.ResourceFactory, "S:/ProjectHoppy/Content.zip");
        //}

        //public override void Run()
        //{
        //    _nssInterpreter.CreateMicrothread("nss/boot-logo");

        //    EnterLoop();

        //    //_nssInterpreter.CreateMicrothread("nss/ch01_007_円山町殺人現場");
        //    //while (_nssInterpreter.Status != NSScriptInterpreterStatus.Idle)
        //    //{
        //    //    _nssInterpreter.Run(HaltCondition.PendingBuiltInCall);

        //    //    LogBuiltInCalls();
        //    //    _nssInterpreter.DispatchPendingBuiltInCalls();
        //    //}
        //}

        public override void Update(float deltaMilliseconds)
        {
            _nssInterpreter.Run(HaltCondition.PendingBuiltInCall);
            LogBuiltInCalls();
            _nssInterpreter.DispatchPendingBuiltInCalls();
            base.Update(deltaMilliseconds);
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