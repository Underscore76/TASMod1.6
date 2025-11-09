using ImGuiNET;
using ImGuiVector2 = System.Numerics.Vector2;
using ImGuiVector4 = System.Numerics.Vector4;
using TASMod.Overlays.Widgets;
using TASMod.Recording;
using TASMod.System;
using TASMod.Extensions;
using TASMod.Inputs;
using TASMod.Patches;
using TASMod.Scripting;
using System.Collections.Generic;

namespace TASMod.Overlays.Widgets
{
    public class ControllerWindow
    {
        public static void Draw(int index)
        {
            ImGui.SetNextWindowPos(new ImGuiVector2(10, 10 + (index - 1) * 150), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSize(new ImGuiVector2(300, 140), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowBgAlpha(0.9f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 5.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 3.0f);
            ImGui.Begin($"Controller {index}");
            if (ImGui.IsWindowFocused())
            {
                ActiveInstance.InstanceIndex = index;
            }
            TASGamePadState con = TASInputState.GetTASGamePadState(index);
            bool hasInput = GamePadInputQueue.HasInput(index);

            // Input status display with inline clear button
            if (hasInput)
            {
                ImGui.TextColored(new ImGuiVector4(0, 1, 0, 1), "+ Has Queued Input");
            }
            else
            {
                ImGui.TextColored(new ImGuiVector4(0.6f, 0.6f, 0.6f, 1), "o No Queued Input");
            }
            ImGui.SameLine();
            if (!hasInput) ImGui.BeginDisabled();
            if (ImGui.Button("Clear##input"))
            {
                GamePadInputQueue.ClearQueue(index);
            }
            if (!hasInput) ImGui.EndDisabled();

            // Frame function status display with inline clear button
            bool hasCoroutine = GamePadInputQueue.HasPlayerCoroutine(index);

            if (hasCoroutine)
            {
                ImGui.TextColored(new ImGuiVector4(0, 1, 0, 1), "+ Has Frame Function");
            }
            else
            {
                ImGui.TextColored(new ImGuiVector4(0.6f, 0.6f, 0.6f, 1), "o No Frame Function");
            }
            ImGui.SameLine();
            if (!hasCoroutine) ImGui.BeginDisabled();
            if (ImGui.Button("Clear##function"))
            {
                GamePadInputQueue.ClearPlayerCoroutine(index);
            }
            if (!hasCoroutine) ImGui.EndDisabled();

            if (hasCoroutine)
            {
                LuaCoroutine coroutine = GamePadInputQueue.GetPlayerCoroutine(index);
                ImGui.Text($"Name: {coroutine?.Name}");
                if (ImGui.IsItemHovered())
                {
                    string description = coroutine?.Description;
                    if (!string.IsNullOrEmpty(description))
                    {
                        ImGui.SetTooltip(description);
                    }
                }
            }
            ImGui.Separator();

            FrameTasks.Draw(index);
            ImGui.Separator();

            // Function assignment section
            if (ImGui.CollapsingHeader("Assign Function"))
            {
                var namedFunctions = GamePadInputQueue.FrameFunctionNames;
                if (namedFunctions.Count > 0)
                {
                    ImGui.Text("Available Functions:");
                    foreach (var functionName in namedFunctions)
                    {
                        var frameFunction = GamePadInputQueue.GetFunctionByName(functionName);

                        if (ImGui.Button($"Assign '{functionName}'"))
                        {
                            GamePadInputQueue.SetPlayerCoroutine(index, functionName);
                        }

                        // Show description as tooltip
                        if (ImGui.IsItemHovered() && !string.IsNullOrEmpty(frameFunction.Description))
                        {
                            ImGui.SetTooltip(frameFunction.Description);
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
            PlayerWidget.Draw(index);
            MinesWidget.Draw(index);
            WeedsWidget.Draw(index);
            TreeWidget.Draw(index);
            ImGui.End();
            ImGui.PopStyleVar(2);
        }
    }
}