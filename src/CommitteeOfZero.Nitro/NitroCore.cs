using System;
using CommitteeOfZero.NsScript.Execution;
using CommitteeOfZero.NsScript;
using CommitteeOfZero.Nitro.Foundation.Content;
using CommitteeOfZero.Nitro.Foundation;
using CommitteeOfZero.Nitro.Graphics;

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
                    break;

                case NsEntityAction.Hide:
                    var visual = entity.GetComponent<Visual>();
                    if (visual != null)
                    {
                        //visual.IsEnabled = false;
                    }
                    break;

                case NsEntityAction.Dispose:
                    if (entity.HasComponent<Visual>())
                    {
                        _entities.Remove(entity);
                    }
                    //else
                    //{
                    //    var sound = entity.GetComponent<SoundComponent>();
                    //    if (sound != null)
                    //    {
                    //        sound.RemoveOncePlayed = true;
                    //    }
                    //}
                    break;
            }
        }
    }
}
