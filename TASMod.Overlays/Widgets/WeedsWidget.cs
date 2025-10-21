using System.Linq;
using System.Numerics;
using ImGuiNET;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Monsters;
using StardewValley.Objects;
using StardewValley.Tools;
using TASMod.Simulators.CutWeed;
using TASMod.Simulators.EnemyKill;

namespace TASMod.Overlays.Widgets
{
    public class WeedsWidget
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
            if (!(farmer.CurrentTool is MeleeWeapon))
                return;
            DrawImpl(index);
        }
        public static void DrawImpl(int index)
        {
            if (ImGui.CollapsingHeader("Weeds"))
            {
                var hit = CutWeed.Estimate(index);
                ImGui.Indent();
                ImGui.BeginTable("Weeds", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg);
                ImGui.TableSetupColumn("Item", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Number", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Seed", ImGuiTableColumnFlags.WidthFixed, 30);
                ImGui.TableHeadersRow();

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text("Weeds");
                ImGui.TableSetColumnIndex(1);
                ImGui.Text(hit.NumWeeds.ToString());
                ImGui.TableSetColumnIndex(2);

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text("Fiber");
                ImGui.TableSetColumnIndex(1);
                ImGui.Text(hit.NumFiber.ToString());
                ImGui.TableSetColumnIndex(2);

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text("Mixed Seeds");
                ImGui.TableSetColumnIndex(1);
                ImGui.Text(hit.NumMixedSeeds.ToString());
                ImGui.TableSetColumnIndex(2);
                if (hit.NumMixedSeeds > 0)
                {
                    var drawList = ImGui.GetWindowDrawList();
                    var pos = ImGui.GetCursorScreenPos();
                    drawList.AddRectFilled(
                        pos,
                        new Vector2(pos.X + 20, pos.Y + ImGui.GetTextLineHeight()),
                        ImGui.ColorConvertFloat4ToU32(new Vector4(0, 1, 0, 1))
                    );
                }

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text("Flower");
                ImGui.TableSetColumnIndex(1);
                ImGui.Text(hit.NumMixedFlowerSeeds.ToString());
                ImGui.TableSetColumnIndex(2);

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text("Hats");
                ImGui.TableSetColumnIndex(1);
                ImGui.Text(hit.NumHats.ToString());
                ImGui.TableSetColumnIndex(2);

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text("Quartz");
                ImGui.TableSetColumnIndex(1);
                ImGui.Text(hit.NumQuartz.ToString());
                ImGui.TableSetColumnIndex(2);

                ImGui.EndTable();
                ImGui.Unindent();
            }
        }
    }
}