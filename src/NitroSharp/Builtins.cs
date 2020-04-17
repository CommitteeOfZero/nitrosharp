using System;
using System.Numerics;
using NitroSharp.Graphics;
using NitroSharp.NsScript;
using NitroSharp.NsScript.Primitives;
using NitroSharp.NsScript.VM;
using NitroSharp.Text;

#nullable enable

namespace NitroSharp
{
    internal sealed class Builtins : BuiltInFunctions
    {
        private readonly World _world;
        private readonly Context _ctx;
        private readonly RenderContext _renderCtx;

        public Builtins(Context context)
        {
            _ctx = context;
            _renderCtx = context.RenderContext;
            _world = context.World;
        }

        public override void CreateEntity(in EntityPath path)
        {
        }

        public override void CreateRectangle(
            in EntityPath entityPath,
            int priority,
            NsCoordinate x, NsCoordinate y,
            int width, int height,
            NsColor color)
        {
            if (_world.ResolvePath(entityPath, out ResolvedEntityPath resolvedPath))
            {
                ColorRect rect = _world.AddRenderItem(new ColorRect(
                    resolvedPath,
                    priority,
                    new SizeF(width, height),
                    color.ToRgbaFloat()
                ));
                rect.Transform.Position = new Vector3(x.Value, y.Value, 0);
            }
        }

        public override void CreateTextBlock(
            in EntityPath entityPath,
            int priority,
            NsCoordinate x, NsCoordinate y,
            NsDimension width, NsDimension height,
            string pxmlText)
        {
            if (_world.ResolvePath(entityPath, out ResolvedEntityPath resolvedPath))
            {
                var textBuffer = TextBuffer.FromPXmlString(pxmlText, _ctx.FontConfig, new PtFontSize(24));
                if (textBuffer.AssertSingleTextSegment() is TextSegment textSegment)
                {
                    var layout = new TextLayout(_ctx.GlyphRasterizer, textSegment.TextRuns.AsSpan(), null);
                    TextRect textRect = _world.AddRenderItem(
                        new TextRect(
                            resolvedPath,
                            priority,
                            _renderCtx.Text,
                            layout
                        )
                    );
                }
            }
        }

        public override void WaitForInput()
        {
            VM.SuspendThread(VM.CurrentThread!);
        }
    }
}
