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
            foreach (var r in PathMap.Regions)
            {
                Color color = r.Walkable ? Color.Green * 0.5f : Color.Red * 0.5f;
                foreach (var tile in r.tiles)
                {
                    DrawFilledTile(0, spriteBatch, tile, color);
                }
                DrawTextAtTile(0, spriteBatch, r.Id.ToString(), r.tiles.First(), Color.White, color);
            }
            foreach (var pair in PathMap.EdgeList)
            {
                DrawEdge(spriteBatch, pair.Key, pair.Value);
            }
        }
        public void DrawEdge(SpriteBatch spriteBatch, Edge edge, List<Region> regions)
        {
            DrawTileEdge(spriteBatch, edge, Color.Blue);
            if (regions.Count > 2 || regions.Count == 0)
            {
                Console.Trace($"Edge {edge} has {regions.Count} regions");
            }
            string text = string.Join(",", regions.Select(r => r.Id));
            Vector2 edgeCenter = new Vector2(edge.X + (edge.Horizontal ? edge.Length / 2f : 0), edge.Y + (edge.Horizontal ? 0 : edge.Length / 2f)) * Game1.tileSize;
            DrawTextGlobal(0, spriteBatch, text, edgeCenter, Color.White, Color.Black);
        }
        public void DrawTileEdge(SpriteBatch spriteBatch, Edge edge, Color color)
        {
            Vector2 start = new(edge.X, edge.Y);
            Vector2 end = edge.Horizontal ? new(edge.X + edge.Length, edge.Y) : new(edge.X, edge.Y + edge.Length);
            DrawLineGlobal(0, spriteBatch, start * Game1.tileSize, end * Game1.tileSize, color, 2);
        }
    }
}