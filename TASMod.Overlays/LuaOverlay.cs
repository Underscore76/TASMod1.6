using System.Collections.Generic;
using ImGuiNET;
using NLua;
using TASMod.Scripting;

namespace TASMod.Overlays
{
    public class LuaOverlay : IOverlay
    {
        public static Dictionary<string, LuaFunction> LuaData = new Dictionary<string, LuaFunction>();
        public static Dictionary<string, LuaFunction> LuaButtons = new Dictionary<string, LuaFunction>();

        public override string Name => "LuaOverlay";

        public override string Description => "Allows register";

        public static void AddData(string name, LuaFunction func)
        {
            LuaData[name] = func;
        }

        public static void AddButton(string name, LuaFunction func)
        {
            LuaButtons[name] = func;
        }

        public override void RenderImGui()
        {
            if (ImGui.CollapsingHeader("Lua Scripts"))
            {
                if (LuaEngine.LuaState == null)
                {
                    ImGui.Text("Lua engine not initialized");
                    return;
                }
                ImGui.SeparatorText("Lua Watchlist");
                ImGui.BeginTable("LuaScripts", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg);
                ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("X").X+10);
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableHeadersRow();

                List<string> toRemove = new List<string>();
                foreach (var script in LuaData)
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.PushID("LuaScripts##"+script.Key);
                    if (ImGui.Button("X"))
                    {
                        toRemove.Add(script.Key);
                    }
                    ImGui.PopID();
                    object[] objects = script.Value.Call();
                    if (objects.Length == 0)
                    {
                        ImGui.TableSetColumnIndex(1);
                        ImGui.Text(script.Key);
                        ImGui.TableSetColumnIndex(2);
                        ImGui.Text("nil");
                        continue;
                    }
                    string value = objects[0].ToString();
                    ImGui.TableSetColumnIndex(1);
                    ImGui.Text(script.Key);
                    ImGui.TableSetColumnIndex(2);
                    ImGui.Text(value);
                }
                ImGui.EndTable();
                foreach (var key in toRemove)
                {
                    LuaData.Remove(key);
                }

                ImGui.SeparatorText("Run Function");
                foreach (var script in LuaButtons)
                {
                    if (ImGui.Button(script.Key))
                    {
                        script.Value.Call();
                    }
                }
            }
        }
    }
}