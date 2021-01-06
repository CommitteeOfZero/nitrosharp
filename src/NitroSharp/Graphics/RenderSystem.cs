using System;
using NitroSharp.Content;
using NitroSharp.NsScript;
using NitroSharp.NsScript.VM;
using NitroSharp.Text;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal sealed class RenderSystem : IDisposable
    {
        public RenderSystem(
            Configuration gameConfiguration,
            GraphicsDevice graphicsDevice,
            Swapchain swapchain,
            GlyphRasterizer glyphRasterizer,
            ContentManager contentManager,
            SystemVariableLookup systemVariables)
        {
            Context = new RenderContext(
                gameConfiguration,
                graphicsDevice,
                swapchain,
                contentManager,
                glyphRasterizer,
                systemVariables
            );
        }

        public RenderContext Context { get; }

        public void BeginFrame(in FrameStamp frameStamp, bool clear)
            => Context.BeginFrame(frameStamp, clear);

        public void EndFrame()
            => Context.EndFrame();

        public void Render(
            in FrameStamp frameStamp,
            GameContext ctx,
            SortableEntityGroupView<RenderItem> renderItems,
            float dt,
            bool assetsReady)
        {
            ReadOnlySpan<RenderItem> active = renderItems.SortActive();
            ReadOnlySpan<RenderItem> inactive = renderItems.Disabled;
            foreach (RenderItem ri in active)
            {
                ri.Update(ctx, dt, assetsReady);
            }
            foreach (RenderItem ri in inactive)
            {
                ri.Update(ctx, dt, assetsReady);
            }

            Context.ResolveGlyphs();

            foreach (RenderItem ri in active)
            {
                ri.Render(Context, assetsReady);
            }

            Context.TextureCache.BeginFrame(frameStamp);
        }

        public void ProcessChoices(World world, InputContext inputCtx, GameWindow window)
        {
            ReadOnlySpan<Choice> choices = world.Choices.Enabled;
            //if (choices.IsEmpty) { return; }

            Choice? focusedChoice = null, firstChoice = null;
            int maxPriority = -1;
            bool anyHovered = false;
            foreach (Choice c in choices)
            {
                c.RecordInput(inputCtx, Context);
                anyHovered |= c.IsHovered;
                if (c.CanFocus && c.Priority > maxPriority)
                {
                    maxPriority = c.Priority;
                    firstChoice = c;
                    if (c.IsHovered)
                    {
                        focusedChoice = c;
                    }
                }
            }

            foreach (RenderItem ri in world.RenderItems.Enabled)
            {
                if (ri is UiElement c)
                {
                    c.RecordInput(inputCtx, Context);
                    anyHovered |= c.IsHovered;
                }
            }

            foreach (RenderItem ri in world.RenderItems.Disabled)
            {
                if (ri is UiElement c)
                {
                    c.RecordInput(inputCtx, Context);
                    anyHovered |= c.IsHovered;
                }
            }

            SystemCursor cursor = anyHovered ? SystemCursor.Hand : SystemCursor.Arrow;
            window.SetCursor(cursor);

            Choice? shiftFocus(NsFocusDirection direction, Choice? current)
            {
                if (current is object && world.Get(current.GetNextFocus(direction)) is Choice next)
                {
                    return next;
                }

                return firstChoice;
            }

            Choice? prevFocus = focusedChoice;

            if (inputCtx.VKeyDown(VirtualKey.Left))
            {
                focusedChoice = shiftFocus(NsFocusDirection.Left, focusedChoice);
            }
            else if (inputCtx.VKeyDown(VirtualKey.Up))
            {
                focusedChoice = shiftFocus(NsFocusDirection.Up, focusedChoice);
            }
            else if (inputCtx.VKeyDown(VirtualKey.Right))
            {
                focusedChoice = shiftFocus(NsFocusDirection.Right, focusedChoice);
            }
            else if (inputCtx.VKeyDown(VirtualKey.Down))
            {
                focusedChoice = shiftFocus(NsFocusDirection.Down, focusedChoice);
            }

            if (!ReferenceEquals(focusedChoice, prevFocus) && focusedChoice is object)
            {
                focusedChoice.Focus(inputCtx.Window, Context);
            }
        }

        public void Present()
        {
            Context.Present();
        }

        public void Dispose()
        {
            Context.Dispose();
        }
    }
}
