using System;
using NitroSharp.Content;
using NitroSharp.NsScript;
using NitroSharp.Text;
using Veldrid;

#nullable enable

namespace NitroSharp.Graphics
{
    internal sealed class RenderSystem : IDisposable
    {
        public RenderSystem(
            Configuration gameConfiguration,
            GraphicsDevice graphicsDevice,
            Swapchain swapchain,
            GlyphRasterizer glyphRasterizer,
            ContentManager contentManager)
        {
            Context = new RenderContext(
                gameConfiguration,
                graphicsDevice,
                swapchain,
                contentManager,
                glyphRasterizer
            );
        }

        public RenderContext Context { get; }

        public void Render(
            GameContext ctx,
            ReadOnlySpan<RenderItem> renderItems,
            in FrameStamp frameStamp,
            float dt,
            bool assetsReady)
        {
            Context.BeginFrame(frameStamp);

            foreach (RenderItem ri in renderItems)
            {
                ri.Update(ctx, dt, assetsReady);
            }
            foreach (RenderItem ri in renderItems)
            {
                if (!ri.IsHidden)
                {
                    ri.Render(Context, assetsReady);
                }
            }

            Context.EndFrame();
        }

        public void ProcessChoices(World world, InputContext inputCtx)
        {
            ReadOnlySpan<Choice> choices = world.Choices.Enabled;
            if (choices.IsEmpty) { return; }

            Choice? focusedChoice = null, firstChoice = null;
            int maxPriority = -1;
            foreach (Choice c in choices)
            {
                c.HandleInput(inputCtx, Context);
                if (c.CanFocus && c.Priority > maxPriority)
                {
                    firstChoice = c;
                    if (c.LastMouseState == MouseState.Over)
                    {
                        focusedChoice = c;
                    }
                }
            }

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
