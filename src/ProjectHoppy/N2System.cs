using System;
using SciAdvNet.NSScript.Execution;
using System.Collections.Generic;
using ProjectHoppy.Graphics;
using ProjectHoppy.Text;
using SciAdvNet.NSScript.PXml;
using SciAdvNet.NSScript;
using HoppyFramework.Content;
using HoppyFramework;
using System.Linq;

namespace ProjectHoppy
{
    public class N2System : NssBuiltInFunctions
    {
        private ContentManager _content;
        private readonly EntityManager _entities;

        public N2System(EntityManager entities)
        {
            _entities = entities;
        }

        public void SetContent(ContentManager content) => _content = content;

        public NSScriptInterpreter Interpreter { get; internal set; }

        public override void AddRectangle(string entityName, int priority, NssCoordinate x, NssCoordinate y, int width, int height, NssColor color)
        {
            _entities.Create(entityName, replace: true)
                .WithComponent(new VisualComponent(VisualKind.Rectangle, x.Value, y.Value, width, height, priority) { Color = RgbaValueF.Black });
        }

        public override void AddTexture(string entityName, int priority, NssCoordinate x, NssCoordinate y, string fileOrEntityName)
        {
            var entity =_entities.Create(entityName)
                .WithComponent(new VisualComponent(VisualKind.Texture, x.Value, y.Value, 0, 0, priority));

            if (fileOrEntityName != "SCREEN")
            {
                entity.AddComponent(new TextureComponent { AssetRef = fileOrEntityName });
            }
            else
            {
                entity.GetComponent<VisualComponent>().Kind = VisualKind.Screenshot;
                return;
            }

            _content.StartLoading<TextureAsset>(fileOrEntityName);
        }

        public override void LoadAudio(string entityName, AudioKind kind, string fileName)
        {
            _entities.Create(entityName, replace: true)
                .WithComponent(new SoundComponent { AudioFile = fileName });
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
            //if (entityName == "黒")
            //{
            //    duration += TimeSpan.FromSeconds(5);
            //}

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
                        PropertyGetter = c => (c as VisualComponent).Opacity,
                        PropertySetter = (c, v) => (c as VisualComponent).Opacity = v,
                        Duration = duration,
                        InitialValue = visual.Opacity,
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

        public override void DrawTransition(string entityName, TimeSpan duration, int initialOpacity, int finalOpacity, int boundary, string fileName, bool wait)
        {
            if (entityName.Length > 0 && entityName[0] == '@')
            {
                entityName = entityName.Substring(1);
            }

            foreach (var entity in _entities.WildcardQuery(entityName))
            {
                var originalTexture = entity.GetComponent<TextureComponent>();
                var transition = new DissolveTransition
                {
                    Texture = originalTexture.AssetRef,
                    AlphaMask = fileName
                };

                _content.StartLoading<TextureAsset>(fileName);

                var visual = entity.GetComponent<VisualComponent>();
                visual.Kind = VisualKind.DissolveTransition;
                entity.RemoveComponent(originalTexture);
                entity.AddComponent(transition);

                var animation = new FloatAnimation
                {
                    TargetComponent = transition,
                    PropertyGetter = c => (c as DissolveTransition).Opacity,
                    PropertySetter = (c, v) => (c as DissolveTransition).Opacity = v,
                    InitialValue = 0.0f,
                    FinalValue = 1.0f,
                    Duration = duration
                };

                entity.AddComponent(animation);
            }

            Interpreter.SuspendThread(CallingThreadId, duration);
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
            if (entityName == null)
                return;

            if (entityName.Length > 0 && entityName[0] == '@')
            {
                entityName = entityName.Substring(1);
            }

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
            if (entity != null)
            {
                entity.GetComponent<SoundComponent>().Volume = volume;
            }
        }

        public override void ToggleLooping(string entityName, bool looping)
        {
            if (entityName.Length > 0 && entityName[0] == '@')
            {
                entityName = entityName.Substring(1);
            }

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
            if (entity != null)
            {
                entity.GetComponent<SoundComponent>().Looping = looping;
            }
        }

        public override void SetLoopPoint(string entityName, TimeSpan loopStart, TimeSpan loopEnd)
        {
            if (entityName.Length > 0 && entityName[0] == '@')
            {
                entityName = entityName.Substring(1);
            }

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
            if (entity != null)
            {
                var sound = entity.GetComponent<SoundComponent>();
                sound.LoopStart = loopEnd;
                sound.LoopEnd = loopEnd;
                sound.Looping = true;
            }
        }

        public override void WaitText(string id, TimeSpan time)
        {

        }

        public override void DisplayDialogue(string pxmlString)
        {
            Interpreter.SuspendThread(CallingThreadId);

            //var visual = new VisualComponent { Kind = VisualKind.Text, X = 40, Y = 470 + 5, Width = 800 - 80, Height = 130, Priority = 30000, Color = RgbaValueF.White };

            var root = PXmlBlock.Parse(pxmlString);
            var flattener = new PXmlTreeFlattener();

            var text = flattener.Flatten(root);
            text.X = 40;
            text.Y = 475;
            text.Width = 800 - 80;
            text.Height = 130;
            text.Priority = 30000;
            text.Color = RgbaValueF.White;


            //text.Animated = true;

            _entities.Create("text", replace: true)
                //.WithComponent(visual)
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
