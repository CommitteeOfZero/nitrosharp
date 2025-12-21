using System;
using System.Collections.Generic;
using NitroSharp.Graphics;
using NitroSharp.Text;

namespace NitroSharp;

internal sealed class Process : Entity
{
    private readonly FontSettings _fontSettings;
    private readonly List<Entity> _updateList = [];
    private readonly List<RenderItem> _renderList = [];

    public Process(EntityName name, Entity? parent, FontSettings fontSettings)
        : base(name, parent)
    {
        _fontSettings = fontSettings;
        Aliases = new AliasMap(this);
    }

    public AliasMap Aliases { get; }

    public Thread MainThread
    {
        get
        {
            foreach (Thread thread in GetChildren<Thread>())
            {
                if (thread.IsMain)
                {
                    return thread;
                }
            }

            throw new InvalidOperationException("Main thread does not exist.");
        }
    }

    public Thread CurrentThread
    {
        get => field ?? MainThread;
        private set;
    }

    public void RenderAll(GameContext ctx)
    {
        _renderList.Clear();

        foreach (RenderItem renderItem in GetDescendants<RenderItem>())
        {
            _renderList.Add(renderItem);
        }

        _renderList.Sort();

        foreach (RenderItem renderItem in _renderList)
        {
            renderItem.Render(ctx);
        }
    }

    public override void Update(GameContext ctx)
    {
        _updateList.Clear();

        foreach (Entity node in GetDescendants())
        {
            _updateList.Add(node);
        }

        foreach (Entity node in _updateList)
        {
            if (node is Thread thread)
            {
                CurrentThread = thread;
            }

            node.Update(ctx);
        }
    }
}
