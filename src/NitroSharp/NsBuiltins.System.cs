using System;
using NitroSharp.NsScript;
using System.Collections.Generic;
using System.Linq;
using NitroSharp.Utilities;
using NitroSharp.NsScript.VM;
using NitroSharp.Content;
using System.Text;
using NitroSharp.Experimental;
using NitroSharp.Graphics;

namespace NitroSharp
{
    internal sealed partial class NsBuiltins : BuiltInFunctions
    {
        private World _world;
        private readonly Game _game;
        private readonly Logger _logger;
        private readonly Queue<EntityName> _entitiesToRemove = new Queue<EntityName>();
        private readonly Queue<Game.Message> _messageQueue = new Queue<Game.Message>();

        public NsBuiltins(Game game)
        {
            _game = game;
            _logger = game.Logger;
            _fontConfig = game.FontConfiguration;
        }

        private ContentManager Content => _game.Content;
        public Queue<Game.Message> MessagesForPresenter => _messageQueue;
        public string SelectedChoice { get; set; }

        public void SetWorld(World gameWorld) => _world = gameWorld;

        public override void SetAlias(string entityName, string alias)
        {
            if (entityName != alias)
            {
                _world.SetAlias(new EntityName(entityName), new EntityName(alias));
            }
        }

        public override void DestroyEntities(string entityName)
        {
            foreach ((Entity entity, EntityName name) in QueryEntities(entityName))
            {
                if (!_world.IsLocked(entity))
                {
                    _entitiesToRemove.Enqueue(name);
                    ThreadContext attachedThread = Interpreter.Threads
                        .FirstOrDefault(x => entityName.StartsWith(x.Name));
                    if (attachedThread != null)
                    {
                        Interpreter.TerminateThread(attachedThread);
                    }
                }
            }

            while (_entitiesToRemove.Count > 0)
            {
                _world.DestroyEntity(_entitiesToRemove.Dequeue());
            }
        }

        private readonly StringBuilder _logMessage = new StringBuilder();

        private EntityQueryResult QueryEntities(string query)
        {
            EntityQueryResult eqr = _world.Query(query);
            if (eqr.IsEmpty)
            {
                _logMessage.Clear();
                _logMessage.Append("Game object query yielded no results: ");
                _logMessage.Append("'");
                _logMessage.Append(eqr.Query);
                _logMessage.Append("'");
                _logger.LogWarning(_logMessage);
            }
            return eqr;
        }

        public override void Delay(TimeSpan delay)
        {
            Interpreter.SuspendThread(CurrentThread, delay);
        }

        public override void WaitForInput()
        {
            Interpreter.SuspendThread(CurrentThread);
        }

        public override void WaitForInput(TimeSpan timeout)
        {
            Interpreter.SuspendThread(CurrentThread, timeout);
        }

        public override void CreateThread(string name, string target)
        {
            bool startImmediately = _world.Query(name + "*").Any();
            ThreadContext thread = Interpreter.CreateThread(name, target, startImmediately);
            var info = new InterpreterThreadInfo(name, thread.EntryModule, target);
            _world.ThreadRecords.Uninitialized.New(new EntityName(name), info);
        }

        public override void Request(string entityName, NsEntityAction action)
        {
            foreach ((Entity entity, EntityName name) in QueryEntities(entityName))
            {
                RequestCore(entity, name, action);
            }
        }

        private void RequestCore(Entity entity, EntityName entityName, NsEntityAction action)
        {
            switch (action)
            {
                case NsEntityAction.Lock:
                    _world.LockEntity(entity);
                    break;
                case NsEntityAction.Unlock:
                    _world.UnlockEntity(entity);
                    break;

                case NsEntityAction.Start:
                    if (Interpreter.TryGetThread(entityName.Value, out ThreadContext thread))
                    {
                        Interpreter.ResumeThread(thread);
                    }
                    break;

                case NsEntityAction.Stop:
                    if (Interpreter.TryGetThread(entityName.Value, out thread))
                    {
                        Interpreter.TerminateThread(thread);
                    }
                    break;

                case NsEntityAction.SetAdditiveBlend:
                    setBlendMode(entity, BlendMode.Additive);
                    break;
                case NsEntityAction.SetReverseSubtractiveBlend:
                    setBlendMode(entity, BlendMode.ReverseSubtractive);
                    break;
                case NsEntityAction.SetMultiplicativeBlend:
                    setBlendMode(entity, BlendMode.Multiplicative);
                    break;

                case NsEntityAction.UseLinearFiltering:
                    var storage = _world.GetStorage<RenderItemStorage>(entity);
                    break;
            }

            void setBlendMode(Entity entity, BlendMode blendMode)
            {
                var storage = _world.GetStorage<QuadStorage>(entity);
                storage.Materials[entity].BlendMode = blendMode;
            }
        }

        public override ConstantValue FormatString(string format, object[] args)
        {
            string s = CRuntime.sprintf(format, args);
            return ConstantValue.String(s);
        }
    }
}
