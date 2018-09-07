using NitroSharp.NsScript.Symbols;
using System;

namespace NitroSharp.NsScript.Execution
{
    public class EngineImplementation : EngineImplementationBase
    {
        private readonly Random _randomGen = new Random();
        private DialogueBlockSymbol _currentDialogueBlock;

        public NsScriptInterpreter Interpreter { get; private set; }
        public ThreadContext MainThread { get; internal set; }
        public ThreadContext CurrentThread => Interpreter.CurrentThread;

        internal void SetInterpreter(NsScriptInterpreter instance) => Interpreter = instance;

        protected virtual void BeginDialogueBlock(DialogueBlockSymbol dialogueBlock)
        {
        }

        internal void NotifyDialogueBlockEntered(DialogueBlockSymbol dialogueBlock)
        {
            _currentDialogueBlock = dialogueBlock;
            BeginDialogueBlock(dialogueBlock);
        }

        public override int GetPlatformId() => 0;
        public override int GenerateRandomNumber(int max) => _randomGen.Next(max);

        public override void WaitText(string id, TimeSpan time)
        {
            CurrentThread.Call(_currentDialogueBlock);
        }
    }
}
