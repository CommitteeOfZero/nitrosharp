using System;
using SciAdvNet.NSScript.Execution;
using System.Collections.Generic;
using ProjectHoppy.Graphics;
using ProjectHoppy.Text;
using SciAdvNet.NSScript.PXml;
using SciAdvNet.NSScript;
using HoppyFramework.Content;
using HoppyFramework;
using ProjectHoppy.Graphics.RenderItems;
using System.Numerics;

namespace ProjectHoppy
{
    public class N2System : NssImplementation
    {
        private System.Drawing.Size _viewport = new System.Drawing.Size(800, 600);
        private ContentManager _content;
        private readonly EntityManager _entities;

        private DialogueBox _currentDialogueBox;
        private Entity _textEntity;

        public N2System(EntityManager entities)
        {
            _entities = entities;
            EnteredDialogueBlock += OnEnteredDialogueBlock;
        }

        private Vector2 Position(NssCoordinate x, NssCoordinate y, Vector2 current, int width, int height)
        {
            float absoluteX = NssToAbsoluteCoordinate(x, current.X, width, _viewport.Width);
            float absoluteY = NssToAbsoluteCoordinate(y, current.Y, height, _viewport.Height);

            return new Vector2(absoluteX, absoluteY);
        }

        private void OnEnteredDialogueBlock(object sender, DialogueBlock block)
        {
            if (_textEntity != null)
            {
                _entities.Remove(_textEntity);
            }

            _currentDialogueBox = _entities.SafeGet(block.BoxName)?.GetComponent<DialogueBox>();
            var textVisual = new GameTextVisual
            {
                Position = _currentDialogueBox.Position,
                Width = _currentDialogueBox.Width,
                Height = _currentDialogueBox.Height,
                IsEnabled = false
            };

            _textEntity = _entities.Create(block.Identifier, replace: true).WithComponent(textVisual);
        }

        public override void DisplayDialogue(string pxmlString)
        {
            CurrentThread.Suspend();

            var root = PXmlBlock.Parse(pxmlString);
            var flattener = new PXmlTreeFlattener();

            string plainText = flattener.Flatten(root);

            var textVisual = _entities.SafeGet(CurrentDialogueBlock.Identifier)?.GetComponent<GameTextVisual>();
            if (textVisual != null)
            {
                textVisual.Reset();

                textVisual.Text = plainText;
                textVisual.Priority = 30000;
                textVisual.Color = RgbaValueF.White;
                textVisual.IsEnabled = true;
            }
        }

        public void SetContent(ContentManager content) => _content = content;

        public override void AddRectangle(string entityName, int priority, NssCoordinate x, NssCoordinate y, int width, int height, NssColor color)
        {
            var rect = new RectangleVisual
            {
                Position = Position(x, y, Vector2.Zero, width, height),
                Width = width,
                Height = height,
                Color = new RgbaValueF(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, 1.0f),
                Priority = priority
            };

            _entities.Create(entityName, replace: true).WithComponent(rect);
        }

        public override void AddTexture(string entityName, int priority, NssCoordinate x, NssCoordinate y, string fileOrEntityName)
        {
            int w = _viewport.Width, h = _viewport.Height;
            TextureAsset ass;
            try
            {
                ass = _content.Load<TextureAsset>(fileOrEntityName);
                w = (int)ass.Width;
                h = (int)ass.Height;
            }
            catch { }

            var position = Position(x, y, Vector2.Zero, w, h);
            if (fileOrEntityName != "SCREEN")
            {
                var visual = new TextureVisual
                {
                    Position = position,
                    Priority = priority,
                    AssetRef = fileOrEntityName,
                    Width = w,
                    Height = h
                };

                var entity = _entities.Create(entityName, replace: true).WithComponent(visual);
            }
            else
            {
                var screencap = new ScreenCap
                {
                    Position = position,
                    Priority = priority,
                };

                var entity = _entities.Create(entityName, replace: true).WithComponent(screencap);
            }
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

        public override void Fade(string entityName, TimeSpan duration, Rational opacity, bool wait)
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

            if (duration > TimeSpan.Zero && wait)
            {
                CurrentThread.Suspend(duration);
            }
        }

        private void FadeCore(Entity entity, TimeSpan duration, Rational opacity, bool wait)
        {
            if (entity != null)
            {
                float adjustedOpacity = opacity.Rebase(1.0f);
                if (duration > TimeSpan.Zero)
                {
                    var visual = entity.GetComponent<Visual>();
                    var animation = new FloatAnimation
                    {
                        TargetComponent = visual,
                        PropertyGetter = c => (c as Visual).Opacity,
                        PropertySetter = (c, v) => (c as Visual).Opacity = v,
                        Duration = duration,
                        InitialValue = visual.Opacity,
                        FinalValue = adjustedOpacity
                    };

                    entity.AddComponent(animation);
                }
                else
                {
                    entity.GetComponent<Visual>().Opacity = adjustedOpacity;
                }
            }
        }

        public override void DrawTransition(string entityName, TimeSpan duration, Rational initialOpacity,
            Rational finalOpacity, Rational feather, string fileName, bool wait)
        {
            if (entityName.Length > 0 && entityName[0] == '@')
            {
                entityName = entityName.Substring(1);
            }

            initialOpacity = initialOpacity.Rebase(1.0f);
            finalOpacity = finalOpacity.Rebase(1.0f);

            foreach (var entity in _entities.WildcardQuery(entityName))
            {
                var sourceVisual = entity.GetComponent<Visual>();
                var transition = new TransitionVisual
                {
                    Source = sourceVisual,
                    MaskAsset = fileName,
                    Priority = sourceVisual.Priority
                };

                _content.StartLoading<TextureAsset>(fileName);

                entity.RemoveComponent(sourceVisual);
                entity.AddComponent(transition);

                var animation = new FloatAnimation
                {
                    TargetComponent = transition,
                    PropertyGetter = c => (c as TransitionVisual).Opacity,
                    PropertySetter = (c, v) => (c as TransitionVisual).Opacity = v,
                    InitialValue = initialOpacity,
                    FinalValue = finalOpacity,
                    Duration = duration
                };

                entity.AddComponent(animation);
                animation.Completed += (o, e) =>
                {
                    entity.RemoveComponent(transition);
                    entity.AddComponent(sourceVisual);
                };
            }

            CurrentThread.Suspend(duration);
        }

        public override void CreateDialogueBox(string entityName, int priority, NssCoordinate x, NssCoordinate y, int width, int height)
        {
            var box = new DialogueBox
            {
                Position = Position(x, y, Vector2.Zero, width, height),
                Width = width,
                Height = height
            };

            _entities.Create(entityName).WithComponent(box);
        }

        public override void Delay(TimeSpan delay)
        {
            CurrentThread.Suspend(delay);
        }

        public override void WaitForInput(TimeSpan timeout)
        {
            CurrentThread.Suspend(timeout);
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

        public override void Move(string entityName, TimeSpan duration, NssCoordinate x, NssCoordinate y, EasingFunction easingFunction, bool wait)
        {
            if (entityName.Length > 0 && entityName[0] == '@')
            {
                entityName = entityName.Substring(1);
            }

            if (IsWildcardQuery(entityName))
            {
                foreach (var entity in _entities.WildcardQuery(entityName))
                {
                    MoveCore(entity, duration, x, y, easingFunction, wait);
                }
            }
            else
            {
                var entity = _entities.SafeGet(entityName);
                MoveCore(entity, duration, x, y, easingFunction, wait);
            }

            if (duration > TimeSpan.Zero && wait)
            {
                CurrentThread.Suspend(duration);
            }
        }

        private void MoveCore(Entity entity, TimeSpan duration, NssCoordinate x, NssCoordinate y, EasingFunction easingFunction, bool wait)
        {
            if (entity != null)
            {
                var visual = entity.GetComponent<Visual>();
                var dst = Position(x, y, visual.Position, (int)visual.Width, (int)visual.Height);

                if (duration > TimeSpan.Zero)
                {
                    var animation = new Vector2Animation
                    {
                        TargetComponent = visual,
                        PropertyGetter = c => (c as Visual).Position,
                        PropertySetter = (c, v) => (c as Visual).Position = v,
                        Duration = duration,
                        InitialValue = visual.Position,
                        FinalValue = dst
                    };
                }
                else
                {
                    visual.Position = dst;
                }
            }
        }

        public override void Zoom(string entityName, TimeSpan duration, Rational scaleX, Rational scaleY, EasingFunction easingFunction, bool wait)
        {
            if (entityName.Length > 0 && entityName[0] == '@')
            {
                entityName = entityName.Substring(1);
            }

            if (IsWildcardQuery(entityName))
            {
                foreach (var entity in _entities.WildcardQuery(entityName))
                {
                    ZoomCore(entity, duration, scaleX, scaleY, easingFunction, wait);
                }
            }
            else
            {
                var entity = _entities.SafeGet(entityName);
                ZoomCore(entity, duration, scaleX, scaleY, easingFunction, wait);
            }

            if (duration > TimeSpan.Zero && wait)
            {
                CurrentThread.Suspend(duration);
            }
        }

        private void ZoomCore(Entity entity, TimeSpan duration, Rational scaleX, Rational scaleY, EasingFunction easingFunction, bool wait)
        {
            if (entity != null)
            {
                var visual = entity.GetComponent<Visual>();
                scaleX = scaleX.Rebase(1.0f);
                scaleY = scaleY.Rebase(1.0f);

                float centerX = (visual.Position.X + visual.Width) / 2.0f;
                float centerY = (visual.Position.Y + visual.Height) / 2.0f;
                var scaleOrigin = new Vector2(centerX, centerY);

                visual.ScaleOrigin = scaleOrigin;
                if (duration > TimeSpan.Zero)
                {
                    visual.Scale = new Vector2(0, 0);
                    var animation = new Vector2Animation
                    {
                        TargetComponent = visual,
                        PropertyGetter = c => (c as Visual).Scale,
                        PropertySetter = (c, v) => (c as Visual).Scale = v,
                        Duration = duration,
                        InitialValue = visual.Scale,
                        FinalValue = new Vector2(scaleX, scaleY),
                        TimingFunction = (TimingFunction)easingFunction
                    };

                    entity.AddComponent(animation);
                }
                else
                {
                    visual.Scale = new Vector2(scaleX, scaleY);
                }
            }
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

        private static float NssToAbsoluteCoordinate(NssCoordinate coordinate, float currentValue, float objectDimension, float viewportDimension)
        {
            switch (coordinate.Origin)
            {
                case NssPositionOrigin.Zero:
                default:
                    return coordinate.Value;

                case NssPositionOrigin.Current:
                    return currentValue + coordinate.Value;

                case NssPositionOrigin.Center:
                    return viewportDimension / 2.0f - objectDimension / 2.0f;

                case NssPositionOrigin.InBottom:
                    return viewportDimension - objectDimension;
            }
        }
    }
}
