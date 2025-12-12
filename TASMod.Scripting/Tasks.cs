using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Netcode;
using NLua;
using NLua.Exceptions;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using TASMod.Console;
using TASMod.Extensions;
using TASMod.Helpers;
using TASMod.Inputs;
using TASMod.Minigames;
using TASMod.Networking;
using TASMod.Recording;
using TASMod.Simulators;
using TASMod.Simulators.SkullCaverns;
using TASMod.System;
using TASMod.Views;

namespace TASMod.Scripting
{
    public class FrameTask
    {
        public string Type;
        public Vector2 Tile;

        public override string ToString()
        {
            return $"{Type}({Tile.X},{Tile.Y})";
        }
    }
    public class FrameTasks
    {
        public static Dictionary<int, List<FrameTask>> StagedTasks = new Dictionary<int, List<FrameTask>>();
        public static void PushLeft(int playerIndex, string type, Vector2 tile)
        {
            if (!StagedTasks.ContainsKey(playerIndex))
            {
                StagedTasks[playerIndex] = new List<FrameTask>();
            }
            StagedTasks[playerIndex].Insert(0, new FrameTask { Type = type, Tile = tile });
        }
        public static void PushRight(int playerIndex, string type, Vector2 tile)
        {
            if (!StagedTasks.ContainsKey(playerIndex))
            {
                StagedTasks[playerIndex] = new List<FrameTask>();
            }
            StagedTasks[playerIndex].Add(new FrameTask { Type = type, Tile = tile });
        }
        public static int Count(int playerIndex)
        {
            if (StagedTasks.ContainsKey(playerIndex))
            {
                return StagedTasks[playerIndex].Count;
            }
            return 0;
        }
        public static FrameTask PopLeft(int playerIndex)
        {
            if (Count(playerIndex) > 0)
            {
                var task = StagedTasks[playerIndex][0];
                StagedTasks[playerIndex].RemoveAt(0);
                return task;
            }
            return null;
        }
        public static FrameTask PopRight(int playerIndex)
        {
            if (Count(playerIndex) > 0)
            {
                var index = StagedTasks[playerIndex].Count - 1;
                var task = StagedTasks[playerIndex][index];
                StagedTasks[playerIndex].RemoveAt(index);
                return task;
            }
            return null;
        }
        public static FrameTask Peek(int playerIndex)
        {
            if (Count(playerIndex) > 0)
            {
                return StagedTasks[playerIndex][0];
            }
            return null;
        }
        public static FrameTask Get(int playerIndex, int taskIndex)
        {
            if (Count(playerIndex) > taskIndex)
            {
                return StagedTasks[playerIndex][taskIndex];
            }
            return null;
        }
        public static void RemoveAt(int playerIndex, int taskIndex)
        {
            if (Count(playerIndex) > taskIndex)
            {
                StagedTasks[playerIndex].RemoveAt(taskIndex);
            }
        }
        public static void Clear(int playerIndex)
        {
            if (StagedTasks.ContainsKey(playerIndex))
            {
                StagedTasks[playerIndex].Clear();
            }
        }
        public static void ClearAll()
        {
            StagedTasks.Clear();
        }

        public static void Draw(int index)
        {
            if (Count(index) > 0)
            {
                ImGui.Text("Staged Tasks:");
                ImGui.Indent();
                int removeAt = -1;
                for (int i = 0; i < StagedTasks[index].Count; i++)
                {
                    ImGui.Text($"- {StagedTasks[index][i]}");
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("Click to remove this task");
                        if (ImGui.IsMouseClicked(0))
                        {
                            removeAt = i;
                        }
                    }
                }
                if (removeAt >= 0)
                {
                    RemoveAt(index, removeAt);
                }
                ImGui.Unindent();
            }
        }
    }
}