using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

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
            if (Game1.currentLocation == null || Game1.stats == null || !Active) return;
            if (ShouldReset())
            {
                Reset(true);
            }
            else if (Game1.currentLocation.Name != LocationName)
            {
                Reset(false);
            }
        }

        public override void ActiveDraw(SpriteBatch spriteBatch)
        {
            if (Tiles.Count == 0 || !Active) return;

            // for (int i = 0; i < Depth; ++i)
            // {
            //     if (Tiles.Count <= i) continue;
            //     foreach (Vector2 tile in Tiles[i])
            //     {
            //         DrawObjectText(spriteBatch, tile, i > 0 ? i.ToString() : "");
            //     }
            // }
            foreach (var tile in TileData)
            {
                DrawObjectText(spriteBatch, tile.Key, string.Join(",", tile.Value.Select(o => o.ToString())));
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

        protected bool EvalTile(Vector2 tile, int depth)
        {
            if (!IsTillable(Game1.currentLocation, tile)) return false;
            Random r = Utility.CreateDaySaveRandom(tile.X * 2000, tile.Y * 77, Game1.stats.DirtHoed + depth);
            GameLocation loc = Game1.currentLocation;
            if (!loc.IsFarm && loc.IsOutdoors && Game1.GetSeasonForLocation(loc).Equals("winter") && r.NextDouble() < 0.08 && !(loc is StardewValley.Locations.Desert))
            {
                return false;
            }
            return r.NextDouble() < 0.03;
        }

        protected bool ShouldReset()
        {
            return Game1.stats.DirtHoed != NumHoed ||
                   (Game1.currentLocation != null &&
                   Game1.currentLocation.Name != LocationName);
        }

        private void DrawObjectText(SpriteBatch spriteBatch, Vector2 tile, string text)
        {
            // DrawObjectSpriteAtTile(spriteBatch, tile, 330);
            DrawTextAtTile(spriteBatch, text, tile, TextColor, Color.Black);
        }

        private List<Vector2> BuildTiles(int depth)
        {
            List<Vector2> tiles = new List<Vector2>();
            if (Game1.currentLocation == null || Game1.stats == null) return tiles;

            int layerHeight = Game1.currentLocation.map.Layers[0].LayerHeight;
            int layerWidth = Game1.currentLocation.map.Layers[0].LayerWidth;
            for (int x = 0; x < layerWidth; x++)
            {
                for (int y = 0; y < layerHeight; y++)
                {
                    Vector2 tile = new Vector2(x, y);
                    if (EvalTile(tile, depth))
                    {
                        tiles.Add(tile);
                    }

                }
            }
            return tiles;
        }

        public void Reset(bool rollover = false)
        {
            NumHoed = Game1.stats.DirtHoed;
            LocationName = Game1.currentLocation.Name;

            if (rollover)
            {
                // drop the current and append a new max depth set of tiles
                if (Tiles.Count > 0)
                    Tiles.RemoveAt(0);
                Tiles.Add(BuildTiles(Depth - 1));
            }
            else
            {
                // rebuild all
                Tiles = new List<List<Vector2>>();
                for (int i = 0; i < Depth; ++i)
                {
                    Tiles.Add(BuildTiles(i));
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