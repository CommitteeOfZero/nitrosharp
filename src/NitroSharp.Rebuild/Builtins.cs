using System;
using System.Diagnostics;
using System.Numerics;
using NitroSharp.Common;
using NitroSharp.Graphics;
using NitroSharp.NsScript;
using NitroSharp.NsScript.Primitives;
using NitroSharp.NsScript.VM;
using Veldrid;

namespace NitroSharp;

internal sealed class Builtins : BuiltInFunctions
{
    private readonly GameContext _ctx;
    private readonly World _world;

    public Builtins(GameContext gameContext)
    {
        _ctx = gameContext;
        _world = gameContext.World;
    }

    private Thread CurrentThread => _world.CurrentProcess.CurrentThread;
    private Stopwatch Clock => _ctx.Clock;

    private SmallList<Entity> Query(in EntityQuery query) => _world.Query(query);

    public override void CreateEntity(in EntityPath entityPath)
    {
        if (_world.TryResolvePath(entityPath, out EntityName name, out Entity? parent))
        {
            _world.AddEntity(new BlankEntity(name, parent));
        }
    }

    public override void LoadImage(in EntityPath entityPath, string source)
    {
        if (_world.TryResolvePath(entityPath, out EntityName name, out Entity? parent))
        {
            _world.AddEntity(new Image(name, parent, GetSpriteTexture(source)));
        }
    }

    public override void CreateSprite(in EntityPath entityPath, int priority, NsCoordinate x, NsCoordinate y, string source)
    {
        if (_world.TryResolvePath(entityPath, out EntityName name, out Entity? parent))
        {
            _world.AddEntity(new Sprite(name, parent, priority, GetSpriteTexture(source)))
                .WithPosition(_ctx.RenderContext, x, y);
        }
    }

    public override void CreateSpriteEx(in EntityPath entityPath, int priority, NsCoordinate x, NsCoordinate y, uint srcX, uint srcY, uint width, uint height, string source)
    {
        if (_world.TryResolvePath(entityPath, out EntityName name, out Entity? parent))
        {
            SpriteTexture texture = GetSpriteTexture(source, new DesignRectU(srcX, srcY, width, height));
            _world.AddEntity(new Sprite(name, parent, priority, texture))
                .WithPosition(_ctx.RenderContext, x, y);
        }
    }

    private SpriteTexture GetSpriteTexture(string source, DesignRectU? sourceRect = null)
    {
        if (_ctx.Content.RequestTexture(source) is { } assetRef)
        {
            return SpriteTexture.FromAsset(assetRef, sourceRect);
        }
        if (EntityPath.TryParse(source) is { } sourcePath && _world.Get(sourcePath) is Image sourceImage)
        {
            return sourceImage.Texture.WithSourceRectangle(sourceRect);
        }

        return SpriteTexture.SolidColor(RgbaFloat.CornflowerBlue, new DesignSizeU(50, 50));
    }

    public override void CreateTextBlock(
        in EntityPath entityPath,
        int priority,
        NsCoordinate x,
        NsCoordinate y,
        NsTextDimension width,
        NsTextDimension height,
        string markup)
    {
        if (_world.TryResolvePath(entityPath, out EntityName name, out Entity? parent))
        {
            _world.AddEntity(new TextBlock(name, parent, priority, markup, _ctx.RenderContext.Text))
                .WithPosition(_ctx.RenderContext, x, y);
        }
    }

    public override int GetWidth(in EntityQuery query)
    {
        return _world.Query(query) is [RenderItem renderItem, ..]
            ? (int)renderItem.GetSize(_ctx.RenderContext).Width
            : 0;
    }

    public override int GetHeight(in EntityQuery query)
    {
        return _world.Query(query) is [RenderItem renderItem, ..]
            ? (int)renderItem.GetSize(_ctx.RenderContext).Height
            : 0;
    }

    public override int GetSoundAmplitude(string characterName)
    {
        return 0;
    }

    public override void CreateThread(in EntityPath entityPath, string target, NsCoordinate x, NsCoordinate y)
    {
        if (_world.TryResolvePath(entityPath, out EntityName name, out Entity? parent))
        {
            parent = _world.CurrentProcess;
            NsScriptThreadState? vmState = VM.CreateThread(CurrentModule.Name, target);
            if (vmState.HasValue)
            {
                _world.AddEntity(new Thread(name, parent, vmState.Value, isMain: false));
            }
        }
    }

    public override void CreateRectangle(in EntityPath entityPath, int priority, NsCoordinate x, NsCoordinate y, uint width, uint height, NsColor color)
    {
        if (_world.TryResolvePath(entityPath, out EntityName entityName, out Entity? parent))
        {
            var size = new DesignSize(width, height);
            _world.AddEntity(new SolidColorRect(entityName, parent, priority, color.ToRgbaFloat(), size))
                .WithPosition(_ctx.RenderContext, x, y);
        }
    }

    public override void Move(in EntityQuery query, TimeSpan duration, NsCoordinate dstX, NsCoordinate dstY, NsEaseFunction easeFunction, TimeSpan delay)
    {
        foreach (Entity entity in Query(query))
        {
            foreach (Entity node in entity.DescendantsAndSelf())
            {
                node.Move(_ctx.RenderContext, dstX, dstY, duration, easeFunction);
            }
        }
    }

    public override void Fade(in EntityQuery query, TimeSpan duration, NsRational dstOpacity, NsEaseFunction easeFunction, TimeSpan delay)
    {
        foreach (Entity entity in Query(query))
        {
            foreach (Entity node in entity.DescendantsAndSelf())
            {
                node.Fade(dstOpacity, duration, easeFunction);
            }
        }
    }

    public override void Zoom(in EntityQuery query, TimeSpan duration, NsRational dstScaleX, NsRational dstScaleY, NsEaseFunction easeFunction, TimeSpan delay)
    {
        duration = AdjustDuration(duration);
        delay = AdjustDuration(delay);
        var dstScale = new Vector3(dstScaleX.Rebase(1.0f), dstScaleY.Rebase(1.0f), 1.0f);
        foreach (Entity entity in Query(query))
        {
            entity.Scale(dstScale, duration, easeFunction);
        }
    }

    private TimeSpan AdjustDuration(TimeSpan duration)
    {
        return false
            ? TimeSpan.FromSeconds(duration.TotalSeconds / 10.0d)
            : duration;
    }

    public override void SetAlias(in EntityQuery query, in EntityAlias alias)
    {
        if (_world.IsValidAlias(alias))
        {
            foreach (Entity entity in Query(query))
            {
                entity.SetAlias(alias);
            }
        }
    }

    public override void Request(in EntityQuery query, NsEntityAction action)
    {
        foreach (Entity entity in Query(query))
        {

        }
    }

    public override void WaitForInput()
    {
        CurrentThread.Wait(Thread.WaitOperation.UserInput(deadline: null));
    }

    public override void WaitForInput(TimeSpan timeout)
    {
        CurrentThread.Wait(Thread.WaitOperation.UserInput(deadline: Clock.Elapsed + timeout));
    }

    public override void DestroyEntities(in EntityQuery query)
    {
        foreach (Entity entity in Query(query))
        {
            _world.DestroyEntity(entity);
        }
    }
}
