using System;
using SciAdvNet.NSScript.Execution;
using System.Collections.Generic;
using System.Linq;
using ProjectHoppy.Graphics;
using SciAdvNet.MediaLayer;
using System.Diagnostics;
using ProjectHoppy.Content;
using System.IO;
using SciAdvNet.MediaLayer.Graphics;

namespace ProjectHoppy
{
    public class N2SystemImplementation : NssBuiltInMethods
    {
        private readonly ContentManager _content;
        private readonly EntityManager _entities;

        public N2SystemImplementation(EntityManager entities, ContentManager content)
        {
            _entities = entities;
            _content = content;
        }

        public NSScriptInterpreter Interpreter { get; internal set; }
        public bool Waiting { get; private set; }

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
            
            foreach (var entity in _entities.PerformQuery(entityName.Replace("@", string.Empty)))
            {
                if (entity != null)
                {
                    if (duration > TimeSpan.Zero)
                    {
                        duration += TimeSpan.FromSeconds(1);
                        var animation = new FloatAnimation
                        {
                            PropertySetter = (e, v) => e.GetComponent<VisualComponent>().Opacity = v,
                            Duration = duration,
                            InitialValue = entity.GetComponent<VisualComponent>().Opacity,
                            CurrentValue = entity.GetComponent<VisualComponent>().Opacity,
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

            if (duration > TimeSpan.Zero)
            {
                Interpreter.SuspendThread(CallingThreadId, duration);
            }
        }

        public override void CreateDialogueBox(string entityName, int priority, NssCoordinate x, NssCoordinate y, int width, int height)
        {
            base.CreateDialogueBox(entityName, priority, x, y, width, height);
        }

        public override void Wait(TimeSpan delay)
        {
            Interpreter.SuspendThread(CallingThreadId, delay);
        }

        public override void WaitForInput(TimeSpan timeout)
        {
            Interpreter.SuspendThread(CallingThreadId, timeout);
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
            var visual = new VisualComponent { Kind = VisualKind.Text, X = 40, Y = 470 + 10, Width = 800 - 80, Height = 130, Priority = 30000, Color = RgbaValueF.White };
            var text = new TextComponent { Animated = true, Text = "I took a peek inside my bag. Several textbooks and a gaming - enabled cellphone were the only things inside." };

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
