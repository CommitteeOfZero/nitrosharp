using System;
using SciAdvNet.NSScript.Execution;
using System.Collections.Generic;
using ProjectHoppy.Graphics;
using SciAdvNet.MediaLayer;
using ProjectHoppy.Content;
using SciAdvNet.MediaLayer.Graphics;
using ProjectHoppy.Text;
using SciAdvNet.NSScript.PXml;
using SciAdvNet.NSScript;
using System.Threading.Tasks;

namespace ProjectHoppy
{
    public class N2System : NssBuiltInFunctions
    {
        private readonly ContentManager _content;
        private readonly EntityManager _entities;

        public N2System(EntityManager entities, ContentManager content)
        {
            _entities = entities;
            _content = content;
        }

        public NSScriptInterpreter Interpreter { get; internal set; }

        public override void AddRectangle(string entityName, int priority, NssCoordinate x, NssCoordinate y, int width, int height, NssColor color)
        {
            _entities.Create(entityName, replace: true)
                .WithComponent(new VisualComponent(VisualKind.Rectangle, x.Value, y.Value, width, height, priority) { Color = RgbaValueF.Black });
        }

        public override void AddTexture(string entityName, int priority, NssCoordinate x, NssCoordinate y, string fileOrEntityName)
        {
            _entities.Create(entityName)
                .WithComponent(new VisualComponent(VisualKind.Texture, x.Value, y.Value, 0, 0, priority))
                .WithComponent(new AssetComponent(fileOrEntityName));


            Task.Run(() => _content.Load<Texture2D>(fileOrEntityName));
        }

        public override void LoadAudio(string entityName, AudioKind kind, string fileName)
        {
            _entities.Create(entityName, replace: true)
                .WithComponent(new AssetComponent(fileName))
                .WithComponent(new SoundComponent());
        }

        private Queue<Entity> _entitiesToRemove = new Queue<Entity>();

        public override void RemoveEntity(string entityName)
        {
            if (entityName.Length > 0 && entityName[0] == '@')
            {
                entityName = entityName.Substring(1);
            }

            if (!IsWildcardQuery(entityName))
            {
                _entities.Remove(entityName);
                return;
            }

            foreach (var e in _entities.WildcardQuery(entityName))
            {
                if (!e.Locked)
                {
                    _entitiesToRemove.Enqueue(e);
                }
            }

            while (_entitiesToRemove.Count > 0)
            {
                _entities.Remove(_entitiesToRemove.Dequeue());
            }
        }

        private bool IsWildcardQuery(string s) => s[s.Length - 1] == '*';

        public override void Fade(string entityName, TimeSpan duration, int opacity, bool wait)
        {
            if (entityName.Length > 0 && entityName[0] == '@')
            {
                entityName = entityName.Substring(1);
            }

            if (IsWildcardQuery(entityName))
            {
                foreach (var entity in _entities.WildcardQuery(entityName))
                {
                    FadeCore(entity, duration, opacity, wait);
                }
            }
            else
            {
                var entity = _entities.SafeGet(entityName);
                FadeCore(entity, duration, opacity, wait);
            }

            if (duration > TimeSpan.Zero)
            {
                Interpreter.SuspendThread(CallingThreadId, duration);
            }
        }

        private void FadeCore(Entity entity, TimeSpan duration, int opacity, bool wait)
        {
            if (entity != null)
            {
                if (duration > TimeSpan.Zero)
                {
                    var visual = entity.GetComponent<VisualComponent>();
                    var animation = new FloatAnimation
                    {
                        TargetComponent = visual,
                        PropertySetter = (c, v) => (c as VisualComponent).Opacity = v,
                        Duration = duration,
                        InitialValue = visual.Opacity,
                        CurrentValue = visual.Opacity,
                        FinalValue = opacity / 1000.0f,
                    };

                    entity.AddComponent(animation);
                }
                else
                {
                    entity.GetComponent<VisualComponent>().Opacity = opacity / 1000.0f;
                }
            }
        }

        public override void CreateDialogueBox(string entityName, int priority, NssCoordinate x, NssCoordinate y, int width, int height)
        {
            base.CreateDialogueBox(entityName, priority, x, y, width, height);
        }

        public override void Delay(TimeSpan delay)
        {
            Interpreter.SuspendThread(CallingThreadId, delay);
        }

        public override void WaitForInput(TimeSpan timeout)
        {
            Interpreter.SuspendThread(CallingThreadId, timeout);
        }

        public override void SetVolume(string entityName, TimeSpan duration, int volume)
        {
            if (!IsWildcardQuery(entityName))
            {
                var entity = _entities.SafeGet(entityName);
                SetVolumeCore(entity, duration, volume);
                return;
            }

            foreach (var e in _entities.WildcardQuery(entityName))
            {
                SetVolumeCore(e, duration, volume);
            }
        }

        private void SetVolumeCore(Entity entity, TimeSpan duration, int volume)
        {
            entity.GetComponent<SoundComponent>().Volume = volume;
        }

        public override void ToggleLooping(string entityName, bool looping)
        {
            if (!IsWildcardQuery(entityName))
            {
                var entity = _entities.SafeGet(entityName);
                ToggleLoopingCore(entity, looping);
                return;
            }

            foreach (var e in _entities.WildcardQuery(entityName))
            {
                ToggleLoopingCore(e, looping);
            }
        }

        private void ToggleLoopingCore(Entity entity, bool looping)
        {
            entity.GetComponent<SoundComponent>().Looping = looping;
        }

        public override void SetLoopPoint(string entityName, TimeSpan loopStart, TimeSpan loopEnd)
        {
            if (!IsWildcardQuery(entityName))
            {
                var entity = _entities.SafeGet(entityName);
                SetLoopingCore(entity, loopStart, loopEnd);
                return;
            }

            foreach (var e in _entities.WildcardQuery(entityName))
            {
                SetLoopingCore(e, loopStart, loopEnd);
            }
        }

        private void SetLoopingCore(Entity entity, TimeSpan loopStart, TimeSpan loopEnd)
        {
            var sound = entity.GetComponent<SoundComponent>();
            sound.LoopStart = loopEnd;
            sound.LoopEnd = loopEnd;
            sound.Looping = true;
        }

        public override void WaitText(string id, TimeSpan time)
        {

        }

        public override void DisplayDialogue(string pxmlString)
        {
            Interpreter.SuspendThread(CallingThreadId);

            var visual = new VisualComponent { Kind = VisualKind.Text, X = 40, Y = 470 + 5, Width = 800 - 80, Height = 130, Priority = 30000, Color = RgbaValueF.White };

            var root = PXmlBlock.Parse(pxmlString);
            var flattener = new PXmlTreeFlattener();

            var text = flattener.Flatten(root);
            text.Animated = true;

            _entities.Create("text", replace: true)
                .WithComponent(visual)
                .WithComponent(text);
        }

        public override void Request(string entityName, NssEntityAction action)
        {
            if (entityName == null)
                return;

            if (entityName.Length > 0 && entityName[0] == '@')
            {
                entityName = entityName.Substring(1);
            }

            if (!IsWildcardQuery(entityName))
            {
                var entity = _entities.SafeGet(entityName);
                RequestCore(entity, action);
                return;
            }

            foreach (var e in _entities.WildcardQuery(entityName))
            {
                RequestCore(e, action);
            }
        }

        private void RequestCore(Entity entity, NssEntityAction action)
        {
            if (entity != null)
            {
                switch (action)
                {
                    case NssEntityAction.Lock:
                        entity.Locked = true;
                        break;

                    case NssEntityAction.Unlock:
                        entity.Locked = false;
                        break;
                }
            }
        }
    }
}
