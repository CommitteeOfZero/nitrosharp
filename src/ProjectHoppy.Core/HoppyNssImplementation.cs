using System;
using SciAdvNet.NSScript.Execution;
using System.Collections.Generic;
using System.Linq;
using ProjectHoppy.Core.Graphics;
using SciAdvNet.MediaLayer;
using System.Diagnostics;
using ProjectHoppy.Core.Content;
using System.IO;
using SciAdvNet.MediaLayer.Graphics;

namespace ProjectHoppy.Core
{
    public class HoppyNssImplementation : NssBuiltInMethods
    {
        private readonly ContentManager _content;
        private readonly EntityManager _entities;

        public HoppyNssImplementation(EntityManager entities, ContentManager content)
        {
            _entities = entities;
            _content = content;
        }

        public NSScriptInterpreter Interpreter { get; internal set; }
        public bool Waiting { get; private set; }

        public override void AddRectangle(string entityName, int zLevel, NssCoordinate x, NssCoordinate y, int width, int height, NssColor color)
        {
            _entities.Create(entityName, replace: true)
                .WithComponent(new VisualComponent(VisualKind.Rectangle, x.Value, y.Value, width, height, zLevel) { Color = RgbaValueF.Black });
        }

        public override void AddTexture(string entityName, int zLevel, NssCoordinate x, NssCoordinate y, string fileOrEntityName)
        {
            _entities.Create(entityName)
                .WithComponent(new VisualComponent(VisualKind.Texture, x.Value, y.Value, 0, 0, zLevel))
                .WithComponent(new AssetComponent(fileOrEntityName));


            _content.StartLoading<Texture2D>(fileOrEntityName);
        }

        public override void LoadAudio(string entityName, AudioKind kind, string fileName)
        {

            _entities.Create(entityName, replace: true)
                .WithComponent(new AssetComponent(fileName))
                .WithComponent(new SoundComponent());

            //if (kind == AudioKind.SoundEffect)
            //{
            //    var s = _entities.Get(entityName).GetComponent<SoundComponent>();
            //    s.Looping = true;
                
            //}
        }

        public override void RemoveEntity(string entityName)
        {
            foreach (var e in _entities.PerformQuery(entityName).Where(x => !x.Locked).ToArray())
            {
                Debug.WriteLine($"Removing entity '{e.Name}'");
                _entities.Remove(e);
            }
        }

        public override void Fade(string entityName, TimeSpan duration, int opacity, bool wait)
        {
            var entity = _entities.SafeGet(entityName.Replace("@", string.Empty));
            if (entity != null)
            {
                if (duration.TotalMilliseconds > 0)
                {
                    var animation = new FloatAnimation
                    {
                        PropertySetter = (e, v) => e.GetComponent<VisualComponent>().Opacity = v,
                        Duration = duration,
                        InitialValue = entity.GetComponent<VisualComponent>().Opacity,
                        FinalValue = opacity / 1000.0f,
                    };

                    entity.AddComponent(animation);
                    Interpreter.SuspendThread(CallingThreadId, duration);
                }
                else
                {
                    entity.GetComponent<VisualComponent>().Opacity = opacity / 1000.0f;
                }
            }
        }

        public override void CreateDialogueBox(string entityName, int zLevel, NssCoordinate x, NssCoordinate y, int width, int height)
        {
            base.CreateDialogueBox(entityName, zLevel, x, y, width, height);
        }

        public override void Wait(TimeSpan delay)
        {
            Interpreter.SuspendThread(CallingThreadId, delay);
        }

        public override void WaitForInput(TimeSpan timeout)
        {
        }

        public override void SetVolume(string entityName, TimeSpan duration, int volume)
        {
            foreach (var e in _entities.PerformQuery(entityName))
            {
                e.GetComponent<SoundComponent>().Volume = volume;

            }
            base.SetVolume(entityName, duration, volume);
        }

        public override void SetLoop(string entityName, bool looping)
        {
            foreach (var e in _entities.PerformQuery(entityName))
            {
                e.GetComponent<SoundComponent>().Looping = looping;
            }
        }

        public override void SetLoopPoint(string entityName, TimeSpan loopStart, TimeSpan loopEnd)
        {
            foreach (var e in _entities.PerformQuery(entityName))
            {
                var sound = e.GetComponent<SoundComponent>();
                sound.LoopStart = loopEnd;
                sound.LoopEnd = loopEnd;
                sound.Looping = true;
            }
            base.SetLoopPoint(entityName, loopStart, loopEnd);
        }

        public override void WaitText(string id, TimeSpan time)
        {
            var visual = new VisualComponent { Kind = VisualKind.Text, X = 40, Y = 470 + 10, Width = 800 - 80, Height = 130, LayerDepth = 30000, Color = RgbaValueF.White };
            var text = new TextComponent { Animated = true, Text = "According to all known laws of aviation, there is no way that a bee should be able to fly. Its wings are too small to get its fat little body off the ground. The bee, of course, flies anyway. Because bees don't care what humans think is impossible." };

            _entities.Create("text")
                .WithComponent(visual)
                .WithComponent(text);

            Interpreter.Suspend();
            Waiting = true;
        }

        public override void Request(string entityName, NssAction action)
        {
            if (entityName == null)
                return;

            foreach (var e in _entities.PerformQuery(entityName))
            {
                switch (action)
                {
                    case NssAction.Lock:
                        e.Locked = true;
                        break;

                    case NssAction.Unlock:
                        e.Locked = false;
                        break;
                }
            }
        }
    }
}
