using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using TASMod.Helpers;

namespace TASMod.Overlays
{
    public class ClayTileMap : IOverlay
    {
        public override string Name => "ClayMap";
        public override string Description => "displays clay tiles on the map";
        private uint NumHoed;
        private string LocationName;
        public int Depth = 3;
        public Dictionary<Vector2, List<int>> TileData;
        public List<List<Vector2>> Tiles;
        public Color TextColor = Color.White;

        public ClayTileMap()
        {
            NumHoed = uint.MaxValue;
            Tiles = new List<List<Vector2>>();
            TileData = new Dictionary<Vector2, List<int>>();
        }

        public override void ActiveUpdate()
        {
            var currentLocation = InstanceCurrentLocation.Get(ActiveInstance.InstanceIndex).Location;
            var stats = InstanceCurrentPlayer.Get(ActiveInstance.InstanceIndex).Player?.stats;
            if (currentLocation == null || stats == null || !Active) return;
            if (ShouldReset(currentLocation, stats))
            {
                Reset(currentLocation, stats, true);
            }
            else if (currentLocation.Name != LocationName)
            {
                Reset(currentLocation, stats, false);
            }
        }

        public override void ActiveDraw(SpriteBatch spriteBatch)
        {
            if (Tiles.Count == 0 || !Active) return;

            foreach (var tile in TileData)
            {
                DrawObjectText(ActiveInstance.InstanceIndex, spriteBatch, tile.Key, string.Join(",", tile.Value.Select(o => o.ToString())));
            }
        }

        public override void RenderImGui()
        {
            if (ImGui.CollapsingHeader("ClayMap"))
            {
                if (ImGui.InputInt("Depth", ref Depth))
                {
                    Depth = Math.Max(1, Depth);
                    Reset();
                }
                if (ImGui.Button("Reset"))
                {
                    Reset();
                }
            }
            base.RenderImGui();
        }

        public static bool IsTillable(GameLocation location, Vector2 tile)
        {
            return location.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Diggable", "Back") != null;
        }

        protected bool EvalTile(GameLocation currentLocation, Stats stats, Vector2 tile, int depth)
        {
            if (!IsTillable(currentLocation, tile)) return false;
            Random r = Utility.CreateDaySaveRandom(tile.X * 2000, tile.Y * 77, stats.DirtHoed + depth);
            GameLocation loc = currentLocation;
            if (!loc.IsFarm && loc.IsOutdoors && Game1.GetSeasonForLocation(loc).Equals("winter") && r.NextDouble() < 0.08 && !(loc is StardewValley.Locations.Desert))
            {
                return false;
            }
            return r.NextDouble() < 0.03;
        }

        protected bool ShouldReset(GameLocation currentLocation, Stats stats)
        {
            return stats.DirtHoed != NumHoed ||
                   (currentLocation != null &&
                   currentLocation.Name != LocationName);
        }

        private void DrawObjectText(int index, SpriteBatch spriteBatch, Vector2 tile, string text)
        {
            // DrawObjectSpriteAtTile(spriteBatch, tile, 330);
            DrawTextAtTile(index, spriteBatch, text, tile, TextColor, Color.Black);
        }

        private List<Vector2> BuildTiles(GameLocation currentLocation, Stats stats, int depth)
        {
            List<Vector2> tiles = new List<Vector2>();
            if (currentLocation == null || stats == null) return tiles;

            int layerHeight = currentLocation.map.Layers[0].LayerHeight;
            int layerWidth = currentLocation.map.Layers[0].LayerWidth;
            for (int x = 0; x < layerWidth; x++)
            {
                for (int y = 0; y < layerHeight; y++)
                {
                    Vector2 tile = new Vector2(x, y);
                    if (EvalTile(currentLocation, stats, tile, depth))
                    {
                        tiles.Add(tile);
                    }

                }
            }
            return tiles;
        }

        public void Reset(GameLocation currentLocation, Stats stats, bool rollover = false)
        {
            NumHoed = stats.DirtHoed;
            LocationName = currentLocation.Name;

            if (rollover)
            {
                // drop the current and append a new max depth set of tiles
                if (Tiles.Count > 0)
                    Tiles.RemoveAt(0);
                Tiles.Add(BuildTiles(currentLocation, stats, Depth - 1));
            }
            else
            {
                // rebuild all
                Tiles = new List<List<Vector2>>();
                for (int i = 0; i < Depth; ++i)
                {
                    Tiles.Add(BuildTiles(currentLocation, stats, i));
                }
            }
            TileData.Clear();
            for (int i = 0; i < Depth; i++)
            {
                if (i >= Tiles.Count) continue;
                foreach (var tile in Tiles[i])
                {
                    if (!TileData.ContainsKey(tile))
                    {
                        TileData[tile] = new List<int>();
                    }
                    TileData[tile].Add(i + 1);
                }
            }
        }
    }
}