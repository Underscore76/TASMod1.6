using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley;
using TASMod.Helpers;
using TASMod.Helpers.Pathing;

namespace TASMod.Overlays
{
    public class RegionMap : IOverlay
    {
        public override string Name => "RegionMap";
        public override string Description => "draw region map";

        public string last_Name = "";
        public int last_numObjects = 0;
        public int last_numTerrainFeatures = 0;
        public int last_numLargeTerrainFeatures = 0;

        public static int GridSize = 7;
        public Map PathMap = new(GridSize);
        public List<Vector2> path = new();
        public void GeneratePath(Vector2 start, Vector2 end)
        {
            path = PathMap.GetTilePath(start, end);
        }
        public override void ActiveUpdate()
        {
            var loc = InstanceCurrentLocation.Get(0);
            var player = InstanceCurrentPlayer.Get(0);
            if (!loc.Active || !player.Active)
                return;

            if (last_Name != loc.Name
                || last_numObjects != loc.Location.Objects.Length
                || last_numTerrainFeatures != loc.Location.terrainFeatures.Length
                || last_numLargeTerrainFeatures != loc.Location.largeTerrainFeatures.Count
                || GridSize != PathMap.GridSize)
            {
                last_Name = loc.Name;
                last_numObjects = loc.Location.Objects.Length;
                last_numTerrainFeatures = loc.Location.terrainFeatures.Length;
                last_numLargeTerrainFeatures = loc.Location.largeTerrainFeatures.Count;
                PathMap.GridSize = GridSize;

                PathMap.Build(loc.Location, collapse: false);
            }
        }
        public override void ActiveDraw(SpriteBatch spriteBatch)
        {
            var viewport = InstanceViewport.Get(0);
            foreach (var r in PathMap.Regions)
            {
                var rect = new Rectangle(
                    (int)(r.Bounds.Left * Game1.tileSize),
                    (int)(r.Bounds.Top * Game1.tileSize),
                     r.Bounds.Width * Game1.tileSize,
                     r.Bounds.Height * Game1.tileSize
                );
                if (!TransformToLocal(0, rect).Intersects(viewport.Window))
                    continue;
                Color color = r.Walkable ? Color.Green * 0.5f : Color.Red * 0.5f;
                foreach (var tile in r.tiles)
                {
                    DrawFilledTile(0, spriteBatch, tile, color);
                }
            }
            foreach (var pair in PathMap.EdgeList)
            {
                Rectangle rect = new(
                    (int)(pair.Key.X * Game1.tileSize),
                    (int)(pair.Key.Y * Game1.tileSize),
                    pair.Key.Horizontal ? (int)(pair.Key.Length * Game1.tileSize) : 2,
                    pair.Key.Horizontal ? 2 : (int)(pair.Key.Length * Game1.tileSize)
                );
                if (!TransformToLocal(0, rect).Intersects(viewport.Window))
                    continue;
                DrawTileEdge(spriteBatch, pair.Key, Color.Blue);
            }
        }
        public void DrawTileEdge(SpriteBatch spriteBatch, Edge edge, Color color)
        {
            Vector2 start = new(edge.X, edge.Y);
            Vector2 end = edge.Horizontal ? new(edge.X + edge.Length, edge.Y) : new(edge.X, edge.Y + edge.Length);
            DrawLineGlobal(0, spriteBatch, start * Game1.tileSize, end * Game1.tileSize, color, 2);
        }
    }
}