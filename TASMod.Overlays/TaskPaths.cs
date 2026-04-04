using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using TASMod.Helpers;
using TASMod.Scripting;
using TASMod.System;

namespace TASMod.Overlays
{
    public class TaskPaths : IOverlay
    {
        public override string Name => "TaskPaths";
        public override string Description => "draw controller task paths";
        public Color gridColor = new Color(0, 255, 255, 255);
        public Color toolColor = Color.Yellow;
        public override void ActiveDraw(SpriteBatch spriteBatch)
        {
            for (int i = 1; i < GameRunner.instance.gameInstances.Count; i++)
            {
                try
                {
                    DrawForInstance(i, spriteBatch);
                }
                catch (Exception e)
                {
                    ModEntry.Console.Log($"TaskPaths ActiveDraw Exception: {e}", StardewModdingAPI.LogLevel.Error);
                }
            }
        }
        public void DrawForInstance(int i, SpriteBatch spriteBatch)
        {
            if (!FrameTasks.StagedTasks.ContainsKey(i))
                return;
            var tasks = FrameTasks.StagedTasks[i];
            var startPos = InstanceCurrentPlayer.Get(i).CurrentTile;
            Dictionary<Vector2, int> visitCount = new Dictionary<Vector2, int>();
            for (int t = 0; t < tasks.Count; t++)
            {
                if (tasks[t].Type == "Walk")
                {
                    DrawLineBetweenTiles(i, spriteBatch, startPos, tasks[t].Tile, gridColor, thickness: 2);
                    startPos = tasks[t].Tile;
                }
                else if (tasks[t].Type == "Tool")
                {
                    if (visitCount.TryGetValue(tasks[t].Tile, out int count))
                    {
                        visitCount[tasks[t].Tile] = count + 1;
                    }
                    else
                    {
                        visitCount[tasks[t].Tile] = 1;
                    }
                }
            }
            foreach (var kvp in visitCount)
            {
                Vector2 tile = kvp.Key;
                int count = kvp.Value;
                Color color;
                switch (count)
                {
                    case 1:
                        color = Color.Yellow;
                        color.A = 128;
                        DrawFilledTile(i, spriteBatch, tile, color);
                        break;
                    case 2:
                        color = Color.Orange;
                        color.A = 192;
                        DrawFilledTile(i, spriteBatch, tile, color);
                        break;
                    default:
                        color = Color.Red;
                        color.A = 192;
                        DrawFilledTile(i, spriteBatch, tile, color);
                        break;
                }
            }
        }
    }
}

