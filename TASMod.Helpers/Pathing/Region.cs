// taking idea for hierarchical segmented pathfinding from https://www.youtube.com/watch?v=pj3_BQbvKw4
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley;
using TASMod.Helpers;
using xTile.ObjectModel;

namespace TASMod.Helpers.Pathing
{
    public class Region
    {
        public Rectangle Bounds;
        public Vector2 Center;
        public HashSet<Vector2> tiles = new();
        public int Size => tiles.Count;
        public int Id;
        public bool Walkable;
        public override string ToString()
        {
            return $"Region {Id} Walkable: {Walkable} Tiles: {tiles.Count}";
        }

        public void AddTile(Vector2 tile)
        {

            if (Bounds == Rectangle.Empty)
            {
                Bounds = new Rectangle((int)tile.X, (int)tile.Y, 1, 1);
                Center = new Vector2(tile.X, tile.Y);
            }
            else
            {
                int minX = Math.Min(Bounds.Left, (int)tile.X);
                int maxX = Math.Max(Bounds.Right, (int)tile.X + 1);
                int minY = Math.Min(Bounds.Top, (int)tile.Y);
                int maxY = Math.Max(Bounds.Bottom, (int)tile.Y + 1);
                Bounds = new Rectangle(minX, minY, maxX - minX, maxY - minY);
                Center = new Vector2(
                    (Center.X * tiles.Count + tile.X) / (tiles.Count + 1),
                    (Center.Y * tiles.Count + tile.Y) / (tiles.Count + 1)
                );
            }
            tiles.Add(tile);
        }
        public bool ContainsTile(Vector2 tile)
        {
            if (!Bounds.Contains(tile))
                return false;
            return tiles.Contains(tile);
        }

        public void UnionWith(Region other)
        {
            foreach (var tile in other.tiles)
            {
                AddTile(tile);
            }
        }

        public static List<Edge> MergeEdges(List<Edge> edges)
        {
            var mergedEdges = new HashSet<Edge>();
            var visited = new HashSet<Edge>();
            foreach (var edge in edges)
            {
                if (visited.Contains(edge))
                    continue;
                visited.Add(edge);
                if (edge.Horizontal)
                {
                    while (true)
                    {
                        Edge left = new Edge() { X = edge.X - 1, Y = edge.Y, Length = 1, Horizontal = true };
                        if (!edges.Contains(left))
                            break;
                        visited.Add(left);
                        edge.X--;
                        edge.Length++;
                    }
                    while (true)
                    {
                        Edge right = new Edge() { X = edge.X + edge.Length, Y = edge.Y, Length = 1, Horizontal = true };
                        if (!edges.Contains(right))
                            break;
                        visited.Add(right);
                        edge.Length++;
                    }
                    mergedEdges.Add(edge);
                }
                else
                {
                    while (true)
                    {
                        Edge up = new Edge() { X = edge.X, Y = edge.Y - 1, Length = 1, Horizontal = false };
                        if (!edges.Contains(up))
                            break;
                        visited.Add(up);
                        edge.Y--;
                        edge.Length++;
                    }
                    while (true)
                    {
                        Edge down = new Edge() { X = edge.X, Y = edge.Y + edge.Length, Length = 1, Horizontal = false };
                        if (!edges.Contains(down))
                            break;
                        visited.Add(down);
                        edge.Length++;
                    }
                    mergedEdges.Add(edge);
                }
            }
            return mergedEdges.ToList();
        }

        public List<Edge> GetAllEdges()
        {
            var edges = new List<Edge>();
            foreach (var tile in tiles)
            {
                int x = (int)tile.X;
                int y = (int)tile.Y;
                // check right
                if (!tiles.Contains(new Vector2(x + 1, y)))
                {
                    edges.Add(new Edge() { X = x + 1, Y = y, Length = 1, Horizontal = false });
                }
                // check left
                if (!tiles.Contains(new Vector2(x - 1, y)))
                {
                    edges.Add(new Edge() { X = x, Y = y, Length = 1, Horizontal = false });
                }
                // check down
                if (!tiles.Contains(new Vector2(x, y + 1)))
                {
                    edges.Add(new Edge() { X = x, Y = y + 1, Length = 1, Horizontal = true });
                }
                // check up
                if (!tiles.Contains(new Vector2(x, y - 1)))
                {
                    edges.Add(new Edge() { X = x, Y = y, Length = 1, Horizontal = true });
                }
            }
            return edges;
        }

        public List<Edge> GetEdges()
        {
            var edges = GetAllEdges();
            var mergedEdges = MergeEdges(edges);
            return mergedEdges;
        }
    }

}