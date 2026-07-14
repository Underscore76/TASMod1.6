using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Locations;
using StardewValley.Tools;
using TASMod.Extensions;
using TASMod.Helpers;
using TASMod.Overlays;
using TASMod.System;
namespace TASMod.Simulators.GemNode
{
    public class GemNodeHit
    {
        public Vector2 Tile;
        public List<string> CurrentGems = new List<string>();
        public List<string> LadderGems = new List<string>();
        public override string ToString()
        {
            return $"Tile: {Tile}, CurrentGems: [{string.Join(",", CurrentGems)}], LadderGems: [{string.Join(",", LadderGems)}]";
        }
    }

    public class GemNode
    {
        public static MinesFloor minesFloor = new MinesFloor(0);
        public static int CurrentFrame = -1;
        public static List<GemNodeHit> Hits = new List<GemNodeHit> { new(), new(), new(), new() };

        public static GemNodeHit Current()
        {
            return Estimate(ActiveInstance.InstanceIndex);
        }

        public static GemNodeHit Estimate(int index)
        {
            int targetFrame = Controller.State.Count;
            if (Controller.GameMode == TASMode.Replay)
            {
                targetFrame = (int)TASDateTime.CurrentFrame;
            }
            if (TASDateTime.CurrentFrame == (uint)targetFrame)
            {
                if (CurrentFrame != targetFrame)
                {
                    CurrentFrame = targetFrame;
                    for (int i = 0; i < 4; i++)
                    {
                        try
                        {
                            Hits[i] = EstimateIndex(i);
                        }
                        catch (Exception e)
                        {
                            ModEntry.Console.Log($"GemNode Estimate Exception: {e}", StardewModdingAPI.LogLevel.Error);
                        }
                    }
                }
                return Hits[index];
            }
            return new();
        }

        public static GemNodeHit EstimateIndex(int index)
        {
            var player = InstanceCurrentPlayer.Get(index).Player;
            var location = InstanceCurrentLocation.Get(index).Location;
            var random = InstanceData.Get(index).random.Copy();
            return EstimateFarmer(random, location, player);
        }

        public static GemNodeHit EstimateNext(GameLocation location)
        {
            var player = InstanceCurrentPlayer.Get(0).Player;
            var random = InstanceData.Get(0).random.Copy();
            return EstimateFarmer(random, location, player);
        }

        public static GemNodeHit EstimateFarmer(Random random, GameLocation loc, Farmer farmer)
        {
            GemNodeHit hit = new GemNodeHit();
            if (loc == null || farmer == null || loc is not MineShaft)
                return hit;
            foreach (var obj in loc.objects.Pairs)
            {
                if (obj.Value.ItemId == "44") // Gem Node
                {
                    string gemType = GetGemType((int)obj.Key.X, (int)obj.Key.Y, farmer, loc as MineShaft, false);
                    string ladderGemType = GetGemType((int)obj.Key.X, (int)obj.Key.Y, farmer, loc as MineShaft, true);
                    hit.Tile = obj.Key;
                    hit.CurrentGems.Add(gemType);
                    hit.LadderGems.Add(ladderGemType);
                }
            }
            return hit;
        }

        public static string GetGemType(int x, int y, Farmer who, MineShaft loc, bool hasLadderSpawned)
        {
            Random r = Utility.CreateDaySaveRandom(x * 1000, y, loc.mineLevel);
            r.NextDouble();
            if (!hasLadderSpawned)
            {
                r.NextDouble();
            }
            var contents = minesFloor.BreakStone("44", x, y, who, loc, r);
            return contents.FirstOrDefault()?.Item1 ?? "";
        }
    }
}
