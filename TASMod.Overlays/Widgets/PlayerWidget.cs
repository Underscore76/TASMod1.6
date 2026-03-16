using ImGuiNET;
using StardewValley;
using StardewValley.Objects;

namespace TASMod.Overlays.Widgets
{
    public class PlayerWidget
    {
        public static void Draw(int index)
        {
            if (ImGui.CollapsingHeader("Player Info"))
            {
                if (GameRunner.instance == null || GameRunner.instance.gameInstances.Count <= index)
                    return;
                Farmer farmer;
                if (GameRunner.instance.gameInstances.Count > 1)
                {
                    farmer = Reflector.GetValue(
                    GameRunner.instance.gameInstances[index].staticVarHolder,
                    "Game1__player") as Farmer;
                }
                else
                {
                    farmer = Game1.player;
                }
                if (farmer == null)
                    return;
                ImGui.Text($"Tile: {farmer.Tile.X},{farmer.Tile.Y}");
                ImGui.Indent();
                if (ImGui.CollapsingHeader("Inventory"))
                {
                    for (int i = 0; i < farmer.Items.Count; i++)
                    {
                        var item = farmer.Items[i];
                        if (item == null) continue;
                        if (item is Furniture furniture)
                        {
                            ImGui.Text($"{i}: {furniture.Name} (dir: {furniture.GetSittingDirection()})");
                        }
                        else if (item is Tool tool)
                        {
                            ImGui.Text($"{i}: {tool.Name}");
                        }
                        else
                        {
                            ImGui.Text($"{i}: {item.Name} x{item.Stack}");
                        }
                    }
                }
                ImGui.Unindent();
            }
        }
    }
}