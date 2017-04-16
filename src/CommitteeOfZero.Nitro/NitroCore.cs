using System;
using CommitteeOfZero.NsScript.Execution;
using System.Collections.Generic;
using CommitteeOfZero.Nitro.Graphics;
using CommitteeOfZero.Nitro.Text;
using CommitteeOfZero.NsScript.PXml;
using CommitteeOfZero.NsScript;
using MoeGame.Framework.Content;
using MoeGame.Framework;
using CommitteeOfZero.Nitro.Graphics.RenderItems;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace CommitteeOfZero.Nitro
{
    public sealed class NitroCore : BuiltInFunctionsBase
    {
        private System.Drawing.Size _viewport;
        private ContentManager _content;
        private readonly EntityManager _entities;

        private readonly Game _game;
        private DialogueBox _currentDialogueBox;
        private Entity _textEntity;

        public NitroCore(Game game, NitroConfiguration configuration, EntityManager entities)
        {
            _game = game;
            _entities = entities;
            _viewport = new System.Drawing.Size(configuration.WindowWidth, configuration.WindowHeight);
            EnteredDialogueBlock += OnEnteredDialogueBlock;
        }

        public void SetContent(ContentManager content) => _content = content;

        public override void AddRectangle(string entityName, int priority, NsCoordinate x, NsCoordinate y, int width, int height, NsColor color)
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

        public override void LoadImage(string entityName, string fileName)
        {

        }

        public override void AddTexture(string entityName, int priority, NsCoordinate x, NsCoordinate y, string fileOrEntityName)
        {
            if (fileOrEntityName.Equals("SCREEN", StringComparison.OrdinalIgnoreCase))
            {
                var position = Position(x, y, Vector2.Zero, _viewport.Width, _viewport.Height);
                var screencap = new ScreenCap
                {
                    Position = position,
                    Priority = priority,
                };

                _entities.Create(entityName, replace: true).WithComponent(screencap);
            }
            else
            {
                CurrentThread.Suspend();
                _content.LoadAsync<TextureAsset>(fileOrEntityName).ContinueWith(t =>
                {
                    var texture = t.Result;
                    var position = Position(x, y, Vector2.Zero, (int)texture.Width, (int)texture.Height);
                    var visual = new TextureVisual
                    {
                        Position = position,
                        Priority = priority,
                        AssetRef = fileOrEntityName,
                        Width = texture.Width,
                        Height = texture.Height
                    };

                    _entities.Create(entityName, replace: true).WithComponent(visual);
                    CurrentThread.Resume();
                }, CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion, _game.MainLoopTaskScheduler);
            }
        }

        private Vector2 Position(NsCoordinate x, NsCoordinate y, Vector2 current, int width, int height)
        {
            float absoluteX = NssToAbsoluteCoordinate(x, current.X, width, _viewport.Width);
            float absoluteY = NssToAbsoluteCoordinate(y, current.Y, height, _viewport.Height);

            return new Vector2(absoluteX, absoluteY);
        }

        public override int GetTextureWidth(string entityName)
        {
            if (_entities.TryGet(entityName, out var entity))
            {
                var texture = entity.GetComponent<TextureVisual>();
                if (texture != null)
                {
                    return (int)texture.Width;
                }
            }

            return 0;
        }

        private void OnEnteredDialogueBlock(object sender, DialogueBlock block)
        {
            if (_textEntity != null)
            {
                _entities.Remove(_textEntity);
            }

            _entities.TryGet(block.BoxName, out var boxEntity);
            _currentDialogueBox = boxEntity?.GetComponent<DialogueBox>();

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

            Task.Run(() =>
            {
                var root = PXmlBlock.Parse(pxmlString);
                var flattener = new PXmlTreeFlattener();

                string plainText = flattener.Flatten(root);
                return Task.FromResult(plainText);
            }).ContinueWith(t =>
            {
                string plainText = t.Result;
                _entities.TryGet(CurrentDialogueBlock.Identifier, out var textEntity);
                var textVisual = textEntity?.GetComponent<GameTextVisual>();
                if (textVisual != null)
                {
                    textVisual.Reset();

                    textVisual.Text = plainText;
                    textVisual.Priority = 30000;
                    textVisual.Color = RgbaValueF.White;
                    textVisual.IsEnabled = true;
                }
            }, _game.MainLoopTaskScheduler);
        }

        public override void LoadAudio(string entityName, NsAudioKind kind, string fileName)
        {
            _entities.Create(entityName, replace: true)
                .WithComponent(new SoundComponent { AudioFile = fileName });
        }

        private Queue<Entity> _entitiesToRemove = new Queue<Entity>();

        public override void RemoveEntity(string entityName)
        {
            if (!IsWildcardQuery(entityName))
            {
                if (_entities.TryGet(entityName, out var entity))
                {
                    RemoveEntityCore(entity);
                }
            }
            else
            {
                foreach (var e in _entities.WildcardQuery(entityName))
                {
                    if (!e.IsLocked())
                    {
                        RemoveEntityCore(e);
                    }
                }
            }

            while (_entitiesToRemove.Count > 0)
            {
                _entities.Remove(_entitiesToRemove.Dequeue());
            }
        }

        private void RemoveEntityCore(Entity entity)
        {
            _entitiesToRemove.Enqueue(entity);
            var texture = entity.GetComponent<TextureVisual>();
            if (texture != null)
            {
                //_content.Unload(texture.AssetRef);
            }
        }

        private bool IsWildcardQuery(string s) => s[s.Length - 1] == '*';

        public override void Fade(string entityName, TimeSpan duration, NsRational opacity, bool wait)
        {
            if (IsWildcardQuery(entityName))
            {
                foreach (var entity in _entities.WildcardQuery(entityName))
                {
                    FadeCore(entity, duration, opacity, wait);
                }
            }
            else
            {
                if (_entities.TryGet(entityName, out var entity))
                {
                    FadeCore(entity, duration, opacity, wait);
                }
            }

            if (duration > TimeSpan.Zero && wait)
            {
                CurrentThread.Suspend(duration);
            }
        }

        private void FadeCore(Entity entity, TimeSpan duration, NsRational opacity, bool wait)
        {
            float adjustedOpacity = opacity.Rebase(1.0f);
            var visual = entity.GetComponent<Visual>();
            if (duration > TimeSpan.Zero)
            {
                var animation = new FloatAnimation
                {
                    TargetComponent = visual,
                    PropertySetter = (c, v) => (c as Visual).Opacity = v,
                    Duration = duration,
                    InitialValue = visual.Opacity,
                    FinalValue = adjustedOpacity
                };

                entity.AddComponent(animation);
            }
            else
            {
                visual.Opacity = adjustedOpacity;
            }
        }

        public override void DrawTransition(string entityName, TimeSpan duration, NsRational initialOpacity,
            NsRational finalOpacity, NsRational feather, string maskFileName, bool wait)
        {
            initialOpacity = initialOpacity.Rebase(1.0f);
            finalOpacity = finalOpacity.Rebase(1.0f);

            foreach (var entity in _entities.WildcardQuery(entityName))
            {
                var sourceVisual = entity.GetComponent<Visual>();
                var transition = new TransitionVisual
                {
                    Source = sourceVisual,
                    MaskAsset = maskFileName,
                    Priority = sourceVisual.Priority,
                    Position = sourceVisual.Position
                };

                var animation = new FloatAnimation
                {
                    TargetComponent = transition,
                    PropertySetter = (c, v) => (c as TransitionVisual).Opacity = v,
                    InitialValue = initialOpacity,
                    FinalValue = finalOpacity,
                    Duration = duration
                };

                animation.Completed += (o, e) =>
                {
                    entity.RemoveComponent(transition);
                    entity.AddComponent(sourceVisual);
                };

                entity.RemoveComponent(sourceVisual);
                CurrentThread.Suspend();
                _content.LoadAsync<TextureAsset>(maskFileName).ContinueWith(t =>
                {
                    entity.AddComponent(transition);
                    entity.AddComponent(animation);
                    CurrentThread.Resume();
                }, CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion, _game.MainLoopTaskScheduler);
            }
        }

        public override void CreateDialogueBox(string entityName, int priority, NsCoordinate x, NsCoordinate y, int width, int height)
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

        public override void WaitForInput()
        {
            CurrentThread.Suspend();
        }

        public override void WaitForInput(TimeSpan timeout)
        {
            CurrentThread.Suspend(timeout);
        }

        public override void SetVolume(string entityName, TimeSpan duration, int volume)
        {
            if (entityName == null)
                return;

            if (!IsWildcardQuery(entityName))
            {
                if (_entities.TryGet(entityName, out var entity))
                {
                    SetVolumeCore(entity, duration, volume);
                    return;
                }
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
                if (_entities.TryGet(entityName, out var entity))
                {
                    ToggleLoopingCore(entity, looping);
                }
            }
            else
            {
                foreach (var e in _entities.WildcardQuery(entityName))
                {
                    ToggleLoopingCore(e, looping);
                }
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
                if (_entities.TryGet(entityName, out var entity))
                {
                    SetLoopingCore(entity, loopStart, loopEnd);
                }
            }
            else
            {
                foreach (var e in _entities.WildcardQuery(entityName))
                {
                    SetLoopingCore(e, loopStart, loopEnd);
                }
            }
        }

        private void SetLoopingCore(Entity entity, TimeSpan loopStart, TimeSpan loopEnd)
        {
            var sound = entity.GetComponent<SoundComponent>();
            sound.LoopStart = loopEnd;
            sound.LoopEnd = loopEnd;
            sound.Looping = true;
        }

        public override void Move(string entityName, TimeSpan duration, NsCoordinate x, NsCoordinate y, NsEasingFunction easingFunction, bool wait)
        {
            if (!IsWildcardQuery(entityName))
            {
                if (_entities.TryGet(entityName, out var entity))
                {
                    MoveCore(entity, duration, x, y, easingFunction, wait);
                }
            }
            else
            {
                foreach (var entity in _entities.WildcardQuery(entityName))
                {
                    MoveCore(entity, duration, x, y, easingFunction, wait);
                }
            }

            if (duration > TimeSpan.Zero && wait)
            {
                CurrentThread.Suspend(duration);
            }
        }

        private void MoveCore(Entity entity, TimeSpan duration, NsCoordinate x, NsCoordinate y, NsEasingFunction easingFunction, bool wait)
        {
            var visual = entity.GetComponent<Visual>();
            var dst = Position(x, y, visual.Position, (int)visual.Width, (int)visual.Height);

            if (duration > TimeSpan.Zero)
            {
                var animation = new Vector2Animation
                {
                    TargetComponent = visual,
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

        public override void Zoom(string entityName, TimeSpan duration, NsRational scaleX, NsRational scaleY, NsEasingFunction easingFunction, bool wait)
        {
            if (!IsWildcardQuery(entityName))
            {
                if (_entities.TryGet(entityName, out var entity))
                {
                    ZoomCore(entity, duration, scaleX, scaleY, easingFunction, wait);
                }
            }
            else
            {
                foreach (var entity in _entities.WildcardQuery(entityName))
                {
                    ZoomCore(entity, duration, scaleX, scaleY, easingFunction, wait);
                }
            }

            if (duration > TimeSpan.Zero && wait)
            {
                CurrentThread.Suspend(duration);
            }
        }

        private void ZoomCore(Entity entity, TimeSpan duration, NsRational scaleX, NsRational scaleY, NsEasingFunction easingFunction, bool wait)
        {
            var visual = entity.GetComponent<Visual>();
            scaleX = scaleX.Rebase(1.0f);
            scaleY = scaleY.Rebase(1.0f);
            if (duration > TimeSpan.Zero)
            {
                Vector2 final = new Vector2(scaleX, scaleY);
                if (visual.Scale == final)
                {
                    visual.Scale = new Vector2(0.0f, 0.0f);
                }

                var animation = new Vector2Animation
                {
                    TargetComponent = visual,
                    PropertySetter = (c, v) => (c as Visual).Scale = v,
                    Duration = duration,
                    InitialValue = visual.Scale,
                    FinalValue = final,
                    TimingFunction = (TimingFunction)easingFunction
                };

                entity.AddComponent(animation);
            }
            else
            {
                visual.Scale = new Vector2(scaleX, scaleY);
            }
        }

        public override void Request(string entityName, NsEntityAction action)
        {
            if (entityName == null)
                return;

            if (!IsWildcardQuery(entityName))
            {
                if (_entities.TryGet(entityName, out var entity))
                {
                    RequestCore(entity, action);
                }
            }
            else
            {
                foreach (var e in _entities.WildcardQuery(entityName))
                {
                    RequestCore(e, action);
                }
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
                    _entities.Remove(entity);
                    break;
            }
        }

        private static float NssToAbsoluteCoordinate(NsCoordinate coordinate, float currentValue, float objectDimension, float viewportDimension)
        {
            switch (coordinate.Origin)
            {
                case NsCoordinateOrigin.Zero:
                    return coordinate.Value;

                case NsCoordinateOrigin.CurrentValue:
                    return coordinate.Value + currentValue;

                case NsCoordinateOrigin.Left:
                    return coordinate.Value - objectDimension * coordinate.AnchorPoint;
                case NsCoordinateOrigin.Top:
                    return coordinate.Value - objectDimension * coordinate.AnchorPoint;
                case NsCoordinateOrigin.Right:
                    return viewportDimension - objectDimension * coordinate.AnchorPoint;
                case NsCoordinateOrigin.Bottom:
                    return viewportDimension - objectDimension * coordinate.AnchorPoint;

                case NsCoordinateOrigin.Center:
                    return (viewportDimension - objectDimension) / 2.0f;

                default:
                    throw new ArgumentException("Illegal value.", nameof(coordinate));
            }
        }
    }
}
