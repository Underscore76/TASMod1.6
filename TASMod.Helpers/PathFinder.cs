using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Extensions;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using xTile.Dimensions;
using xTile.ObjectModel;
using Object = StardewValley.Object;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace TASMod.Helpers
{
    public class PathFinder
    {
        public float getDefaultMovementSpeed(float temporarySpeedBuff)
        {
            var player = InstanceCurrentPlayer.Get(ActiveInstance.InstanceIndex).Player;
            // Farmer:getMovementSpeed
            float movementMultiplier = 0.066f;
            float movementSpeed = (
                (!player.isRidingHorse())
                    ? Math.Max(
                        1f,
                        (
                            (float)player.speed
                            + ((player.addedSpeed + temporarySpeedBuff))
                        )
                            * movementMultiplier
                            * (float)Game1.currentGameTime.ElapsedGameTime.Milliseconds
                    )
                    : Math.Max(
                        1f,
                        (
                            (float)player.speed
                            + (
                                (
                                    player.addedSpeed
                                    + 4.6f
                                    + (player.mount.ateCarrotToday ? 0.4f : 0f)
                                    + ((player.stats.Get("Book_Horse") != 0) ? 0.5f : 0f)
                                )
                            )
                        )
                            * movementMultiplier
                            * (float)Game1.currentGameTime.ElapsedGameTime.Milliseconds
                    )
            );
            return movementSpeed;
        }

        public float BaseSpeed => getDefaultMovementSpeed(0);
        public float GrassSpeed
        {
            get
            {
                var player = InstanceCurrentPlayer.Get(ActiveInstance.InstanceIndex).Player;
                if (player.stats.Get("Book_Grass") != 0)
                {
                    return getDefaultMovementSpeed(-0.33f);
                }
                else
                {
                    return getDefaultMovementSpeed(-1f);
                }
            }
        }

        public float CardinalWeight(bool isGrassOrCrop = false)
        {
            if (isGrassOrCrop)
                return 64 / GrassSpeed;
            return 64 / BaseSpeed;
        }

        public float DiagonalWeight(bool isGrassOrCrop = false)
        {
            if (isGrassOrCrop)
                return 64 / (GrassSpeed * 0.707f);
            return 64 / (BaseSpeed * 0.707f);
        }

        public const float toolWeight = 13;
        public const float weaponWeight = 6;
        public bool useTools = true;
        public GameLocation location;
        public AStar<Tile> solver;

        public bool hasPath => path != null && path.Count > 0;
        public List<Tile> path;
        public double cost;
        public int maxCost = 5000;

        public PathFinder()
        {
            solver = new AStar<Tile>(this.GetNeighbors, this.DistanceStep, this.DistanceHeuristic);
        }

        public bool IsValidVector2(int index, Vector2 vec)
        {
            return IsValid(index, new Tile() { X = (int)vec.X, Y = (int)vec.Y });
        }

        public void Update(int index, Tile start, Tile end, bool useTool = true)
        {
            var locationInfo = InstanceCurrentLocation.Get(index);
            location = locationInfo.Location;
            useTools = useTool;
            if (!IsValid(index, end))
            {
                path = null;
                return;
            }
            try
            {
                path = solver.Search(index, start, end, out cost, maxCost);
            }
            catch
            {
                path = null;
            }
        }

        public string GetToolString(Vector2 tile)
        {
            Object obj = location.getObjectAtTile((int)tile.X, (int)tile.Y);
            if (obj != null)
            {
                if (obj.Name.Contains("Stone"))
                {
                    return $"Pickaxe";
                }
                else if (obj.Name.Contains("Weed"))
                {
                    return "Weapon";
                }
                else if (obj.Name.Contains("Twig"))
                {
                    return "Axe";
                }
            }
            TerrainFeature tf;
            if (location.terrainFeatures.TryGetValue(tile, out tf))
            {
                if (!tf.isPassable() && tf is Tree tree)
                {
                    switch (tree.growthStage.Value)
                    {
                        case 1:
                        case 2:
                            return "Weapon";
                        default:
                            return "Axe";
                    }
                }
            }
            if (location is MineShaft mineShaft)
            {
                foreach (ResourceClump current in mineShaft.resourceClumps)
                {
                    Rectangle rect = new Rectangle(
                        (int)current.Tile.X,
                        (int)current.Tile.Y,
                        current.width.Value,
                        current.height.Value
                    );
                    if (rect.Intersects(new Rectangle((int)tile.X, (int)tile.Y, 1, 1)))
                    {
                        return "Pickaxe";
                    }
                }
            }
            else
            {
                foreach (ResourceClump current in location.resourceClumps)
                {
                    Rectangle rect = new Rectangle(
                        (int)current.Tile.X,
                        (int)current.Tile.Y,
                        current.width.Value,
                        current.height.Value
                    );
                    if (rect.Intersects(new Rectangle((int)tile.X, (int)tile.Y, 1, 1)))
                    {
                        switch (current.parentSheetIndex.Value)
                        {
                            case 600: // stump
                            case 602: // hollow log
                                return "Axe";
                            case 622: // meteorite
                            case 672: // boulder
                            case 752: // mines rocks
                            case 754: // mines rocks
                            case 756: // mines rocks
                            case 758: // mines rocks
                                return "Pickaxe";
                        }
                    }
                }
            }
            return "";
        }

        public bool HasValidTool(Farmer player, ResourceClump clump, out string tool)
        {
            tool = "";
            if (clump == null)
                return true;
            switch (clump.parentSheetIndex.Value)
            {
                case 600: // stump
                    if (player.Items.Any(i => i is Axe axe && axe.UpgradeLevel > 0))
                    {
                        tool = "Axe";
                        return true;
                    }
                    return false;
                case 602: // hollow log
                    if (player.Items.Any(i => i is Axe axe && axe.UpgradeLevel > 1))
                    {
                        tool = "Axe";
                        return true;
                    }
                    return false;
                case 622: // meteorite
                    if (player.Items.Any(i => i is Pickaxe pickaxe && pickaxe.UpgradeLevel > 2))
                    {
                        tool = "Pickaxe";
                        return true;
                    }
                    return false;
                case 672: // boulder
                    if (player.Items.Any(i => i is Pickaxe pickaxe && pickaxe.UpgradeLevel > 1))
                    {
                        tool = "Pickaxe";
                        return true;
                    }
                    return false;
                case 752: // mines rocks
                case 754: // mines rocks
                case 756: // mines rocks
                case 758: // mines rocks
                    if (player.Items.Any(i => i is Pickaxe))
                    {
                        tool = "Pickaxe";
                        return true;
                    }
                    return false;
            }
            return false;
        }

        public void Update(int index, int endX, int endY, bool useTool)
        {
            var playerInfo = InstanceCurrentPlayer.Get(index);
            Tile start = new Tile()
            {
                X = (int)playerInfo.CurrentTile.X,
                Y = (int)playerInfo.CurrentTile.Y
            };
            Tile end = new Tile() { X = endX, Y = endY };
            Update(index, start, end, useTool);
        }

        public void Update(int index, int startX, int startY, int endX, int endY, bool useTool)
        {
            Tile start = new Tile() { X = startX, Y = startY };
            Tile end = new Tile() { X = endX, Y = endY };
            Update(index, start, end, useTool);
        }

        public class Tile
        {
            public int X;
            public int Y;

            public override bool Equals(object obj)
            {
                if (obj == null || !(obj is Tile))
                    return false;
                return this.GetHashCode() == ((Tile)obj).GetHashCode();
            }

            public override int GetHashCode()
            {
                return X * 65535 + Y;
            }

            public Vector2 toVector2()
            {
                return new Vector2(X, Y);
            }
        }

        public IEnumerable<Tile> GetNeighbors(int index, Tile tile)
        {
            List<Tile> neighbors = new List<Tile>();
            for (int i = -1; i <= 1; ++i)
            {
                for (int j = -1; j <= 1; ++j)
                {
                    if (i == 0 && j == 0)
                        continue;
                    Tile newTile = new Tile() { X = tile.X + i, Y = tile.Y + j };
                    if (!IsValid(index, newTile))
                        continue;
                    if (i != 0 && j != 0)
                    {
                        if (
                            !IsValid(index, new Tile() { X = tile.X, Y = newTile.Y })
                            || !IsValid(index, new Tile() { X = newTile.X, Y = tile.Y })
                        )
                            continue;
                    }
                    neighbors.Add(newTile);
                }
            }
            return neighbors;
        }

        public bool IsValid(int index, Tile tile)
        {
            if (!location.isTileOnMap(tile.toVector2()))
                return false;
            Rectangle tileRect = new Rectangle(
                tile.X * Game1.tileSize,
                tile.Y * Game1.tileSize,
                Game1.tileSize,
                Game1.tileSize
            );
            foreach (LargeTerrainFeature current in location.largeTerrainFeatures)
            {
                Rectangle rect = current.getBoundingBox();
                if (rect.Intersects(tileRect))
                    return false;
            }
            var player = InstanceCurrentPlayer.Get(index).Player;
            if (location is MineShaft mineShaft)
            {
                foreach (ResourceClump current in mineShaft.resourceClumps)
                {
                    Rectangle rect = new Rectangle(
                        (int)(current.Tile.X * Game1.tileSize),
                        (int)(current.Tile.Y * Game1.tileSize),
                        current.width.Value * Game1.tileSize,
                        current.height.Value * Game1.tileSize
                    );
                    if (rect.Intersects(tileRect) && !HasValidTool(player, current, out _))
                    {
                        return useTools;
                    }
                }
            }
            if (location is Farm farm)
            {
                foreach (ResourceClump current in farm.resourceClumps)
                {
                    Rectangle rect = new Rectangle(
                        (int)(current.Tile.X * Game1.tileSize),
                        (int)(current.Tile.Y * Game1.tileSize),
                        current.width.Value * Game1.tileSize,
                        current.height.Value * Game1.tileSize
                    );
                    if (rect.Intersects(tileRect) && !HasValidTool(player, current, out _))
                    {
                        return useTools;
                    }
                }
                foreach (Building building in farm.buildings)
                {
                    if (building.intersects(tileRect) && !building.isTilePassable(tile.toVector2()))
                        return false;
                }
            }
            if (location.overlayObjects.ContainsKey(tile.toVector2()))
            {
                Object obj = location.overlayObjects[tile.toVector2()];
                return obj.isPassable();
            }
            if (location.Objects.ContainsKey(tile.toVector2()))
            {
                Object obj = location.Objects[tile.toVector2()];
                if (!obj.isDebrisOrForage())
                    return false;
                return true;
            }
            // check furniture
            if (location.GetFurnitureAt(tile.toVector2()) is Furniture f)
            {
                if (f.IntersectsForCollision(tileRect) && !f.isPassable())
                    return false;
            }
            // check layer properties
            if (!location.isTilePassable(tile.toVector2()))
                return false;
            // allow bridges
            if (location.doesTileHaveProperty(tile.X, tile.Y, "Passable", "Buildings") == "T")
            {
                var result = location.doesTileHaveProperty(tile.X, tile.Y, "Passable", "Back");
                if (result == null || result == "T")
                    return true;
            }
            var viewport = InstanceViewport.Get(index);
            tileRect = new Rectangle(tile.X * Game1.tileSize + 24, tile.Y * Game1.tileSize + 24, 16, 16);
            if (location.isCollidingPosition(tileRect, viewport.Viewport, true, 0, false, Game1.player))
            {
                return false;
            }
            return true;
        }

        public double DistanceStep(int index, Tile start, Tile end)
        {
            double toolCost;
            double baseWeight;
            bool isGrassOrCrop = false;
            if (location.terrainFeatures.TryGetValue(end.toVector2(), out TerrainFeature tf))
            {
                if (tf is Grass || tf is HoeDirt)
                    isGrassOrCrop = true;
            }
            if (start.X == end.X || start.Y == end.Y) // cardinal motion
            {
                baseWeight =
                    (Math.Abs(start.X - end.X) + Math.Abs(start.Y - end.Y))
                    * CardinalWeight(isGrassOrCrop);
                toolCost = GetToolCost(index, location, new List<Tile> { end });
            }
            else
            {
                baseWeight =
                    Math.Max(Math.Abs(start.X - end.X), Math.Abs(start.Y - end.Y))
                    * DiagonalWeight(isGrassOrCrop);
                toolCost = GetToolCost(
                    index,
                    location,
                    new List<Tile>
                    {
                        new Tile() { X = end.X, Y = end.Y },
                        new Tile() { X = end.X, Y = start.Y },
                        new Tile() { X = start.X, Y = end.Y },
                    }
                );
            }
            return baseWeight + toolCost;
        }

        public double DistanceHeuristic(int index, Tile start, Tile end)
        {
            int tilesDiagonal,
                tilesCardinal;
            if (Math.Abs(start.X - end.X) < Math.Abs(start.Y - end.Y))
            {
                tilesDiagonal = Math.Abs(end.X - start.X);
                tilesCardinal = Math.Abs(end.Y - start.Y) - tilesDiagonal;
            }
            else
            {
                tilesDiagonal = Math.Abs(end.Y - start.Y);
                tilesCardinal = Math.Abs(end.X - start.X) - tilesDiagonal;
            }
            return tilesDiagonal * DiagonalWeight() + tilesCardinal * CardinalWeight();
        }

        /*
        public Func<T, IEnumerable<T>> GetNeighbors = null;
        public Func<T, bool> IsValid = null;
        public Func<T, T, double> DistanceStep;
        public Func<T, T, double> DistanceHeuristic;
        */

        private double GetToolCost(int index, GameLocation location, List<Tile> tiles)
        {
            var player = InstanceCurrentPlayer.Get(index).Player;
            double weight = 0;
            foreach (var tile in tiles)
            {
                Object obj = location.getObjectAtTile(tile.X, tile.Y);
                if (!useTools && obj != null && !obj.isPassable())
                    return double.NaN;
                bool featureExists = location.terrainFeatures.TryGetValue(
                    tile.toVector2(),
                    out TerrainFeature feature
                );
                if (!useTools && featureExists && !feature.isPassable(player))
                    return double.NaN;
                ResourceClump clump = null;
                foreach (ResourceClump current in location.resourceClumps)
                {
                    Rectangle rect = new Rectangle(
                        (int)current.Tile.X,
                        (int)current.Tile.Y,
                        current.width.Value,
                        current.height.Value
                    );
                    if (rect.Intersects(new Rectangle(tile.X, tile.Y, 1, 1)))
                    {
                        clump = current;
                        break;
                    }
                }
                weight += GetToolCost(obj, feature, clump);
            }
            return weight;
        }

        public static double GetToolCost(Object obj, TerrainFeature tf, ResourceClump clump)
        {
            double weight = 0;
            if (obj != null)
            {
                if (obj.Name.Contains("Stone"))
                {
                    weight = obj.MinutesUntilReady * toolWeight;
                }
                else if (obj.Name.Contains("Weed"))
                {
                    weight = weaponWeight;
                }
                else if (obj.Name.Contains("Twig"))
                {
                    weight = toolWeight;
                }
            }
            if (tf != null && !tf.isPassable())
            {
                if (tf is Tree tree)
                {
                    switch (tree.growthStage.Value)
                    {
                        case 1:
                        case 2:
                            weight += weaponWeight;
                            break;
                        case 3:
                            weight += (tree.health.Value / 2) * toolWeight;
                            break;
                        case 5:
                            weight += (tree.health.Value / 1) * toolWeight;
                            break;
                        default:
                            weight += (tree.health.Value / 1) * toolWeight;
                            break;
                    }
                }
            }
            if (clump != null)
            {
                weight += 10 * toolWeight;
            }
            return weight;
        }

        public Tile PeekFront()
        {
            if (path == null || path.Count == 0)
                return null;
            return path[0];
        }

        public Tile PeekBack()
        {
            if (path == null || path.Count == 0)
                return null;
            return path[path.Count - 1];
        }

        public Tile PopFront()
        {
            if (path == null || path.Count == 0)
                return null;
            Tile front = path[0];
            path.RemoveAt(0);
            return front;
        }

        public void Reset()
        {
            location = null;
            path = null;
            cost = 0;
        }
    }
}
