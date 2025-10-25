using System.Linq;
using ImGuiNET;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Monsters;
using StardewValley.Objects;
using TASMod.Simulators.EnemyKill;
using TASMod.Simulators.GemNode;

namespace TASMod.Overlays.Widgets
{
    public class MinesWidget
    {
        public static void Draw(int index)
        {
            if (GameRunner.instance == null || GameRunner.instance.gameInstances.Count <= index)
                return;
            Farmer farmer = Reflector.GetValue(
                GameRunner.instance.gameInstances[index].staticVarHolder,
                "Game1__player") as Farmer;
            if (farmer == null)
                return;
            GameLocation loc = GameRunner.instance.gameInstances[index].instanceGameLocation;
            if (loc is MineShaft mine)
            {
                Draw(index, farmer, mine);
            }
        }
        public static void Draw(int index, Farmer farmer, MineShaft mine)
        {
            if (ImGui.CollapsingHeader("MineShaft"))
            {
                ImGui.Text($"Tile: {farmer.Tile.X},{farmer.Tile.Y}");
                ImGui.Indent();
                if (ImGui.CollapsingHeader("Enemies"))
                {
                    ImGui.BeginTable("Enemies", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg);
                    ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("HP", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("Tile", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("Items", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableHeadersRow();
                    for (int i = 0; i < mine.characters.Count; i++)
                    {
                        var character = mine.characters[i];
                        if (character is Monster monster)
                        {
                            ImGui.TableNextRow();
                            ImGui.TableSetColumnIndex(0);
                            ImGui.Text(monster.Name);
                            ImGui.TableSetColumnIndex(1);
                            ImGui.Text($"{monster.Health}/{monster.MaxHealth}");
                            ImGui.TableSetColumnIndex(2);
                            ImGui.Text($"{monster.Tile.X},{monster.Tile.Y}");
                            ImGui.TableSetColumnIndex(3);
                            ImGui.Text(string.Join(", ", monster.objectsToDrop));
                        }
                    }
                    ImGui.EndTable();
                }
                if (ImGui.CollapsingHeader("Monster Kill"))
                {
                    ImGui.BeginTable("SpawnRareObject", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg);
                    ImGui.TableSetupColumn("Item", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("Hit", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("Actual", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("Needed", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("Diff", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableHeadersRow();
                    var hit = EnemyKill.Estimate(index);

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text("Ladder");
                    ImGui.TableSetColumnIndex(1);
                    ImGui.Text(hit.Ladder.ToString());

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text("Book");
                    ImGui.TableSetColumnIndex(1);
                    ImGui.Text(hit.RareItem);
                    ImGui.TableSetColumnIndex(2);
                    ImGui.Text(hit.IndexAtSpawnRare.ToString());
                    ImGui.TableSetColumnIndex(3);
                    ImGui.Text(hit.IndexNeededSpawnRare.ToString());
                    ImGui.TableSetColumnIndex(4);
                    ImGui.Text((hit.IndexNeededSpawnRare - hit.IndexAtSpawnRare).ToString());

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text("Void");
                    ImGui.TableSetColumnIndex(1);
                    ImGui.Text(hit.VoidBook.ToString());
                    ImGui.TableSetColumnIndex(2);
                    ImGui.Text(hit.IndexAtVoidBook.ToString());
                    ImGui.TableSetColumnIndex(3);
                    ImGui.Text(hit.IndexNeededVoidBook.ToString());
                    ImGui.TableSetColumnIndex(4);
                    ImGui.Text((hit.IndexNeededVoidBook - hit.IndexAtVoidBook).ToString());

                    ImGui.EndTable();
                }
                if (ImGui.CollapsingHeader("Gem Nodes"))
                {
                    ImGui.BeginTable("Gem Nodes", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg);
                    ImGui.TableSetupColumn("Tile", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("Current", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("Ladder", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableHeadersRow();
                    var hit = GemNode.Estimate(index);
                    for (int i = 0; i < hit.CurrentGems.Count; i++)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        ImGui.Text(hit.Tile.X + "," + hit.Tile.Y);
                        ImGui.TableSetColumnIndex(1);
                        ImGui.Text(hit.CurrentGems[i]);
                        ImGui.TableSetColumnIndex(2);
                        ImGui.Text(hit.LadderGems[i]);
                    }
                    ImGui.EndTable();
                }
                ImGui.Unindent();
            }
        }
    }
}