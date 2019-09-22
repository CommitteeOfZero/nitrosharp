using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using NitroSharp.Animation;
using NitroSharp.Graphics;
using NitroSharp.Media;
using NitroSharp.Content;
using NitroSharp.Primitives;
using NitroSharp.Utilities;
using Veldrid;
using NitroSharp.NsScript;

#nullable enable

namespace NitroSharp
{
    internal sealed class OldWorld
    {
        public const ushort InitialCapacity = 1024;
        public const ushort InitialSpriteCount = 768;
        public const ushort InitialRectangleCount = 32;
        public const ushort InitialTextLayoutCount = 32;
        public const ushort InitialAudioClipCount = 64;
        public const ushort InitialVideoClipCount = 4;

        private readonly Dictionary<string, OldEntity> _entities;
        private readonly Dictionary<string, string> _aliases;
        private readonly List<EntityTable> _tables;
        private ushort _nextEntityId = 1;

        private readonly Dictionary<AnimationDictionaryKey, PropertyAnimation> _activeAnimations;
        private readonly List<(AnimationDictionaryKey key, PropertyAnimation anim)> _animationsToDeactivate;

        private Queue<(AnimationDictionaryKey, PropertyAnimation)> _queuedAnimations;

        public OldWorld()
        {
            _entities = new Dictionary<string, OldEntity>(InitialCapacity);
            _aliases = new Dictionary<string, string>();
            _tables = new List<EntityTable>(8);

            Threads = RegisterTable(new ThreadTable(this, 32));
            Sprites = RegisterTable(new SpriteTable(this, InitialSpriteCount));
            Rectangles = RegisterTable(new RectangleTable(this, InitialRectangleCount));
            TextBlocks = RegisterTable(new TextBlockTable(this, columnCount: 32));
            AudioClips = RegisterTable(new AudioClipTable(this, InitialAudioClipCount));
            VideoClips = RegisterTable(new VideoClipTable(this, InitialVideoClipCount));
            Choices = RegisterTable(new ChoiceTable(this, 32));

            _activeAnimations = new Dictionary<AnimationDictionaryKey, PropertyAnimation>();
            _animationsToDeactivate = new List<(AnimationDictionaryKey key, PropertyAnimation anim)>();
            _queuedAnimations = new Queue<(AnimationDictionaryKey, PropertyAnimation)>();
        }

        public ThreadTable Threads { get; }
        public SpriteTable Sprites { get; }
        public RectangleTable Rectangles { get; }
        public TextBlockTable TextBlocks { get; }
        public AudioClipTable AudioClips { get; }
        public VideoClipTable VideoClips { get; }
        public ChoiceTable Choices { get; }

        public Dictionary<string, OldEntity>.Enumerator EntityEnumerator => _entities.GetEnumerator();
        public Dictionary<AnimationDictionaryKey, PropertyAnimation>.ValueCollection AttachedAnimations
            => _activeAnimations.Values;

        private T RegisterTable<T>(T table) where T : EntityTable
        {
            _tables.Add(table);
            return table;
        }

        public T GetTable<T>(OldEntity entity) where T : EntityTable
            => (T)_tables[(int)entity.Kind];

        public T GetEntityStruct<T>(OldEntity entity) where T : EntityStruct
        {
            EntityTable table = GetTable<EntityTable>(entity);
            return table.Get<T>(entity);
        }

        public bool TryGetEntity(string name, out OldEntity entity)
            => _entities.TryGetValue(name, out entity);

        public bool IsEntityAlive(OldEntity entity)
        {
            var table = GetTable<EntityTable>(entity);
            return table.EntityExists(entity);
        }

        public void SetAlias(string name, string alias)
        {
            if (TryGetEntity(name, out OldEntity entity) || TryGetEntity(alias, out entity))
            {
                _entities[name] = entity;
                _entities[alias] = entity;
                _aliases[name] = alias;
                _aliases[alias] = name;
            }
        }

        public TextBlock CreateTextBlock(string name, int renderPriority)
        {
            OldEntity entity = CreateEntity(name, EntityKind.TextBlock);
            TextBlock block = GetEntityStruct<TextBlock>(entity);
            block.Transform = Matrix4x4.Identity;
            block.SortKey = new RenderItemKey((ushort)renderPriority, entity.Id);
            block.TransformComponents.Scale = Vector3.One;
            return block;
        }

        public OldEntity CreateThreadEntity(in InterpreterThreadInfo threadInfo)
        {
            OldEntity entity = CreateEntity(threadInfo.Name, EntityKind.Thread);
            Threads.Infos.Set(entity, threadInfo);
            return entity;
        }

        public OldEntity CreateSprite(
            string name, AssetId image, in RectangleF sourceRectangle,
            int renderPriority, SizeF size, ref RgbaFloat color)
        {
            OldEntity entity = CreateVisual(name, EntityKind.Sprite, renderPriority, size, ref color);
            Sprites.ImageSources.Set(entity, new ImageSource(image, sourceRectangle));
            return entity;
        }

        public OldEntity CreateRectangle(string name, int renderPriority, SizeF size, ref RgbaFloat color)
        {
            OldEntity entity = CreateVisual(name, EntityKind.Rectangle, renderPriority, size, ref color);
            return entity;
        }

        public OldEntity CreateAudioClip(string name, AssetId asset, bool enableLooping)
        {
            OldEntity entity = CreateEntity(name, EntityKind.AudioClip);
            AudioClips.Asset.Set(entity, asset);
            AudioClips.LoopData.Set(entity, new MediaClipLoopData(enableLooping, null));
            AudioClips.Volume.Set(entity, 1.0f);
            return entity;
        }

        public OldEntity CreateVideoClip(string name, AssetId asset, bool enableLooping, int renderPriority, ref RgbaFloat color)
        {
            OldEntity entity = CreateVisual(name, EntityKind.VideoClip, renderPriority, default, ref color);
            VideoClips.Asset.Set(entity, asset);
            VideoClips.LoopData.Set(entity, new MediaClipLoopData(enableLooping, null));
            VideoClips.Volume.Set(entity, 1.0f);
            return entity;
        }

        public OldEntity CreateChoice(string name)
        {
            OldEntity entity = CreateEntity(name, EntityKind.Choice);
            Choices.Name.Set(entity, name);
            return entity;
        }

        private OldEntity CreateVisual(
            string name, EntityKind kind,
            int renderPriority, SizeF size, ref RgbaFloat color)
        {
            OldEntity entity = CreateEntity(name, kind);
            RenderItemTable table = GetTable<RenderItemTable>(entity);
            table.SortKeys.Set(entity, new RenderItemKey((ushort)renderPriority, entity.Id));
            table.Bounds.Set(entity, size);
            table.Colors.Set(entity, ref color);
            table.TransformComponents.GetRef(entity).Scale = Vector3.One;
            return entity;
        }

        public void CommitActivateAnimations()
        {
            foreach ((AnimationDictionaryKey key, PropertyAnimation anim) in _queuedAnimations)
            {
                _activeAnimations[key] = anim;
            }
            _queuedAnimations.Clear();
        }

        public void ActivateAnimation<T>(T animation) where T : PropertyAnimation
        {
            var key = new AnimationDictionaryKey(animation.Entity, typeof(T));
            _queuedAnimations.Enqueue((key, animation));
            //_activeAnimations[key] = animation;
            //_animationEvents.Add(new AnimationEvent(key, AnimationEventKind.AnimationActivated));
        }

        public void DeactivateAnimation(PropertyAnimation animation)
        {
            var key = new AnimationDictionaryKey(animation.Entity, animation.GetType());
            _animationsToDeactivate.Add((key, animation));
        }

        public bool TryGetAnimation<T>(OldEntity entity, out T? animation) where T : PropertyAnimation
        {
            var key = new AnimationDictionaryKey(entity, typeof(T));
            bool result = _activeAnimations.TryGetValue(key, out PropertyAnimation? val);
            animation = val as T;
            return result;
        }

        public void FlushDetachedAnimations()
        {
            foreach ((var dictKey, var anim) in _animationsToDeactivate)
            {
                if (_activeAnimations.TryGetValue(dictKey, out var value) && value == anim)
                {
                    _activeAnimations.Remove(dictKey);
                }
            }
            _animationsToDeactivate.Clear();
        }

        public void FlushFrameEvents()
        {
            foreach (EntityTable table in _tables)
            {
                table.FlushFrameEvents();
            }
        }

        public OldEntity CreateEntity(string name, EntityKind kind)
        {
            if (_entities.TryGetValue(name, out _))
            {
                RemoveEntity(name);
            }

            EntityTable table = _tables[(int)kind];
            var handle = new OldEntity(_nextEntityId++, kind);
            table.Insert(handle);
            _entities[name] = handle;

            var parsedName = new EntityName(name);
            ReadOnlySpan<char> parentName = parsedName.Parent;
            if (parentName.Length > 0
                && TryGetEntity(parentName.ToString(), out OldEntity parent)
                && parent.IsValid)
            {
                table.Parents.Set(handle, parent);
            }

            return handle;
        }

        public void RemoveEntity(string name)
        {
            RemoveEntityCore(name);
        }

        private OldEntity RemoveEntityCore(string name)
        {
            if (_entities.TryGetValue(name, out OldEntity entity))
            {
                _entities.Remove(name);
                if (_aliases.TryGetValue(name, out string? alias))
                {
                    _entities.Remove(alias);
                    _aliases.Remove(name);
                    _aliases.Remove(alias);
                }

                var table = GetTable<EntityTable>(entity);
                table.Remove(entity);
                return entity;
            }

            return OldEntity.Invalid;
        }

        internal readonly struct AnimationDictionaryKey : IEquatable<AnimationDictionaryKey>
        {
            public readonly OldEntity Entity;
            public readonly Type RuntimeType;

            public AnimationDictionaryKey(OldEntity entity, Type runtimeType)
            {
                Entity = entity;
                RuntimeType = runtimeType;
            }

            public bool Equals(AnimationDictionaryKey other)
                => Entity.Equals(other.Entity) && RuntimeType == other.RuntimeType;

            public override bool Equals(object? obj)
                => obj is AnimationDictionaryKey other && Equals(other);

            public override int GetHashCode()
                => HashHelper.Combine(Entity.GetHashCode(), RuntimeType.GetHashCode());
        }
    }
}
