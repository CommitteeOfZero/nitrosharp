using System;
using System.Numerics;
using ImGuiNET;

namespace NitroSharp.Diagnostics
{
    internal sealed class LogView
    {
        private readonly LogEventRecorder _logEventRecorder;
        private int _prevEventCount;

        public LogView(LogEventRecorder logEventRecorder)
        {
            _logEventRecorder = logEventRecorder;
        }

        public void Render()
        {
            ImGui.BeginChild("Log", Vector2.Zero, false);
            ReadOnlySpan<LogEvent> logEvents = _logEventRecorder.LogEvents;
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            foreach (LogEvent logEvent in logEvents)
            {
                Vector4 color = logEvent.LogLevel switch
                {
                    LogLevel.Information => Vector4.One,
                    LogLevel.Warning => new Vector4(1, 1, 0, 1),
                    LogLevel.Error => new Vector4(1, 0, 0, 1),
                    _ => ThrowHelper.Unreachable<Vector4>()
                };
                
                Vector2 topLeft = ImGui.GetCursorScreenPos();
                Vector2 textSize = ImGui.CalcTextSize(logEvent.Message);
                ImGui.TextColored(color, logEvent.Message);
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.Text("Copy to clipboard");
                    ImGui.EndTooltip();
                    drawList.AddRectFilled(
                        a: topLeft,
                        b: topLeft + textSize,
                        ImGui.GetColorU32(ImGuiCol.FrameBgHovered)
                    );
                }
                if (ImGui.IsItemClicked())
                {
                    ImGui.SetClipboardText(logEvent.Message);
                }
            }

            if (logEvents.Length > _prevEventCount)
            {
                ImGui.SetScrollHereY();
            }

            _prevEventCount = logEvents.Length;
            ImGui.EndChild();
        }
    }
}
