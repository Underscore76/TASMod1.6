using System.Linq;
using ImGuiNET;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Monsters;
using StardewValley.Objects;
using StardewValley.Tools;
using TASMod.Simulators.TreeHit;

namespace TASMod.Overlays.Widgets
{
    public class TreeWidget
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
            if (farmer.CurrentTool is not Axe)
                return;

            var hit = TreeHit.Estimate(index);
            if (!hit.HitTree)
                return;
            DrawImpl(hit);
        }
        public static void DrawImpl(TreeHitInstance hit)
        {
            if (ImGui.CollapsingHeader("Tree"))
            {
                ImGui.Indent();
                ImGui.BeginTable("Weeds", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg);
                ImGui.TableSetupColumn("Elem", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableHeadersRow();

                ImGui.TableNextRow(); ImGui.TableSetColumnIndex(0); ImGui.Text("Woody"); ImGui.TableSetColumnIndex(1); ImGui.Text(hit.WoodySecret.ToString());
                // ImGui.TableNextRow(); ImGui.TableSetColumnIndex(0); ImGui.Text("Box"); ImGui.TableSetColumnIndex(1); ImGui.Text(hit.MysteryBox.ToString());
                // ImGui.TableNextRow(); ImGui.TableSetColumnIndex(0); ImGui.Text("Note"); ImGui.TableSetColumnIndex(1); ImGui.Text(hit.SecretNote.ToString());
                ImGui.TableNextRow(); ImGui.TableSetColumnIndex(0); ImGui.Text("Book"); ImGui.TableSetColumnIndex(1); ImGui.Text(hit.SkillBook);
                ImGui.TableNextRow(); ImGui.TableSetColumnIndex(0); ImGui.Text("Index"); ImGui.TableSetColumnIndex(1); ImGui.Text($"{hit.IndexAtSpawnBook}/{hit.IndexNeededSpawnBook}");

                ImGui.EndTable();
                ImGui.Unindent();
            }
        }
    }
}