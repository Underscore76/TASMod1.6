using ImGuiNET;
using ImGuiVector2 = System.Numerics.Vector2;
using ImGuiVector4 = System.Numerics.Vector4;
using TASMod.Overlays.Widgets;
using TASMod.Recording;
using TASMod.System;
using TASMod.Extensions;
using TASMod.Inputs;

namespace TASMod.Overlays.Widgets
{
    public class ControllerWidget
    {
        public static void Draw(int index)
        {
            ImGui.SetNextWindowPos(new ImGuiVector2(10, 10 + (index - 1) * 150), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSize(new ImGuiVector2(300, 140), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowBgAlpha(0.9f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 5.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 3.0f);
            ImGui.Begin($"Controller {index}", ImGuiWindowFlags.AlwaysAutoResize);

            TASGamePadState con = TASInputState.GetTASGamePadState(index);
            bool hasInput = GamePadInputQueue.HasInput(index);

            // Input status display with inline clear button
            if (hasInput)
            {
                ImGui.TextColored(new ImGuiVector4(0, 1, 0, 1), "✓ Has Queued Input");
            }
            else
            {
                ImGui.TextColored(new ImGuiVector4(0.6f, 0.6f, 0.6f, 1), "○ No Queued Input");
            }
            ImGui.SameLine();
            if (!hasInput) ImGui.BeginDisabled();
            if (ImGui.Button("Clear##input"))
            {
                GamePadInputQueue.ClearQueue(index);
            }
            if (!hasInput) ImGui.EndDisabled();

            // Frame function status display with inline clear button
            bool hasFrameFunction = GamePadInputQueue.HasFrameFunction(index);

            if (hasFrameFunction)
            {
                ImGui.TextColored(new ImGuiVector4(0, 1, 0, 1), "✓ Has Frame Function");
            }
            else
            {
                ImGui.TextColored(new ImGuiVector4(0.6f, 0.6f, 0.6f, 1), "○ No Frame Function");
            }
            ImGui.SameLine();
            if (!hasFrameFunction) ImGui.BeginDisabled();
            if (ImGui.Button("Clear##function"))
            {
                GamePadInputQueue.ClearFrameFunction(index);
            }
            if (!hasFrameFunction) ImGui.EndDisabled();

            if (hasFrameFunction)
            {
                FrameFunction frameFunc = GamePadInputQueue.GetFrameFunction(index);
                ImGui.Text($"Name: {frameFunc?.name}");
                if (ImGui.IsItemHovered())
                {
                    string description = frameFunc?.description;
                    if (!string.IsNullOrEmpty(description))
                    {
                        ImGui.SetTooltip(description);
                    }
                }
            }

            ImGui.Separator();

            // Function assignment section
            if (ImGui.CollapsingHeader("Assign Function"))
            {
                var namedFunctions = GamePadInputQueue.NamedFunctions;
                if (namedFunctions.Count > 0)
                {
                    ImGui.Text("Available Functions:");
                    foreach (var kvp in namedFunctions)
                    {
                        string functionName = kvp.Key;
                        var frameFunction = kvp.Value;

                        if (ImGui.Button($"Assign '{functionName}'"))
                        {
                            GamePadInputQueue.SetFrameFunction(index, functionName);
                        }

                        // Show description as tooltip
                        if (ImGui.IsItemHovered() && !string.IsNullOrEmpty(frameFunction.description))
                        {
                            ImGui.SetTooltip(frameFunction.description);
                        }
                    }
                }
                else
                {
                    ImGui.TextColored(new ImGuiVector4(0.6f, 0.6f, 0.6f, 1), "No named functions available");
                }
            }

            ImGui.Separator();

            // Controller input display
            GamePadInputWidget.Draw("Input", ref con);
            ImGui.End();
            ImGui.PopStyleVar(2);
        }
    }
}