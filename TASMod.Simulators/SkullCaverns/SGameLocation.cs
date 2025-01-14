using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Extensions;
using xTile;
using xTile.Tiles;

namespace TASMod.Simulators.SkullCaverns
{
    public class SSGameLocation
    {
        public List<SNPC> Characters = new List<SNPC>();
        public Dictionary<Vector2, string> Objects = new Dictionary<Vector2, string>();
        public Dictionary<Vector2, string> TerrainFeatures = new Dictionary<Vector2, string>();
        public Map map;

        public bool hasTileAt(int x, int y, string layer, string tilesheetId = null)
        {
            return map?.HasTileAt(x, y, layer, tilesheetId) ?? false;
        }

        public int getTileIndexAt(
            xTile.Dimensions.Location p,
            string layer,
            string tilesheetId = null
        )
        {
            return map?.GetTileIndexAt(p.X, p.Y, layer, tilesheetId) ?? (-1);
        }

        public int getTileIndexAt(int x, int y, string layer, string tilesheetId = null)
        {
            return map?.GetTileIndexAt(x, y, layer, tilesheetId) ?? (-1);
        }

        public bool isTileOnMap(Vector2 position)
        {
            if (
                position.X >= 0f
                && position.X < (float)map.Layers[0].LayerWidth
                && position.Y >= 0f
            )
            {
                return position.Y < (float)map.Layers[0].LayerHeight;
            }

            return false;
        }

        public bool isTileOnMap(Point tile)
        {
            return isTileOnMap(tile.X, tile.Y);
        }

        public bool isTileOnMap(int x, int y)
        {
            if (x >= 0 && x < map.Layers[0].LayerWidth && y >= 0)
            {
                return y < map.Layers[0].LayerHeight;
            }

            return false;
        }

        public virtual string doesTileHaveProperty(
            int xTile,
            int yTile,
            string propertyName,
            string layerName,
            bool ignoreTileSheetProperties = false
        )
        {
            if (map != null)
            {
                Tile tile2 = map.GetLayer(layerName)?.Tiles[xTile, yTile];
                if (tile2 != null)
                {
                    if (tile2.Properties.TryGetValue(propertyName, out var value))
                    {
                        return value;
                    }

                    if (
                        !ignoreTileSheetProperties
                        && tile2.TileIndexProperties.TryGetValue(propertyName, out value)
                    )
                    {
                        return value;
                    }
                }
            }

            return null;
        }

        public virtual bool isTilePlaceable(Vector2 v, bool itemIsPassable = false)
        {
            if (!hasTileAt((int)v.X, (int)v.Y, "Back"))
            {
                return false;
            }

            string text = doesTileHaveProperty((int)v.X, (int)v.Y, "NoFurniture", "Back");
            if (text != null)
            {
                if (text == "total")
                {
                    return false;
                }

                if (!itemIsPassable || !Game1.currentLocation.IsOutdoors)
                {
                    return false;
                }
            }

            return true;
        }

        public virtual bool IsTileOccupiedBy(
            Vector2 tile,
            CollisionMask collisionMask = CollisionMask.All,
            CollisionMask ignorePassables = CollisionMask.None,
            bool useFarmerTile = false
        )
        {
            Microsoft.Xna.Framework.Rectangle value = new Microsoft.Xna.Framework.Rectangle(
                (int)tile.X * 64,
                (int)tile.Y * 64,
                64,
                64
            );
            if (
                collisionMask.HasFlag(CollisionMask.Farmers)
                && !ignorePassables.HasFlag(CollisionMask.Farmers)
            )
            {
                // foreach (Farmer farmer in farmers)
                // {
                //     if (
                //         useFarmerTile
                //             ? (farmer.Tile == tile)
                //             : farmer.GetBoundingBox().Intersects(value)
                //     )
                //     {
                //         return true;
                //     }
                // }
            }

            if (
                collisionMask.HasFlag(CollisionMask.Objects)
                && Objects.TryGetValue(tile, out var value2)
                && (!ignorePassables.HasFlag(CollisionMask.Objects))
            )
            {
                return true;
            }

            if (collisionMask.HasFlag(CollisionMask.Furniture)) { }

            if (collisionMask.HasFlag(CollisionMask.Characters))
            {
                foreach (SNPC character in Characters)
                {
                    if (
                        character != null
                        && character.GetBoundingBox().Intersects(value)
                        && !character.IsInvisible
                        && (
                            !ignorePassables.HasFlag(CollisionMask.Characters)
                            || !character.farmerPassesThrough
                        )
                    )
                    {
                        return true;
                    }
                }
            }

            if (
                collisionMask.HasFlag(CollisionMask.LocationSpecific)
                && IsLocationSpecificOccupantOnTile(tile)
            )
            {
                return true;
            }

            if (collisionMask.HasFlag(CollisionMask.Buildings)) { }

            return false;
        }

        public virtual bool IsLocationSpecificOccupantOnTile(Vector2 tileLocation)
        {
            return false;
        }

        public virtual bool CanItemBePlacedHere(
            Vector2 tile,
            bool itemIsPassable = false,
            CollisionMask collisionMask = CollisionMask.All,
            CollisionMask ignorePassables = ~CollisionMask.Objects,
            bool useFarmerTile = false,
            bool ignorePassablesExactly = false
        )
        {
            if (!ignorePassablesExactly)
            {
                ignorePassables &= ~CollisionMask.Objects;
                if (!itemIsPassable)
                {
                    ignorePassables &= ~(CollisionMask.Characters | CollisionMask.Farmers);
                }
            }

            if (!isTileOnMap(tile))
            {
                return false;
            }

            if (!isTilePlaceable(tile, itemIsPassable))
            {
                return false;
            }

            if (IsTileBlockedBy(tile, collisionMask, ignorePassables, useFarmerTile))
            {
                return false;
            }

            return true;
        }

        public virtual bool IsTileBlockedBy(
            Vector2 tile,
            CollisionMask collisionMask = CollisionMask.All,
            CollisionMask ignorePassables = CollisionMask.None,
            bool useFarmerTile = false
        )
        {
            if (!IsTileOccupiedBy(tile, collisionMask, ignorePassables, useFarmerTile))
            {
                return !isTilePassable(tile);
            }

            return true;
        }

        public bool isTilePassable(Vector2 tileLocation)
        {
            Tile tile = map.RequireLayer("Back").Tiles[(int)tileLocation.X, (int)tileLocation.Y];
            if (tile != null && tile.TileIndexProperties.ContainsKey("Passable"))
            {
                return false;
            }

            Tile tile2 = map.RequireLayer("Buildings").Tiles[
                (int)tileLocation.X,
                (int)tileLocation.Y
            ];
            if (
                tile2 != null
                && !tile2.TileIndexProperties.ContainsKey("Shadow")
                && !tile2.TileIndexProperties.ContainsKey("Passable")
            )
            {
                return false;
            }

            return true;
        }

        public virtual bool CanSpawnCharacterHere(Vector2 tileLocation)
        {
            if (isTileOnMap(tileLocation) && isTilePlaceable(tileLocation))
            {
                return !IsTileBlockedBy(tileLocation);
            }

            return false;
        }
    }
}
