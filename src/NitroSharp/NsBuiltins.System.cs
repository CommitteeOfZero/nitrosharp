using System;
using NitroSharp.NsScript;
using NitroSharp.NsScript.Execution;
using NitroSharp.Content;
using System.Collections.Generic;
using System.Linq;

namespace NitroSharp
{
    internal sealed partial class NsBuiltins : EngineImplementation
    {
        private World _world;
        private readonly Game _game;

        private readonly Queue<string> _entitiesToRemove = new Queue<string>();

        public NsBuiltins(Game game)
        {
            _game = game;
        }

        public void SetWorld(World gameWorld) => _world = gameWorld;

        private ContentManager Content => _game.Content;
        private bool IsAnimationInProgress => MainThread.SleepTimeout != TimeSpan.MaxValue;

        private void SuspendMainThread()
        {
            Interpreter.SuspendThread(MainThread);
        }

        private void ResumeMainThread()
        {
            Interpreter.ResumeThread(MainThread);
        }

        public override void SetAlias(string entityName, string alias)
        {
            if (entityName != alias)
            {
                _world.SetAlias(entityName, alias);
            }
        }

        public override void RemoveEntity(string entityName)
        {
            foreach ((Entity entity, string name) in _world.Query(entityName))
            {
                var table = _world.GetTable<EntityTable>(entity);
                if (!table.IsLocked.GetValue(entity))
                {
                    _entitiesToRemove.Enqueue(name);
                    var attachedThread = Interpreter.Threads.FirstOrDefault(x => entityName.StartsWith(x.Name));
                    if (attachedThread != null)
                    {
                        Interpreter.TerminateThread(attachedThread);
                    }
                }
            }

            while (_entitiesToRemove.Count > 0)
            {
                _world.RemoveEntity(_entitiesToRemove.Dequeue());
            }
        }

        public override void Delay(TimeSpan delay)
        {
            Interpreter.SuspendThread(CurrentThread, delay);
        }

        public override void WaitForInput()
        {
            if (_dialogueState.DialogueLine?.IsEmpty == true)
            {
                return;
            }

            Interpreter.SuspendThread(CurrentThread);
            _dialogueState.Clear = true;
        }

        public override void WaitForInput(TimeSpan timeout)
        {
            Interpreter.SuspendThread(CurrentThread, timeout);
        }

        public override void CreateThread(string name, string target)
        {
            bool startImmediately = _world.Query(name + "*").Any();
            Interpreter.CreateThread(name, target, startImmediately);
        }

        public override void Request(string entityName, NsEntityAction action)
        {
            foreach ((Entity entity, string name) in _world.Query(entityName))
            {
                RequestCore(entity, name, action);
            }
        }

        private void RequestCore(Entity entity, string entityName, NsEntityAction action)
        {
            EntityTable table = _world.GetTable<EntityTable>(entity);
            switch (action)
            {
                case NsEntityAction.Lock:
                    table.IsLocked.Set(entity, true);
                    break;
                case NsEntityAction.Unlock:
                    table.IsLocked.Set(entity, false);
                    break;

                case NsEntityAction.Start:
                    if (Interpreter.TryGetThread(entityName, out var thread))
                    {
                        Interpreter.ResumeThread(thread);
                    }
                    break;

                case NsEntityAction.Stop:
                    if (Interpreter.TryGetThread(entityName, out thread))
                    {
                        Interpreter.TerminateThread(thread);
                    }
                    break;
            }
        }
    }
}
