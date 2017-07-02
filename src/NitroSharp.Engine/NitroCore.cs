using System;
using NitroSharp.NsScript.Execution;
using NitroSharp.NsScript;
using NitroSharp.Foundation.Content;
using NitroSharp.Foundation;
using NitroSharp.Graphics;
using System.Linq;

namespace NitroSharp
{
    public sealed partial class NitroCore : BuiltInFunctionsBase
    {
        private ContentManager _content;
        private readonly EntityManager _entities;

        private readonly NitroGame _game;

        public NitroCore(Game game, NitroConfiguration configuration, EntityManager entities)
        {
            _game = game as NitroGame;
            _entities = entities;
            _viewport = new System.Drawing.Size(configuration.WindowWidth, configuration.WindowHeight);
        }

        public void SetContent(ContentManager content) => _content = content;

        public override void SetAlias(string entityName, string alias)
        {
            if (entityName != alias && _entities.TryGet(entityName, out var entity))
            {
                entity.SetAlias(alias);
            }
        }

        public override void RemoveEntity(string entityName)
        {
            foreach (var entity in _entities.Query(entityName))
            {
                if (!entity.IsLocked())
                {
                    _entities.Remove(entity);
                    var attachedThread = Interpreter.Threads.FirstOrDefault(x => entityName.StartsWith(x.Name));
                    attachedThread?.Terminate();
                }
            }
        }

        public override void Delay(TimeSpan delay)
        {
            CurrentThread.Suspend(delay);
        }

        public override void WaitForInput()
        {
            CurrentThread.Suspend();
        }

        public override void WaitForInput(TimeSpan timeout)
        {
            CurrentThread.Suspend(timeout);
        }

        public override void Request(string entityName, NsEntityAction action)
        {
            foreach (var e in _entities.Query(entityName))
            {
                RequestCore(e, action);
            }
        }

        private void RequestCore(Entity entity, NsEntityAction action)
        {
            switch (action)
            {
                case NsEntityAction.Lock:
                    entity.Lock();
                    break;

                case NsEntityAction.Unlock:
                    entity.Unlock();
                    break;

                case NsEntityAction.ResetText:
                    TextEntity.Destroy();
                    break;

                case NsEntityAction.Hide:
                    var visual = entity.GetComponent<Visual>();
                    if (visual != null)
                    {
                        //visual.IsEnabled = false;
                    }
                    break;

                case NsEntityAction.Dispose:
                    //entity.Destroy();
                    break;
            }
        }
    }
}
