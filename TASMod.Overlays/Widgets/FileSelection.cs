using Num = System.Numerics;
using ImGuiNET;
using TASMod.ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using TASMod.System;
using TASMod.Extensions;
using TASMod.Inputs;
using TASMod.Console;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using StardewValley.Minigames;
using StardewValley.Objects;
using ImGuiVector2 = System.Numerics.Vector2;
using TASMod.Overlays.Widgets;
using TASMod.Recording;
using TASMod.Patches;

namespace TASMod.Overlays.Widgets
{
    public class FileSelection
    {
        // File selection state
        public static List<string> availableFiles = new List<string>();
        public static bool filesLoaded = false;

        public static void Draw()
        {
            if (ImGui.CollapsingHeader("File Selection"))
            {
                if (ImGui.Button("Refresh File List") || !filesLoaded)
                {
                    RefreshFileList();
                }
                ImGui.SameLine();
                ImGui.Text($"({availableFiles.Count} files)");

                ImGui.Separator();

                // File list with load buttons
                if (availableFiles.Count > 0)
                {
                    ImGui.BeginChild("FileList", new Num.Vector2(0, 200));
                    for (int i = 0; i < availableFiles.Count; i++)
                    {
                        string fileName = availableFiles[i];
                        string displayName = Path.GetFileNameWithoutExtension(fileName);

                        ImGui.Text(displayName);
                        ImGui.SameLine();

                        // Load button
                        ImGui.PushID($"load_{i}");
                        if (ImGui.Button("Load"))
                        {
                            LoadFile(displayName, false);
                        }
                        ImGui.PopID();

                        ImGui.SameLine();

                        // Fast Load button
                        ImGui.PushID($"fastload_{i}");
                        if (ImGui.Button("Fast Load"))
                        {
                            LoadFile(displayName, true);
                        }
                        ImGui.PopID();
                    }
                    ImGui.EndChild();
                }
                else
                {
                    ImGui.TextColored(new Num.Vector4(0.6f, 0.6f, 0.6f, 1), "No save state files found");
                }
            }
        }
        public static void RefreshFileList()
        {
            availableFiles.Clear();
            try
            {
                if (Directory.Exists(Constants.SaveStatePath))
                {
                    var files = Directory.GetFiles(Constants.SaveStatePath, "*.json");
                    foreach (var file in files)
                    {
                        availableFiles.Add(Path.GetFileName(file));
                    }
                    availableFiles.Sort();
                }
                filesLoaded = true;
            }
            catch (Exception ex)
            {
                // Handle directory access errors gracefully
                Controller.Console.PushResult($"Error loading file list: {ex.Message}");
                filesLoaded = false;
            }
        }

        public static void LoadFile(string fileName, bool fastLoad)
        {
            try
            {
                if (fastLoad)
                {
                    Controller.Console.PushCommand($"fload {fileName}");
                }
                else
                {
                    Controller.Console.PushCommand($"load {fileName}");
                }
            }
            catch (Exception ex)
            {
                Controller.Console.PushResult($"Error loading file {fileName}: {ex.Message}");
            }
        }
    }
}