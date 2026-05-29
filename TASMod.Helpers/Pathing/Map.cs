using System.Collections.Generic;
using System;
using Microsoft.Xna.Framework;
using StardewValley;
using xTile.ObjectModel;

namespace TASMod.Helpers.Pathing
{
    public class Map
    {
        public int GridSize;
        public Dictionary<Edge, List<Region>> EdgeList = new();
        public Dictionary<Region, List<Edge>> RegionList = new();
        public IEnumerable<Edge> Edges => EdgeList.Keys;
        public IEnumerable<Region> Regions => RegionList.Keys;

        public Map(int gridSize = 7)
        {
            GridSize = gridSize;
        }

        public void Clear()
        {
            EdgeList.Clear();
            RegionList.Clear();
        }
        public bool IsWalkable(GameLocation loc, int x, int y)
        {
            Vector2 vec = new(x, y);
            if (!loc.isTileOnMap(vec))
                return false;
            Rectangle tileRect = new Rectangle(x * Game1.tileSize, y * Game1.tileSize, Game1.tileSize, Game1.tileSize);
            foreach (var current in loc.largeTerrainFeatures)
            {
                if (current.getBoundingBox().Intersects(tileRect))
                    return false;
            }
            foreach (var current in loc.resourceClumps)
            {
                if (current.getBoundingBox().Intersects(tileRect))
                    return false;
            }

            foreach (var current in loc.buildings)
            {
                if (current.GetBoundingBox().Intersects(tileRect))
                    return false;
            }
            if (loc.overlayObjects.ContainsKey(vec))
            {
                StardewValley.Object obj = loc.overlayObjects[vec];
                return obj.isPassable();
            }
            if (loc.Objects.ContainsKey(vec))
            {
                StardewValley.Object obj = loc.Objects[vec];
                return obj.isPassable();
            }
            if (loc.GetFurnitureAt(vec) != null)
                return false;
            if (loc.isTilePassable(vec))
                return true;
            // bridges
            if (loc.doesTileHaveProperty(x, y, "Passable", "Buildings") != null)
            {
                var viewport = InstanceViewport.Get(0);
                var backTile = loc
                    .map.GetLayer("Back")
                    .PickTile(
                        new xTile.Dimensions.Location(tileRect.X, tileRect.Y),
                        viewport.Viewport.Size
                    );
                if (
                    backTile == null
                    || !backTile.TileIndexProperties.TryGetValue(
                        "Passable",
                        out PropertyValue value
                    )
                    || value != "F"
                )
                    return true;
            }
            return false;
        }

        public void Build(GameLocation loc, bool collapse = false)
        {
            Clear();
            var regions = BuildRegions(loc);
            foreach (var region in regions)
            {
                var edges = region.GetEdges();
                foreach (var edge in edges)
                {
                    AddEdgeRegion(edge, region);
                }
            }
            if (collapse)
            {
                CollapseSelf();
            }
        }
        public List<Region> BuildRegions(GameLocation loc)
        {
            List<Region> regions = new();
            int MapWidth = loc.map.Layers[0].LayerWidth;
            int MapHeight = loc.map.Layers[0].LayerHeight;
            for (int minX = 0; minX < MapWidth; minX += GridSize)
            {
                int x = minX;
                int maxX = Math.Min(MapWidth, x + GridSize);
                for (int minY = 0; minY < MapHeight; minY += GridSize)
                {
                    int y = minY;
                    int maxY = Math.Min(MapHeight, y + GridSize);
                    // floodfill from (x,y) to (maxX, maxY) to get connected components
                    HashSet<Vector2> visited = new();
                    while (x < maxX && y < maxY)
                    {
                        Vector2 start = new(x, y);
                        Region r = new() { tiles = new(), Id = x * 1024 + y, Walkable = IsWalkable(loc, x, y) };
                        Queue<Vector2> q = new();
                        q.Enqueue(start);
                        while (q.Count > 0)
                        {
                            Vector2 curr = q.Dequeue();
                            if (curr.X < minX || curr.X >= maxX || curr.Y < minY || curr.Y >= maxY)
                                continue;
                            if (visited.Contains(curr))
                                continue;
                            if (IsWalkable(loc, (int)curr.X, (int)curr.Y) != r.Walkable)
                                continue;
                            if (r.ContainsTile(curr))
                                continue;
                            r.AddTile(curr);
                            visited.Add(curr);
                            q.Enqueue(new Vector2(curr.X + 1, curr.Y));
                            q.Enqueue(new Vector2(curr.X - 1, curr.Y));
                            q.Enqueue(new Vector2(curr.X, curr.Y + 1));
                            q.Enqueue(new Vector2(curr.X, curr.Y - 1));
                        }
                        if (r.Size > 0)
                        {
                            regions.Add(r);
                        }
                        if (x + 1 < maxX)
                        {
                            x++;
                        }
                        else
                        {
                            x = minX;
                            y++;
                        }
                    }
                }
            }
            return regions;
        }


        public void AddEdgeRegion(Edge e, Region r)
        {
            if (!EdgeList.ContainsKey(e))
            {
                EdgeList.Add(e, new());
            }
            if (!RegionList.ContainsKey(r))
            {
                RegionList.Add(r, new());
            }
            for (int i = 0; i < EdgeList[e].Count; i++)
            {
                if (EdgeList[e][i].Id == r.Id)
                    return;
            }
            EdgeList[e].Add(r);
            RegionList[r].Add(e);
        }

        public List<Region> ConnectedComponents()
        {
            List<Region> regions = new();
            HashSet<Region> visited = new();
            foreach (var pair in RegionList)
            {
                if (visited.Contains(pair.Key))
                    continue;
                Region r = new() { tiles = new(), Id = pair.Key.Id, Walkable = pair.Key.Walkable };
                Queue<Region> q = new();
                q.Enqueue(pair.Key);
                while (q.Count > 0)
                {
                    Region curr = q.Dequeue();
                    if (visited.Contains(curr))
                        continue;
                    visited.Add(curr);
                    r.UnionWith(curr);
                    foreach (var edge in RegionList[curr])
                    {
                        foreach (var neighbor in EdgeList[edge])
                        {
                            if (!visited.Contains(neighbor) && neighbor.Walkable == r.Walkable)
                            {
                                q.Enqueue(neighbor);
                            }
                        }
                        // need to also check for edge overlaps that aren't identical edges
                        foreach (var otherEdge in EdgeList.Keys)
                        {
                            if (!edge.Overlaps(otherEdge))
                                continue;
                            foreach (var neighbor in EdgeList[otherEdge])
                            {
                                if (!visited.Contains(neighbor) && neighbor.Walkable == r.Walkable)
                                {
                                    q.Enqueue(neighbor);
                                }
                            }
                        }

                    }
                }
                regions.Add(r);
            }

            return regions;
        }

        private void CollapseSelf()
        {
            var regions = ConnectedComponents();
            Clear();
            foreach (var region in regions)
            {
                var edges = region.GetEdges();
                foreach (var edge in edges)
                {
                    AddEdgeRegion(edge, region);
                }
            }
        }

        public Map Collapse()
        {
            Map collapsed = new(this.GridSize);
            var regions = ConnectedComponents();
            foreach (var region in regions)
            {
                var edges = region.GetEdges();
                foreach (var edge in edges)
                {
                    collapsed.AddEdgeRegion(edge, region);
                }
            }
            return collapsed;
        }

        public bool PathExists(Vector2 start, Vector2 end)
        {
            Map reduced = Collapse();
            foreach (var r in reduced.Regions)
            {
                if (r.ContainsTile(start) && r.ContainsTile(end))
                {
                    return true;
                }
            }
            return false;
        }

        public bool AreNeighbors(Region r1, Region r2)
        {
            if (r1.Id == r2.Id)
                return true;
            foreach (var edge in RegionList[r1])
            {
                foreach (var otherEdge in EdgeList.Keys)
                {
                    if (!edge.Overlaps(otherEdge))
                        continue;
                    foreach (var neighbor in EdgeList[otherEdge])
                    {
                        if (neighbor.Id == r2.Id)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public Region GetRegion(Vector2 tile)
        {
            foreach (var region in Regions)
            {
                if (region.ContainsTile(tile))
                {
                    return region;
                }
            }
            return null;
        }

        public List<Region> GetNeighborRegions(Region r)
        {
            HashSet<int> neighborIds = new();
            List<Region> neighbors = new();
            foreach (var edge in RegionList[r])
            {
                foreach (var otherEdge in EdgeList.Keys)
                {
                    if (!edge.Overlaps(otherEdge))
                        continue;
                    foreach (var neighbor in EdgeList[otherEdge])
                    {
                        if (neighbor.Id != r.Id && neighbor.Walkable == r.Walkable && !neighborIds.Contains(neighbor.Id))
                        {
                            neighbors.Add(neighbor);
                            neighborIds.Add(neighbor.Id);
                        }
                    }
                }
            }
            // how would I capture diagonal where 4 regions meet at a point?
            return neighbors;
        }

        public List<Region> GetAnyNeighbors(Region r)
        {
            HashSet<int> neighborIds = new();
            List<Region> neighbors = new();
            foreach (var edge in RegionList[r])
            {
                foreach (var otherEdge in EdgeList.Keys)
                {
                    if (!edge.Overlaps(otherEdge))
                        continue;
                    foreach (var neighbor in EdgeList[otherEdge])
                    {
                        if (neighbor.Id != r.Id && !neighborIds.Contains(neighbor.Id))
                        {
                            neighbors.Add(neighbor);
                            neighborIds.Add(neighbor.Id);
                        }
                    }
                }
            }
            return neighbors;
        }

        public double GetRegionDistance(Region r1, Region r2)
        {
            var x = Math.Abs(r1.Center.X - r2.Center.X);
            var y = Math.Abs(r1.Center.Y - r2.Center.Y);
            return x * x + y * y;
        }

        public double GetRegionHeuristic(Region r1, Region r2)
        {
            var x = Math.Abs(r1.Center.X - r2.Center.X);
            var y = Math.Abs(r1.Center.Y - r2.Center.Y);
            return x * x + y * y;
        }

        public List<Region> GetRegionPath(Vector2 start, Vector2 end)
        {
            // A* on the reduced graph
            Region startRegion = GetRegion(start);
            Region endRegion = GetRegion(end);
            if (startRegion == null || endRegion == null)
                return null;
            if (startRegion.Id == endRegion.Id)
                return new List<Region>() { startRegion, endRegion };

            AStar<Region> astar = new(
                (index, r) => GetNeighborRegions(r),
                (index, r1, r2) => GetRegionDistance(r1, r2),
                (index, r1, r2) => GetRegionHeuristic(r1, r2)
            );
            var path = astar.Search(0, startRegion, endRegion, out double cost);
            if (path == null) return null;
            return path;
        }
        public List<Region> ExpandRegions(List<Region> path)
        {
            HashSet<int> regionIds = new();
            List<Region> expanded = new();
            foreach (var region in path)
            {
                if (!regionIds.Contains(region.Id))
                {
                    expanded.Add(region);
                    regionIds.Add(region.Id);
                }
                foreach (var neighbor in GetNeighborRegions(region))
                {
                    if (!regionIds.Contains(neighbor.Id))
                    {
                        expanded.Add(neighbor);
                        regionIds.Add(neighbor.Id);
                    }
                }
            }
            return expanded;
        }

        public List<Vector2> GetTileNeighbors(GameLocation loc, Vector2 tile)
        {
            List<Vector2> neighbors = new();
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0)
                        continue;
                    Vector2 neighbor = tile + new Vector2(i, j);
                    if (!IsWalkable(loc, (int)neighbor.X, (int)neighbor.Y))
                        continue;
                    if (i != 0 && j != 0)
                    {
                        if (!IsWalkable(loc, (int)neighbor.X, (int)tile.Y) || !IsWalkable(loc, (int)tile.X, (int)neighbor.Y))
                            continue;
                    }
                    foreach (var region in Regions)
                    {
                        if (region.ContainsTile(neighbor))
                        {
                            neighbors.Add(neighbor);
                            break;
                        }
                    }
                }
            }
            return neighbors;
        }
        public double GetTileDistance(GameLocation loc, Vector2 t1, Vector2 t2)
        {
            if (t1.X == t2.X || t1.Y == t2.Y)
            {
                return 1;
            }
            else
            {
                return 1.4142135624;
            }
        }
        public double GetTileHeuristic(Vector2 t1, Vector2 t2)
        {
            int tilesDiagonal, tilesCardinal;
            if (t1.X == t2.X || t1.Y == t2.Y)
            {
                tilesDiagonal = 0;
                tilesCardinal = (int)(Math.Abs(t1.X - t2.X) + Math.Abs(t1.Y - t2.Y));
            }
            else
            {
                tilesDiagonal = (int)Math.Min(Math.Abs(t1.X - t2.X), Math.Abs(t1.Y - t2.Y));
                tilesCardinal = (int)(Math.Abs(t1.X - t2.X) + Math.Abs(t1.Y - t2.Y) - 2 * tilesDiagonal);
            }
            return tilesDiagonal * 1.4142135624 + tilesCardinal;
        }
        public List<Vector2> GetTilePath(Vector2 start, Vector2 end, GameLocation loc = null)
        {
            if (loc == null)
            {
                loc = InstanceCurrentLocation.Get(0).Location;
            }
            var path = GetRegionPath(start, end);
            if (path == null) return new();
            // pad out a region boundary to help better capture diag paths?
            // path = ExpandRegions(path);
            Map pathMap = new();
            foreach (var region in path)
            {
                var edges = region.GetEdges();
                foreach (var edge in edges)
                {
                    pathMap.AddEdgeRegion(edge, region);
                }
            }
            AStar<Vector2> astarTiles = new(
                (index, tile) => pathMap.GetTileNeighbors(loc, tile),
                (index, t1, t2) => GetTileDistance(loc, t1, t2),
                (index, t1, t2) => GetTileHeuristic(t1, t2)
            );
            var tilePath = astarTiles.Search(0, start, end, out double tileCost);
            return tilePath;
        }

        public Vector2 FloodFillNearest(Vector2 start, List<Vector2> targets)
        {
            HashSet<Vector2> targetSet = new(targets);
            Queue<Vector2> queue = new();
            HashSet<Vector2> visited = new();
            queue.Enqueue(start);
            var loc = InstanceCurrentLocation.Get(0).Location;
            while (queue.Count > 0)
            {
                Vector2 current = queue.Dequeue();
                if (targetSet.Contains(current))
                    return current;
                if (visited.Contains(current))
                    continue;
                visited.Add(current);
                if (!IsWalkable(loc, (int)current.X, (int)current.Y))
                    continue;
                foreach (var neighbor in Utility.getAdjacentTileLocations(current))
                {
                    if (!visited.Contains(neighbor))
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }
            return new Vector2(-1, -1);
        }
    }
}