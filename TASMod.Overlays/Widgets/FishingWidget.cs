using System.Linq;
using ImGuiNET;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Monsters;
using StardewValley.Objects;
using StardewValley.Tools;
using TASMod.Helpers;
using TASMod.Simulators.Fishing;
using TASMod.Simulators.TreeHit;

namespace TASMod.Overlays.Widgets
{
    public class FishingWidget
    {
        public static void Draw(int index)
        {
            if (GameRunner.instance == null || GameRunner.instance.gameInstances.Count <= index)
                return;
            IClickableMenu menu = InstanceCurrentMenu.Get(index).Menu;
            if (menu == null || !(menu is BobberBar))
                return;

            DrawImpl(menu as BobberBar);
        }
        public static void DrawImpl(BobberBar bar)
        {
            if (ImGui.CollapsingHeader("Fishing"))
            {
                ImGui.Indent();
                SBobberBar state = new SBobberBar(bar);
                ImGui.SeparatorText("Fish State");
                ImGui.Text($"Pos: {state.bobberPosition}");
                ImGui.Text($"Target: {state.bobberTargetPosition}");
                ImGui.Text($"Speed: {state.bobberSpeed}");
                ImGui.SeparatorText("Bar State");
                ImGui.Text($"Pos: {state.bobberBarPos}");
                ImGui.Text($"Height: {state.bobberBarPos + state.bobberBarHeight}");
                ImGui.Text($"Speed: {state.bobberBarSpeed}");
                ImGui.Unindent();
            }
        }
    }
}