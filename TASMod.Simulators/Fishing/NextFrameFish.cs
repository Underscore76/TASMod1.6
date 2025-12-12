using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.GameData;
using StardewValley.GameData.Locations;
using StardewValley.Internal;
using StardewValley.Tools;
using TASMod.Console;
using TASMod.Extensions;
using TASMod.Helpers;
using TASMod.System;
using xTile.Dimensions;

namespace TASMod.Simulators.Fishing
{
    public class FishHit
    {
        public string FishType;
    }

    public class NextFrameFish
    {
        public static int CurrentFrame = -1;
        public static List<FishHit> Hits = new List<FishHit> { new(), new(), new(), new() };
        public static FishHit Current()
        {
            return Estimate(ActiveInstance.InstanceIndex);
        }
        public static FishHit Estimate(int index)
        {
            if (TASDateTime.CurrentFrame == (uint)Controller.State.Count)
            {
                if (CurrentFrame != Controller.State.Count)
                {
                    CurrentFrame = Controller.State.Count;
                    for (int i = 0; i < 4; i++)
                    {
                        try
                        {
                            Hits[i] = EstimateIndex(i);
                        }
                        catch (Exception e)
                        {
                            Hits[i] = new FishHit();
                            Controller.Console.Error($"Error estimating fish for player {i} on frame {Controller.State.Count}: {e}");
                        }
                    }
                }
                return Hits[index];
            }
            return new();
        }

        public static FishHit EstimateIndex(int index)
        {
            Farmer farmer = InstanceCurrentPlayer.Get(index).Player;
            Random random = InstanceData.Get(index).random.Copy();
            GameLocation location = InstanceCurrentLocation.Get(index).Location;
            if (farmer.CurrentTool is FishingRod rod && rod.isNibbling)
            {
                string fishType = FishingRod_DoFunction(location, random, farmer, rod);
                return new FishHit { FishType = fishType };
            }
            return new FishHit();
        }
        public static int MaxAttempts = 10000;
        public static int CalcOffset(int index, string target)
        {
            Farmer farmer = InstanceCurrentPlayer.Get(index).Player;
            Random curRandom = InstanceData.Get(index).random.Copy();
            GameLocation location = InstanceCurrentLocation.Get(index).Location;
            int counter = 0;
            while (counter++ < MaxAttempts)
            {
                Random random = curRandom.Copy();
                string fishType = FishingRod_DoFunction(location, random, farmer, (FishingRod)farmer.CurrentTool);
                if (fishType == target)
                {
                    return curRandom.get_Index() - InstanceData.Get(index).random.get_Index();
                }
                curRandom.Next(); // increment
            }
            return -1;
        }

        public static void Tool_DoFunction(Random random)
        {
            random.Next(-32768, 32768);
        }
        public static string FishingRod_DoFunction(GameLocation location, Random random, Farmer player, FishingRod rod)
        {
            Tool_DoFunction(random);
            // Controller.Console.Warn($"PRE: {random.get_Index()})");
            string t = Location_GetFish(location, random, player, rod);
            // Controller.Console.Warn($"POST: caught {t} ({r.get_Index()} -> {random.get_Index()})");
            return t;
        }

        public static string Location_GetFish(GameLocation location, Random random, Farmer player, FishingRod rod)
        {
            return GetFishFromLocationData(location, random, player, rod);
        }
        public static string GetFishFromLocationData(GameLocation location, Random random, Farmer player, FishingRod rod)
        {
            Vector2 bobberTile = calculateBobberTile(rod.bobber.Value);
            bool isTutorialCatch = player.fishCaught.Length == 0;
            int waterDepth = rod.clearWaterDistance;
            if (location.fishSplashPoint.Value != Point.Zero)
            {
                Microsoft.Xna.Framework.Rectangle fishSplashRect2 = new Microsoft.Xna.Framework.Rectangle(location.fishSplashPoint.X * 64, location.fishSplashPoint.Y * 64, 64, 64);
                Microsoft.Xna.Framework.Rectangle bobberRect = new Microsoft.Xna.Framework.Rectangle((int)rod.bobber.X - 80, (int)rod.bobber.Y - 80, 64, 64);
                if (fishSplashRect2.Intersects(bobberRect))
                {
                    waterDepth += 1;
                    if (Controller.DebugMode)
                    {
                        Controller.Console.Warn($"increased water depth to {waterDepth} due to fish splash point");
                    }
                }
            }
            bool isInherited = false;
            LocationData locationData = location.GetData();
            Dictionary<string, string> allFishData = DataLoader.Fish(Game1.content);
            Season season = Game1.GetSeasonForLocation(location);
            if (location == null || !location.TryGetFishAreaForTile(bobberTile, out var fishAreaId, out var _))
            {
                fishAreaId = null;
            }

            bool usingMagicBait = false;
            bool hasCuriosityLure = false;
            string baitTargetFish = null;
#pragma warning disable CS0219 // Variable is assigned but its value is never used
            bool usingGoodBait = false;
#pragma warning restore CS0219 // Variable is assigned but its value is never used
            if (rod.isFishing)
            {
                usingMagicBait = rod.HasMagicBait();
                hasCuriosityLure = rod.HasCuriosityLure();
                StardewValley.Object bait = rod.GetBait();
                if (bait != null)
                {
                    if (bait.QualifiedItemId == "(O)SpecificBait" && bait.preservedParentSheetIndex.Value != null)
                    {
                        baitTargetFish = "(O)" + bait.preservedParentSheetIndex.Value;
                    }
                    if (bait.QualifiedItemId != "(O)685")
                    {
                        usingGoodBait = true;
                    }
                }
            }
            Point playerTile = player.TilePoint;
            ItemQueryContext itemQueryContext = new ItemQueryContext(location, null, random, "location '" + location.Name + "' > fish data");
            IEnumerable<SpawnFishData> possibleFish = Game1.locationData["Default"].Fish;
            if (locationData != null && locationData.Fish?.Count > 0)
            {
                possibleFish = possibleFish.Concat(locationData.Fish);
            }
            // possibleFish = possibleFish.OrderBy(p => (p.Precedence, random.Next())).Select(p => p);
            possibleFish = possibleFish.OrderBy((SpawnFishData p) => p.Precedence).ThenBy((SpawnFishData p) => random.Next());
            // Controller.Console.Warn($"sorted {possibleFish.Count()} fish ({random.get_Index()})");
            HashSet<string> ignoreQueryKeys = (usingMagicBait ? GameStateQuery.MagicBaitIgnoreQueryKeys : null);
            for (int i = 0; i < 2; i++)
            {
                foreach (SpawnFishData spawn in possibleFish)
                {
                    // Controller.Console.Warn($"testing {spawn.ItemId} fish ({random.get_Index()})");
                    if ((isInherited && !spawn.CanBeInherited) || (spawn.FishAreaId != null && fishAreaId != spawn.FishAreaId) || (spawn.Season.HasValue && !usingMagicBait && spawn.Season != season))
                    {
                        continue;
                    }

                    Microsoft.Xna.Framework.Rectangle? playerPosition = spawn.PlayerPosition;
                    if (playerPosition.HasValue && !playerPosition.GetValueOrDefault().Contains(playerTile.X, playerTile.Y))
                    {
                        continue;
                    }

                    playerPosition = spawn.BobberPosition;
                    if ((playerPosition.HasValue && !playerPosition.GetValueOrDefault().Contains((int)bobberTile.X, (int)bobberTile.Y)) || player.FishingLevel < spawn.MinFishingLevel || waterDepth < spawn.MinDistanceFromShore || (spawn.MaxDistanceFromShore > -1 && waterDepth > spawn.MaxDistanceFromShore) || (spawn.RequireMagicBait && !usingMagicBait))
                    {
                        continue;
                    }

                    float chance = spawn.GetChance(hasCuriosityLure, player.DailyLuck, player.LuckLevel, (float value, IList<QuantityModifier> modifiers, QuantityModifier.QuantityModifierMode mode) => Utility.ApplyQuantityModifiers(value, modifiers, mode, location, random: random), spawn.ItemId == baitTargetFish);
                    if (spawn.UseFishCaughtSeededRandom)
                    {
                        if (!Utility.CreateRandom(Game1.uniqueIDForThisGame, player.stats.Get("PreciseFishCaught") * 859).NextBool(chance))
                        {
                            continue;
                        }
                    }
                    else if (!random.NextBool(chance))
                    {
                        continue;
                    }
                    if (spawn.Condition != null && !GameStateQuery.CheckConditions(spawn.Condition, location, random: random, ignoreQueryKeys: ignoreQueryKeys))
                    {
                        continue;
                    }

                    // This item was selected
                    // Controller.Console.Warn($"PRE:TryResolve {spawn.ItemId} fish ({random.get_Index()})");
                    random.NextBool(); // object..ctor flipped
                    // Controller.Console.Warn($"POST:TryResolve {spawn.ItemId} fish ({random.get_Index()})");
                    if (CheckGenericFishRequirements(random, spawn.ItemId, allFishData, location, player, spawn, waterDepth, usingMagicBait, hasCuriosityLure, spawn.ItemId == baitTargetFish, isTutorialCatch))
                    {
                        // Fish was caught
                        return spawn.ItemId;
                    }
                }
            }
            return "(O)145";
        }

        public static bool CheckGenericFishRequirements(Random random, string itemId, Dictionary<string, string> allFishData, GameLocation location, Farmer player, SpawnFishData spawn, int waterDepth, bool usingMagicBait, bool hasCuriosityLure, bool usingTargetBait, bool isTutorialCatch)
        {
            if (Controller.DebugMode)
            {
                Controller.Console.Warn($"pre initial check {itemId} {itemId.StartsWith("(O)")}");
            }
            if (!itemId.StartsWith("(O)") || !allFishData.TryGetValue(itemId.Substring(3), out var rawSpecificFishData))
            {
                return !isTutorialCatch;
            }
            if (Controller.DebugMode)
            {
                Controller.Console.Warn($"data{rawSpecificFishData}");
            }
            // Controller.Console.Warn($"post initial check {itemId} ({random.get_Index()})");
            string[] specificFishData = rawSpecificFishData.Split('/');
            if (ArgUtility.Get(specificFishData, 1) == "trap")
            {
                return !isTutorialCatch;
            }
            bool isTrainingRod = player?.CurrentTool?.QualifiedItemId == "(T)TrainingRod";
            if (isTrainingRod)
            {
                bool? canUseTrainingRod = spawn.CanUseTrainingRod;
                if (canUseTrainingRod.HasValue)
                {
                    if (!canUseTrainingRod.GetValueOrDefault())
                    {
                        return false;
                    }
                }
                else
                {
                    if (!ArgUtility.TryGetInt(specificFishData, 1, out var difficulty, out var error7, "int difficulty"))
                    {
                        return false;
                    }
                    if (difficulty >= 50)
                    {
                        return false;
                    }
                }
            }
            if (isTutorialCatch)
            {
                if (!ArgUtility.TryGetOptionalBool(specificFishData, 13, out var isTutorialFish, out var error6, defaultValue: false, "bool isTutorialFish"))
                {
                    return false;
                }
                if (!isTutorialFish)
                {
                    return false;
                }
            }
            if (!spawn.IgnoreFishDataRequirements)
            {
                if (!usingMagicBait)
                {
                    if (!ArgUtility.TryGet(specificFishData, 5, out var rawTimeSpans, out var error5, allowBlank: true, "string rawTimeSpans"))
                    {
                        return false;
                    }
                    string[] timeSpans = ArgUtility.SplitBySpace(rawTimeSpans);
                    bool found = false;
                    for (int i = 0; i < timeSpans.Length; i += 2)
                    {
                        if (!ArgUtility.TryGetInt(timeSpans, i, out var startTime, out error5, "int startTime") || !ArgUtility.TryGetInt(timeSpans, i + 1, out var endTime, out error5, "int endTime"))
                        {
                            return false;
                        }
                        // Controller.Console.Warn($"checking {startTime} <= {Game1.timeOfDay} < {endTime}");
                        if (Game1.timeOfDay >= startTime && Game1.timeOfDay < endTime)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        return false;
                    }
                }
                if (!usingMagicBait)
                {
                    if (!ArgUtility.TryGet(specificFishData, 7, out var weather, out var error4, allowBlank: true, "string weather"))
                    {
                        return false;
                    }
                    if (!(weather == "rainy"))
                    {
                        if (weather == "sunny" && location.IsRainingHere())
                        {
                            return false;
                        }
                    }
                    else if (!location.IsRainingHere())
                    {
                        return false;
                    }
                }
                if (!ArgUtility.TryGetInt(specificFishData, 12, out var minFishingLevel, out var error3, "int minFishingLevel"))
                {
                    return false;
                }
                if (player.FishingLevel < minFishingLevel)
                {
                    return false;
                }
                if (!ArgUtility.TryGetInt(specificFishData, 9, out var maxDepth, out var error2, "int maxDepth") || !ArgUtility.TryGetFloat(specificFishData, 10, out var chance, out error2, "float chance") || !ArgUtility.TryGetFloat(specificFishData, 11, out var depthMultiplier, out error2, "float depthMultiplier"))
                {
                    return false;
                }
                float dropOffAmount = depthMultiplier * chance;
                chance -= (float)Math.Max(0, maxDepth - waterDepth) * dropOffAmount;
                chance += (float)player.FishingLevel / 50f;
                if (isTrainingRod)
                {
                    chance *= 1.1f;
                }
                chance = Math.Min(chance, 0.9f);
                if ((double)chance < 0.25 && hasCuriosityLure)
                {
                    if (spawn.CuriosityLureBuff > -1f)
                    {
                        chance += spawn.CuriosityLureBuff;
                    }
                    else
                    {
                        float max = 0.25f;
                        float min = 0.08f;
                        chance = (max - min) / max * chance + (max - min) / 2f;
                    }
                }
                if (usingTargetBait)
                {
                    chance *= 1.66f;
                }
                if (spawn.ApplyDailyLuck)
                {
                    chance += (float)player.DailyLuck;
                }
                List<QuantityModifier> chanceModifiers = spawn.ChanceModifiers;
                if (chanceModifiers != null && chanceModifiers.Count > 0)
                {
                    chance = Utility.ApplyQuantityModifiers(chance, spawn.ChanceModifiers, spawn.ChanceModifierMode, location);
                }
                if (Controller.DebugMode)
                {
                    Controller.Console.Warn($"final chance: {chance}");
                }
                if (!random.NextBool(chance))
                {
                    return false;
                }
            }
            return true;
        }

        private static Vector2 calculateBobberTile(Vector2 bobber)
        {
            return new Vector2(bobber.X / 64f, bobber.Y / 64f);
        }
    }
}