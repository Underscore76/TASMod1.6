using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.TerrainFeatures;
using xTile.Dimensions;

namespace TASMod.Overlays
{
    public class PointSource
    {
        public Vector2 Position;
        public float value;
        public float decay;
    }

    public class TileWeight
    {
        public Vector2 Position;
        public float mapWeight;
        public float energyWeight;
        public float distWeight;
        public float structuralWeight;

        public float TotalWeight => mapWeight * energyWeight * distWeight * structuralWeight;

        public override string ToString()
        {
            return $"TileWeight({Position}, {TotalWeight} = {mapWeight} * {energyWeight} * {distWeight} * {structuralWeight})";
        }
    }

    public class MapWeights : IOverlay
    {
        public override string Name => "MapWeights";

        public override string Description =>
            "Displays the weights of the farm map for planting purposes";

        public bool BreakoutWeights = false;

        public bool ValidTile(GameLocation location, Vector2 tile)
        {
            if (location.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Diggable", "Back") == null)
                return false;
            if (!location.isTilePassable(tile))
                return false;

            return true;
        }

        public List<PointSource> pointSources = new List<PointSource>()
        {
            new PointSource()
            {
                Position = new Vector2(64, 15),
                value = 1,
                decay = 0.95f
            },
            new PointSource()
            {
                Position = new Vector2(70, 28),
                value = 0.5f,
                decay = 0.85f
            },
        };

        public List<TileWeight> tileWeights;
        public float MaxWeight = 0;

        public void UpdateMapWeights(Farm farm)
        {
            // float maxWeight = 0;
            float totalSource = 0;
            foreach (PointSource source in pointSources)
            {
                totalSource += source.value;
            }
            foreach (TileWeight weight in tileWeights)
            {
                weight.mapWeight = 0;

                foreach (PointSource source in pointSources)
                {
                    Vector2 pos = source.Position - weight.Position;
                    if (source.Position == weight.Position)
                    {
                        weight.mapWeight += source.value;
                    }
                    else
                    {
                        float shell = Math.Max(Math.Abs(pos.X), Math.Abs(pos.Y));
                        float value = source.value * (float)Math.Pow(source.decay, shell);
                        weight.mapWeight += value;
                    }
                }
            }
            foreach (TileWeight weight in tileWeights)
            {
                weight.mapWeight /= totalSource;
            }
        }

        public void UpdateDistWeights(Farm farm, Farmer player)
        {
            Vector2 position = player.Tile;
            foreach (TileWeight weight in tileWeights)
            {
                Vector2 pos = (weight.Position - position);
                float shell = Math.Max(Math.Abs(pos.X), Math.Abs(pos.Y));
                weight.distWeight = 1f * (float)Math.Pow(0.8, shell);
            }
        }

        public float WEED_COST = 0.1f;
        public float STONE_COST = 0.3f;
        public float TWIG_COST = 0.3f;
        public float TREE_COST = 0.8f;
        public float CLUMP_COST = 1f;

        public float EnergyCost(Farm farm, Farmer player, Vector2 position)
        {
            if (farm.objects.TryGetValue(position, out var obj))
            {
                if (obj.IsBreakableStone())
                {
                    return STONE_COST;
                }
                else if (obj.IsTwig())
                {
                    return TWIG_COST;
                }
                else if (obj.IsWeeds())
                {
                    return WEED_COST;
                }
                else
                {
                    return 1f;
                }
            }
            if (farm.terrainFeatures.TryGetValue(position, out var feature))
            {
                if (feature is Tree tree)
                {
                    // public const int seedStage = 0;
                    // public const int sproutStage = 1;
                    // public const int saplingStage = 2;
                    // public const int bushStage = 3;
                    switch (tree.growthStage.Value)
                    {
                        case 0:
                            return TWIG_COST;
                        case 1:
                        case 2:
                            return WEED_COST;
                        case 3:
                            return (TWIG_COST + TREE_COST) / 2;
                        case 4:
                        case 5:
                            return TREE_COST;
                        default:
                            return TREE_COST;
                    }
                }
            }

            foreach (var clump in farm.resourceClumps)
            {
                if (clump.occupiesTile((int)position.X, (int)position.Y))
                {
                    Tool axe,
                        pickaxe;
                    switch (clump.parentSheetIndex.Value)
                    {
                        // stump
                        case 600:
                            axe = player.getToolFromName("Axe");
                            if (axe != null && axe.UpgradeLevel >= 1)
                            {
                                return TWIG_COST;
                            }
                            else
                            {
                                return CLUMP_COST;
                            }
                        // hollow log
                        case 602:
                            axe = player.getToolFromName("Axe");
                            if (axe != null && axe.UpgradeLevel >= 2)
                            {
                                return TWIG_COST;
                            }
                            else
                            {
                                return CLUMP_COST;
                            }
                        case 148:
                        case 622:
                            pickaxe = player.getToolFromName("Pickaxe");
                            if (pickaxe != null && pickaxe.UpgradeLevel >= 3)
                            {
                                return STONE_COST;
                            }
                            else
                            {
                                return CLUMP_COST;
                            }
                        case 672:
                            pickaxe = player.getToolFromName("Pickaxe");
                            if (pickaxe != null && pickaxe.UpgradeLevel >= 2)
                            {
                                return STONE_COST;
                            }
                            else
                            {
                                return CLUMP_COST;
                            }
                    }
                }
            }
            return 0;
        }

        public void UpdateEnergyWeights(Farm farm)
        {
            foreach (TileWeight weight in tileWeights)
            {
                weight.energyWeight = 1 - EnergyCost(farm, Game1.player, weight.Position);
            }
        }

        public List<TileWeight> BuildWeights(Farm farm)
        {
            int width = farm.map.Layers[0].LayerWidth;
            int height = farm.map.Layers[0].LayerHeight;
            List<TileWeight> weights = new List<TileWeight>();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!ValidTile(farm, new Vector2(x, y)))
                        continue;
                    weights.Add(
                        new TileWeight()
                        {
                            Position = new Vector2(x, y),
                            mapWeight = float.NaN,
                            energyWeight = float.NaN,
                            distWeight = float.NaN,
                            structuralWeight = float.NaN
                        }
                    );
                }
            }
            return weights;
        }

        // compute tile grid
        public enum TileType
        {
            EMPTY,
            HOEDIRT,
            CROP,
            SPRINKLER,
        }

        private static Vector2 UP = new Vector2(0, -1);
        private static Vector2 DOWN = new Vector2(0, 1);
        private static Vector2 LEFT = new Vector2(-1, 0);
        private static Vector2 RIGHT = new Vector2(1, 0);
        private static Vector2[] directions = new Vector2[] { UP, DOWN, LEFT, RIGHT };

        public bool IsAdjacentToHoeDirt(Farm farm, Vector2 tile)
        {
            foreach (var dir in directions)
            {
                Vector2 pos = tile + dir;
                if (farm.terrainFeatures.TryGetValue(pos, out var feature))
                {
                    if (feature is HoeDirt dirt)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool FormsInternalCorner(Grid grid, Vector2 tile)
        {
            // would make an empty tile have at least 2 adjacent hoedirt tiles
            foreach (var dir in directions)
            {
                Vector2 pos = tile + dir;
                if (!grid.InBounds(pos))
                {
                    continue;
                }
                if (grid.Get(pos) == TileType.EMPTY)
                {
                    // how many adjacent hoedirt tiles are there to this empty tile?
                    int adjCount = 0;
                    foreach (var dir2 in directions)
                    {
                        Vector2 pos2 = pos + dir2;
                        if (!grid.InBounds(pos2))
                        {
                            continue;
                        }
                        if (grid.Get(pos2) == TileType.HOEDIRT || grid.Get(pos2) == TileType.CROP)
                        {
                            adjCount++;
                        }
                    }
                    if (adjCount >= 1)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool ClosesInternalCorner(Grid grid, Vector2 tile)
        {
            // would fill in an empty tile that has at least 2 adjacent hoedirt tiles
            int baseCount = 0;
            foreach (var dir in directions)
            {
                Vector2 pos = tile + dir;
                if (!grid.InBounds(pos))
                {
                    continue;
                }
                if (grid.Get(pos) == TileType.HOEDIRT || grid.Get(pos) == TileType.CROP)
                {
                    baseCount++;
                }
            }
            if (baseCount >= 2)
            {
                return true;
            }
            return false;
        }

        public bool ReducesAspectRatio(Grid grid, Vector2 tile)
        {
            // would make the aspect ratio more like a square
            // need to floodfill to find the current associated region of tiles so far filled in
            // define the min/max aspect ratio of the region
            Vector2 minCoords = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 maxCoords = new Vector2(float.MinValue, float.MinValue);
            HashSet<Vector2> visited = new HashSet<Vector2>();
            Queue<Vector2> queue = new Queue<Vector2>();
            queue.Enqueue(tile);
            while (queue.Count > 0)
            {
                Vector2 pos = queue.Dequeue();
                if (visited.Contains(pos))
                {
                    continue;
                }
                visited.Add(pos);
                if (pos != tile)
                {
                    minCoords = Vector2.Min(minCoords, pos);
                    maxCoords = Vector2.Max(maxCoords, pos);
                }
                foreach (var dir in directions)
                {
                    Vector2 newPos = pos + dir;
                    if (
                        grid.InBounds(newPos) && grid.Get(newPos) == TileType.HOEDIRT
                        || grid.Get(newPos) == TileType.CROP
                    )
                    {
                        queue.Enqueue(newPos);
                    }
                }
            }
            int width = Math.Abs((int)(maxCoords.X - minCoords.X));
            int height = Math.Abs((int)(maxCoords.Y - minCoords.Y));
            int diff = Math.Abs(width - height);
            maxCoords = Vector2.Max(maxCoords, tile);
            minCoords = Vector2.Min(minCoords, tile);
            int newWidth = Math.Abs((int)(maxCoords.X - minCoords.X));
            int newHeight = Math.Abs((int)(maxCoords.Y - minCoords.Y));
            int newDiff = Math.Abs(newWidth - newHeight);
            return newDiff < diff;
        }

        public struct Grid
        {
            public int Width;
            public int Height;
            public List<List<TileType>> grid;
            public List<Vector2> sprinklerTiles;

            public Grid(int width, int height)
            {
                Width = width;
                Height = height;
                grid = new List<List<TileType>>();
                for (int y = 0; y < height; y++)
                {
                    grid.Add(new List<TileType>());
                    for (int x = 0; x < width; x++)
                    {
                        grid[y].Add(TileType.EMPTY);
                    }
                }
                sprinklerTiles = new List<Vector2>();
            }

            public Grid(GameLocation location)
                : this(location.Map.Layers[0].LayerWidth, location.Map.Layers[0].LayerHeight) { }

            public void Set(int x, int y, TileType type)
            {
                grid[y][x] = type;
            }

            public void Set(Vector2 pos, TileType type)
            {
                grid[(int)pos.Y][(int)pos.X] = type;
            }

            public void AddSprinklerTiles(List<Vector2> tiles)
            {
                sprinklerTiles.AddRange(tiles);
            }

            public TileType Get(int x, int y)
            {
                return grid[y][x];
            }

            public TileType Get(Vector2 pos)
            {
                return grid[(int)pos.Y][(int)pos.X];
            }

            public bool CoveredBySprinkler(Vector2 pos)
            {
                return sprinklerTiles.Contains(pos);
            }

            public bool InBounds(Vector2 pos)
            {
                return pos.X >= 0 && pos.Y >= 0 && pos.X < Width && pos.Y < Height;
            }

            public bool InBounds(int x, int y)
            {
                return x >= 0 && y >= 0 && x < Width && y < Height;
            }
        }

        public Grid BuildGrid(Farm farm)
        {
            Grid grid = new Grid(farm.map.Layers[0].LayerWidth, farm.map.Layers[0].LayerHeight);
            for (int y = 0; y < farm.map.Layers[0].LayerHeight; y++)
            {
                for (int x = 0; x < farm.map.Layers[0].LayerWidth; x++)
                {
                    if (farm.terrainFeatures.TryGetValue(new Vector2(x, y), out var feature))
                    {
                        if (feature is HoeDirt dirt)
                        {
                            if (dirt.crop == null || dirt.crop.dead.Value || dirt.readyForHarvest())
                            {
                                grid.Set(x, y, TileType.HOEDIRT);
                            }
                            else
                            {
                                grid.Set(x, y, TileType.CROP);
                            }
                        }
                    }
                    else if (farm.objects.TryGetValue(new Vector2(x, y), out var obj))
                    {
                        int radius = obj.GetBaseRadiusForSprinkler();
                        if (radius >= 0)
                        {
                            grid.AddSprinklerTiles(obj.GetSprinklerTiles());
                            grid.Set(x, y, TileType.SPRINKLER);
                        }
                    }
                }
            }
            return grid;
        }

        public void UpdateStructuralWeights(Farm farm)
        {
            Grid grid = BuildGrid(farm);
            foreach (TileWeight weight in tileWeights)
            {
                TileType type = grid.Get(weight.Position);
                if (type == TileType.SPRINKLER || type == TileType.CROP)
                {
                    weight.structuralWeight = 0;
                    continue;
                }
                if (type == TileType.HOEDIRT)
                {
                    weight.structuralWeight = 1;
                    continue;
                }

                // need to figure out basic structure stuff
                // is this within range of a sprinkler?
                if (grid.CoveredBySprinkler(weight.Position))
                {
                    weight.structuralWeight = 0.85f;
                }
                // is this next to an existing hoedirt
                else if (IsAdjacentToHoeDirt(farm, weight.Position))
                {
                    // would this form a contiguous region of crops?
                    bool formsInternalCorner = FormsInternalCorner(grid, weight.Position);
                    bool closesInternalCorner = ClosesInternalCorner(grid, weight.Position);
                    bool reducesAspectRatio = ReducesAspectRatio(grid, weight.Position);
                    if (closesInternalCorner)
                    {
                        weight.structuralWeight = 0.9f;
                    }
                    else if (reducesAspectRatio)
                    {
                        weight.structuralWeight = 0.75f;
                    }
                    else if (formsInternalCorner)
                    {
                        weight.structuralWeight = 0.5f;
                    }
                    else
                    {
                        weight.structuralWeight = 0.25f;
                    }
                }
                else
                {
                    // kinda just open space that could have a crop planted
                    weight.structuralWeight = 0.25f;
                }
            }
        }

        public TileWeight GetTileWeight(Vector2 tile)
        {
            foreach (TileWeight weight in tileWeights)
            {
                if (weight.Position == tile)
                {
                    return weight;
                }
            }
            return null;
        }

        public override void ActiveUpdate()
        {
            Farm farm = Game1.currentLocation as Farm;
            if (farm == null)
                return;
            if (tileWeights == null)
            {
                tileWeights = BuildWeights(farm);
            }
            UpdateMapWeights(farm);
            UpdateDistWeights(farm, Game1.player);
            UpdateEnergyWeights(farm);
            UpdateStructuralWeights(farm);

            MaxWeight = 0;
            for (int i = 0; i < tileWeights.Count; i++)
            {
                TileWeight weight = tileWeights[i];
                if (weight.TotalWeight > MaxWeight)
                {
                    MaxWeight = weight.TotalWeight;
                }
            }
        }

        public override void ActiveDraw(SpriteBatch b)
        {
            Farm farm = Game1.currentLocation as Farm;
            if (farm == null || tileWeights == null)
                return;

            foreach (var weight in tileWeights)
            {
                if (BreakoutWeights)
                {
                    if (!float.IsNaN(weight.mapWeight))
                    {
                        DrawTileQuadrant(
                            b,
                            weight.Position,
                            Color.Red,
                            Quadrant.TOP_LEFT,
                            weight.mapWeight
                        );
                    }
                    if (!float.IsNaN(weight.distWeight))
                    {
                        DrawTileQuadrant(
                            b,
                            weight.Position,
                            Color.Green,
                            Quadrant.TOP_RIGHT,
                            weight.distWeight
                        );
                    }
                    if (!float.IsNaN(weight.energyWeight))
                    {
                        DrawTileQuadrant(
                            b,
                            weight.Position,
                            Color.Blue,
                            Quadrant.BOTTOM_LEFT,
                            weight.energyWeight
                        );
                    }
                    if (!float.IsNaN(weight.structuralWeight))
                    {
                        DrawTileQuadrant(
                            b,
                            weight.Position,
                            Color.Purple,
                            Quadrant.BOTTOM_RIGHT,
                            weight.structuralWeight
                        );
                    }
                }
                else
                {
                    Color c = (weight.TotalWeight >= MaxWeight) ? Color.Green : Color.White;

                    DrawTileQuadrant(b, weight.Position, c, Quadrant.TOP_LEFT, weight.TotalWeight);
                }
            }
        }
    }
}
