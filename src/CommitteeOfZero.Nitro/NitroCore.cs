using System;
using CommitteeOfZero.NsScript.Execution;
using CommitteeOfZero.NsScript;
using MoeGame.Framework.Content;
using MoeGame.Framework;
using CommitteeOfZero.Nitro.Graphics.Visuals;

namespace CommitteeOfZero.Nitro
{
    public sealed partial class NitroCore : BuiltInFunctionsBase
    {
        private ContentManager _content;
        private readonly EntityManager _entities;

        private readonly Game _game;
        
        public NitroCore(Game game, NitroConfiguration configuration, EntityManager entities)
        {
            _game = game;
            _entities = entities;
            _viewport = new System.Drawing.Size(configuration.WindowWidth, configuration.WindowHeight);
            EnteredDialogueBlock += OnEnteredDialogueBlock;
        }

        public void SetContent(ContentManager content) => _content = content;

        public override void SetAlias(string entityName, string alias)
        {
            if (_entities.TryGet(entityName, out var entity))
            {
                _entities.Add(alias, entity);
            }
        }

        public override void RemoveEntity(string entityName)
        {
            foreach (var entity in _entities.Query(entityName))
            {
                if (!entity.IsLocked())
                {
                    _entities.Remove(entity);
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
            if (entityName == null)
                return;

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
                    entity.GetComponent<GameTextVisual>()?.Reset();
                    break;

                case NsEntityAction.Hide:
                    var visual = entity.GetComponent<Visual>();
                    if (visual != null)
                    {
                        //visual.IsEnabled = false;
                    }
                    break;

                case NsEntityAction.Dispose:
                    //_entities.Remove(entity);
                    break;
            }
        }
    }
}
