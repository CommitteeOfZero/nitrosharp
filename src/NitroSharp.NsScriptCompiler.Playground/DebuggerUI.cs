using System;
using System.IO;
using System.Numerics;
using System.Text;
using ImGuiNET;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace NitroSharp.NsScriptCompiler.Playground
{
    public class DebuggerUI
    {
        private Sdl2Window _window;
        private GraphicsDevice _gd;

        private CommandList _cl;
        private ImGuiRenderer _imguiRenderer;

        private string[] _sourceTextLines;
        private bool[] _breakpointActive;
        private byte[] _fontName;
        private ImFontPtr _font;

        public DebuggerUI()
        {
            _sourceTextLines = File.ReadAllLines("S:/ChaosContent/Noah/nss/boot.nss");
            _breakpointActive = new bool[_sourceTextLines.Length];

            _fontName = Encoding.UTF8.GetBytes("C:/Windows/Fonts/Segoe UI.ttf");
        }

        public void Run()
        {
            VeldridStartup.CreateWindowAndGraphicsDevice(
                new WindowCreateInfo(50, 50, 1280, 720, WindowState.Normal, "NitroSharp Debugger UI"),
                new GraphicsDeviceOptions(true, null, true),
                GraphicsBackend.Direct3D11,
                out _window,
                out _gd);

            _window.Resized += () =>
            {
                _gd.MainSwapchain.Resize((uint)_window.Width, (uint)_window.Height);
                _imguiRenderer.WindowResized(_window.Width, _window.Height);
            };

            _cl = _gd.ResourceFactory.CreateCommandList();
            _imguiRenderer = new ImGuiRenderer(
                _gd, _gd.MainSwapchain.Framebuffer.OutputDescription,
                _window.Width, _window.Height);

            ImGui.EndFrame();

            //_imguiRenderer.CreateDeviceResources(_gd, _gd.MainSwapchain.Framebuffer.OutputDescription);

            ImGuiIOPtr io = ImGui.GetIO();
            io.Fonts.Clear();

            ImFontPtr font = io.Fonts.AddFontFromFileTTF(
                 "S:/NotoSansCJKtc-Regular.ttf",
                22, null, io.Fonts.GetGlyphRangesJapanese());

            _imguiRenderer.RecreateFontDeviceTexture(_gd);
            ImGui.NewFrame();

            //io.Fonts.AddFontDefault(font.ConfigData);

            while (_window.Exists)
            {
                InputSnapshot snapshot = _window.PumpEvents();
                if (!_window.Exists) { break; }
                _imguiRenderer.Update(1f / 60f, snapshot);
                SubmitUI();

                _cl.Begin();
                _cl.SetFramebuffer(_gd.MainSwapchain.Framebuffer);
                _cl.ClearColorTarget(0, RgbaFloat.CornflowerBlue);

                _imguiRenderer.Render(_gd, _cl);

                _cl.End();
                _gd.SubmitCommands(_cl);
                _gd.SwapBuffers(_gd.MainSwapchain);
            }

            _gd.WaitForIdle();
            _cl.Dispose();
            _gd.Dispose();
        }

        private unsafe void SubmitUI()
        {
           ImGui.PushFont(_font);

            ImGui.SetNextWindowSize(new Vector2(500, 350), ImGuiCond.FirstUseEver);
            if (!ImGui.Begin("meow"))
            {
                ImGui.End();
                return;
            }

            int totalLines = _sourceTextLines.Length;
            float lineHeight = ImGuiNative.igGetTextLineHeight();

            ImGuiNative.igSetNextWindowContentSize(new Vector2(0.0f, totalLines * lineHeight));
            ImGui.BeginChild("##scrolling", new Vector2(0, -ImGuiNative.igGetFrameHeightWithSpacing()), false, ImGuiWindowFlags.None);

            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0, 0));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));

            (int displayStart, int displayEnd) = ListClipper.Clip(totalLines, lineHeight);
            for (int i = displayStart; i < displayEnd; i++)
            {
                ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 24);
                Vector4* frameBg = ImGui.GetStyleColorVec4(ImGuiCol.FrameBg);
                ImGui.PushStyleColor(ImGuiCol.Button,
                    !_breakpointActive[i]
                        ? *frameBg : new Vector4(1.0f, 0.0f, 0.0f, 1.0f));

                ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(1.0f, 0.0f, 0.0f, 0.8f));
                //ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(1.0f, 0.0f, 0.0f, 1.0f));

                if (ImGui.Button(string.Empty, new Vector2(10, 10)))
                {
                    _breakpointActive[i] = !_breakpointActive[i];
                }
                ImGui.PopStyleColor(2);
                ImGui.PopStyleVar();

                ImGui.SameLine();

                ImGui.TextUnformatted((i + 1).ToString());
                ImGui.SameLine();
                ImGui.TextUnformatted(_sourceTextLines[i]);
            }

            ImGui.EndChild();
            ImGui.End();
        }
    }

    internal unsafe struct ListClipper
    {
        public static (int displayStart, int displayEnd) Clip(int itemCount, float itemHeight)
        {
            float startY = ImGuiNative.igGetCursorPosY();
            if (itemHeight > 0.0f)
            {
                int dispStart, dispEnd;
                ImGuiNative.igCalcListClipping(itemCount, itemHeight, &dispStart, &dispEnd);
                if (dispStart > 0)
                {
                    ImGuiNative.igSetCursorPosY(startY + dispStart * itemHeight);
                }

                return (dispStart, dispEnd);
            }

            return (0, 0);
        }
    }
}
