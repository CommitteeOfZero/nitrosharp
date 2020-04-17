using System;
using ImGuiNET;
using Veldrid;

#nullable enable

namespace NitroSharp.Diagnostics
{
    internal sealed class DevModeOverlay : IDisposable
    {
        private bool _enabled = false;

        private readonly ImGuiRenderer _imguiRenderer;
        private readonly GraphicsDevice _gd;
        private readonly CommandList _cl;
        private readonly Framebuffer _framebuffer;
        private readonly LogView _logView;

        public DevModeOverlay(
            GraphicsDevice graphicsDevice,
            Framebuffer framebuffer,
            LogEventRecorder logEventRecorder)
        {
            Framebuffer mainFramebuffer = framebuffer;
            _imguiRenderer = new ImGuiRenderer(
                graphicsDevice,
                mainFramebuffer.OutputDescription,
                (int)mainFramebuffer.Width,
                (int)mainFramebuffer.Height
            );

            ImGui.EndFrame();

            ImGuiIOPtr io = ImGui.GetIO();
            io.Fonts.Clear();
            io.Fonts.AddFontFromFileTTF(
                "Fonts/NotoSansCJKjp-Regular.ttf",
                16, null, io.Fonts.GetGlyphRangesJapanese()
            );
            _imguiRenderer.RecreateFontDeviceTexture(graphicsDevice);
            ImGui.NewFrame();

            _gd = graphicsDevice;
            _cl = _gd.ResourceFactory.CreateCommandList();
            _framebuffer = mainFramebuffer;

            _logView = new LogView(logEventRecorder);
        }

        public void Tick(float deltaMilliseconds, InputTracker inputTracker)
        {
            if (inputTracker.IsKeyDown(Key.ControlLeft)
                && inputTracker.IsKeyDownThisFrame(Key.D))
            {
                _enabled = !_enabled;
            }

            if (_enabled)
            {
                Render(deltaMilliseconds, inputTracker.CurrentSnapshot);
            }
        }

        private void Render(float deltaMilliseconds, InputSnapshot inputSnapshot)
        {
            _imguiRenderer.Update(deltaMilliseconds * 1000.0f, inputSnapshot);
            ImGui.Begin("Log");
            _logView.Render();
            ImGui.End();

            _cl.Begin();
            _cl.SetFramebuffer(_framebuffer);
            _imguiRenderer.Render(_gd, _cl);
            _cl.End();
            _gd.SubmitCommands(_cl);
        }

        public void Dispose()
        {
            _imguiRenderer.Dispose();
            _cl.Dispose();
        }
    }
}
