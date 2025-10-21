using ImGuiNET;
using ImGuiVector2 = System.Numerics.Vector2;
using ImGuiVector4 = System.Numerics.Vector4;
using TASMod.Patches;
using TASMod.Inputs;

namespace TASMod.Overlays.Widgets
{
    public class KeyboardMouseWindow
    {

        public static void Draw(int index)
        {
            ImGui.SetNextWindowPos(new ImGuiVector2(10, 10 + (index - 1) * 150), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSize(new ImGuiVector2(300, 140), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowBgAlpha(0.9f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 5.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 3.0f);
            ImGui.Begin($"Controller {index}", ImGuiWindowFlags.AlwaysAutoResize);
            if (ImGui.IsWindowFocused())
            {
                ActiveInstance.InstanceIndex = index;
            }

            if (AutomationManager.AppliedLogic != null)
            {
                ImGui.TextColored(new ImGuiVector4(0, 1, 0, 1), $"+ Queued: {AutomationManager.AppliedLogic}");
            }
            else
            {
                ImGui.TextColored(new ImGuiVector4(0.6f, 0.6f, 0.6f, 1), "o No Queued Input");
            }

            ImGui.SeparatorText("Last Frame Input");
            if (Controller.State.FrameStates.Count > 0)
            {
                ImGui.Text("Left Click: " + Controller.LastFrameMouse().LeftMouseClicked);
                ImGui.Text("Right Click: " + Controller.LastFrameMouse().RightMouseClicked);
                ImGui.Text($"Mouse Position: {Controller.LastFrameMouse().MouseX},{Controller.LastFrameMouse().MouseY}");
                string keys = string.Join(",", Controller.State.FrameStates.Last().keyboardState);
                ImGui.Text("Keyboard: " + keys);
            }
            if (TextBoxInput.GetSelected() != null)
            {
                // var helper = OverlayManager.Get<TextBoxHelper>();
                string text = TextBoxInput.Text;
                ImGui.InputTextMultiline("Multiline", ref text, 100000, new ImGuiVector2(200, 100));
                TextBoxInput.Text = text;
            }
            else
            {
                TextBoxInput.Text = "";
            }
            PlayerWidget.Draw(index);
            MinesWidget.Draw(index);
            WeedsWidget.Draw(index);
            TreeWidget.Draw(index);
            ImGui.End();
            ImGui.PopStyleVar(2);
        }
    }
}