using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using TASMod.Helpers;
using TASMod.System;

namespace TASMod.Overlays
{
    public class TileGrid : IOverlay
    {
        public override string Name => "Grid";
        public override string Description => "draw tile grid lines";
        public Color gridColor = Color.Red;

        public override void ActiveDraw(SpriteBatch spriteBatch)
        {
            for (int i = 0; i < GameRunner.instance.gameInstances.Count; i++)
            {
                try
                {
                    DrawGridForInstance(i, spriteBatch);
                }
                catch (Exception e)
                {
                    ModEntry.Console.Log($"TileGrid2 ActiveDraw Exception: {e}", StardewModdingAPI.LogLevel.Error);
                }
            }
        }
        public void DrawGridForInstance(int i, SpriteBatch spriteBatch)
        {
            var viewport = InstanceViewport.Get(i).Viewport;
            int offsetX = (int)((viewport.X - Game1.tileSize) / Game1.tileSize) * Game1.tileSize;
            int offsetY = (int)((viewport.Y - Game1.tileSize) / Game1.tileSize) * Game1.tileSize;
            int xMin = viewport.X;
            int xMax = viewport.X + viewport.Width;
            int yMin = viewport.Y;
            int yMax = viewport.Y + viewport.Height;
            for (int x = offsetX; x <= xMax; x += Game1.tileSize)
            {
                DrawLineGlobal(i, spriteBatch, new Vector2(x, yMin), new Vector2(x, yMax), gridColor * 0.5f);
            }
            for (int y = offsetY; y <= yMax; y += Game1.tileSize)
            {
                DrawLineGlobal(i, spriteBatch, new Vector2(xMin, y), new Vector2(xMax, y), gridColor * 0.5f);
            }
        }
    }
}

