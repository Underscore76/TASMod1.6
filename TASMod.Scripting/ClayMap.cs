using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using xTile.Dimensions;
using xTile.Tiles;

namespace TASMod.Scripting
{
    public static class TileInfo
    {
        public static bool IsTillable(GameLocation location, Vector2 tile)
        {
            return location.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Diggable", "Back")
                != null;
        }

        public static bool IsOccupied(GameLocation location, Vector2 tile, bool careObjects = true)
        {
            // impassable tiles (e.g. water)
            if (!location.isTilePassable(new Location((int)tile.X, (int)tile.Y), Game1.viewport))
                return true;

            if (careObjects)
            {
                // objects & large terrain features
                if (
                    location.objects.ContainsKey(tile)
                    || location.largeTerrainFeatures.Any(p => p.Tile == tile)
                )
                    return true;
            }
            return false;
        }
    }

    public class ClayMap
    {
        private bool EvalTile(GameLocation location, Vector2 tile)
        {
            if (!TileInfo.IsTillable(location, tile))
                return false;
            if (TileInfo.IsOccupied(location, tile))
                return false;

            Random r = new Random(
                ((int)tile.X) * 2000
                    + ((int)tile.Y) * 77
                    + (int)Game1.uniqueIDForThisGame / 2
                    + (int)Game1.stats.DaysPlayed
                    + (int)Game1.stats.DirtHoed
            );
            if (
                !location.IsFarm
                && location.IsOutdoors
                && Game1.GetSeasonForLocation(location).Equals("winter")
                && r.NextDouble() < 0.08
                && !(location is StardewValley.Locations.Desert)
            )
            {
                return false;
            }
            return r.NextDouble() < 0.03;
        }

        public List<Vector2> GetClayTiles()
        {
            List<Vector2> tiles = new List<Vector2>();
            if (Game1.currentLocation == null || Game1.stats == null)
                return tiles;
            int layerHeight = Game1.currentLocation.Map.Layers[0].LayerHeight;
            int layerWidth = Game1.currentLocation.Map.Layers[0].LayerWidth;
            for (int y = 0; y < layerHeight; y++)
            {
                for (int x = 0; x < layerWidth; x++)
                {
                    Vector2 tile = new Vector2(x, y);
                    if (EvalTile(Game1.currentLocation, tile))
                    {
                        tiles.Add(tile);
                    }
                }
            }
            return tiles;
        }
    }
}
