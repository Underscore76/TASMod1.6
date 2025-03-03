using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace TASMod.Simulators
{
    public class ClayPattern
    {
        public static int EXISTING_CLAY_COST = 30;
        public static int PREV_SAME_TILE_COST = 30;
        public static int DISTANCE_COST = 10;
        public static int MAX_DISTANCE = 20;
        public struct Element
        {
            public int X;
            public int Y;
            public int d;

            public Vector2 ToVector()
            {
                return new Vector2(X, Y);
            }

            public override readonly bool Equals([NotNullWhen(true)] object obj)
            {
                if (obj is Element e)
                {
                    return X == e.X && Y == e.Y && d == e.d;
                }
                return false;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(X, Y, d);
            }

            public int Distance(Element other)
            {
                int dx = Math.Abs(X - other.X);
                int dy = Math.Abs(Y - other.Y);
                return Math.Max(dx * dx, dy * dy) * DISTANCE_COST;
            }
        }

        public static List<Vector2> GetPathIgnore(int depth, bool trees, bool debris)
        {
            if (Game1.currentLocation == null || Game1.stats == null) return null;

            List<List<Element>> tiles = new();
            for (int i = 0; i < depth; ++i)
            {
                tiles.Add(BuildTiles(i, trees, debris));
            }
            Element start = new Element { X = (int)Game1.player.Tile.X, Y = (int)Game1.player.Tile.Y, d = 0 };
            List<Element> path = Dijkstra(tiles, start);
            return path.Select(t => t.ToVector()).ToList();
        }

        public static List<Vector2> GetPath(int depth)
        {
            if (Game1.currentLocation == null || Game1.stats == null) return null;

            List<List<Element>> tiles = new();
            for (int i = 0; i < depth; ++i)
            {
                tiles.Add(BuildTiles(i, false, false));
            }
            Element start = new Element { X = (int)Game1.player.Tile.X, Y = (int)Game1.player.Tile.Y, d = 0 };
            List<Element> path = Dijkstra(tiles, start);
            return path.Select(t => t.ToVector()).ToList();
        }

        protected static bool EvalTile(Element tile, int depth, bool trees, bool debris)
        {
            if (!IsTillable(Game1.currentLocation, tile)) return false;
            if (!Game1.currentLocation.isTilePassable(tile.ToVector())) return false;
            if (debris && Game1.currentLocation.Objects.ContainsKey(tile.ToVector())) return false;
            if (trees
                && Game1.currentLocation.terrainFeatures.ContainsKey(tile.ToVector())
                && Game1.currentLocation.terrainFeatures[tile.ToVector()] is Tree tree
                && tree.growthStage.Value >= 3)
            {
                return false;
            }
            foreach (var clump in Game1.currentLocation.resourceClumps)
            {
                if (clump.occupiesTile(tile.X, tile.Y))
                {
                    return false;
                }
            }

            Random r = Utility.CreateDaySaveRandom(tile.X * 2000, tile.Y * 77, Game1.stats.DirtHoed + depth);
            GameLocation loc = Game1.currentLocation;
            if (!loc.IsFarm && loc.IsOutdoors && Game1.GetSeasonForLocation(loc).Equals("winter") && r.NextDouble() < 0.08 && !(loc is StardewValley.Locations.Desert))
            {
                return false;
            }
            return r.NextDouble() < 0.03;
        }

        public static bool IsTillable(GameLocation location, Element tile)
        {
            return location.doesTileHaveProperty(tile.X, tile.Y, "Diggable", "Back") != null;
        }

        private static List<Element> BuildTiles(int depth, bool trees, bool debris)
        {
            Vector2 player = Game1.player.Tile;
            List<Element> tiles = new List<Element>();
            if (Game1.currentLocation == null || Game1.stats == null) return tiles;

            int layerHeight = Game1.currentLocation.map.Layers[0].LayerHeight;
            int layerWidth = Game1.currentLocation.map.Layers[0].LayerWidth;
            for (int x = 0; x < layerWidth; x++)
            {
                for (int y = 0; y < layerHeight; y++)
                {
                    Element tile = new Element { X = x, Y = y, d = depth + 1 };
                    int dist = (int)Math.Max(Math.Abs(x - player.X), Math.Abs(y - player.Y));
                    if (dist > MAX_DISTANCE)
                    {
                        continue;
                    }
                    if (EvalTile(tile, depth, trees, debris))
                    {
                        tiles.Add(tile);
                    }

                }
            }
            return tiles;
        }

        public static List<Element> Dijkstra(List<List<Element>> tiles, Element start)
        {
            PriorityQueue<Element, int> queue = new();
            Dictionary<Element, int> distances = new();
            Dictionary<Element, Element> prev = new();
            HashSet<Vector2> currentClay = new();
            foreach (var tf in Game1.currentLocation.terrainFeatures.Values)
            {
                if (tf is StardewValley.TerrainFeatures.HoeDirt dirt)
                {
                    currentClay.Add(dirt.Tile);
                }
            }
            queue.Enqueue(start, 0);
            distances[start] = 0;
            while (queue.TryDequeue(out Element current, out int dist))
            {
                if (current.d >= tiles.Count)
                    continue;
                if (dist > distances[current])
                    continue;
                foreach (Element move in tiles[current.d])
                {
                    int cost = dist + current.Distance(move);
                    if (currentClay.Contains(move.ToVector()))
                    {
                        cost += EXISTING_CLAY_COST;
                    }
                    if (move.ToVector() == current.ToVector())
                    {
                        cost += PREV_SAME_TILE_COST;
                    }
                    queue.Enqueue(move, cost);
                    if (!prev.ContainsKey(move) || cost < distances[move])
                    {
                        prev[move] = current;
                        distances[move] = cost;
                    }
                }
            }

            List<Element> min_path = null;
            int min_cost = int.MaxValue;
            foreach (var elem in tiles[tiles.Count - 1])
            {
                if (!prev.ContainsKey(elem))
                    continue;
                List<Element> path = new();
                HashSet<Element> visited = new() { elem };
                Element current = elem;
                int cost = 0;
                while (prev.ContainsKey(current))
                {
                    cost += current.Distance(prev[current]);
                    if (currentClay.Contains(prev[current].ToVector()))
                    {
                        cost += EXISTING_CLAY_COST;
                    }
                    if (visited.Contains(prev[current]))
                    {
                        cost += PREV_SAME_TILE_COST;
                    }
                    path.Add(current);
                    visited.Add(current);
                    current = prev[current];
                }
                cost += current.Distance(start);
                if (cost < min_cost || min_path == null)
                {
                    min_path = path;
                    min_path.Reverse();
                    min_cost = cost;
                }
            }
            return min_path;
        }
    }
}