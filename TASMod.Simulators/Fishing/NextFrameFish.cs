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
using xTile.Dimensions;

namespace TASMod.Simulators.Fishing
{
    public class NextFrameFish
    {
        Random random;
        public Vector2 bobber = new Vector2(0, 0);
        public int clearWaterDistance = 0;
        public float fishingNibbleAccumulator = 0;
        public float timeUntilFishingNibbleDone = 0;
        public bool isNibbling;
        public NextFrameFish()
        {
            random = Game1.random.Copy();
            if (Game1.player.CurrentTool is FishingRod rod)
            {
                isNibbling = rod.isNibbling;
                if (isNibbling)
                {
                    clearWaterDistance = rod.clearWaterDistance;
                    bobber = rod.bobber.Value;
                    fishingNibbleAccumulator = rod.fishingNibbleAccumulator;
                    timeUntilFishingNibbleDone = rod.timeUntilFishingNibbleDone;
                }
            }
        }

        public void Tool_DoFunction()
        {
            random.Next(-32768, 32768);
        }
        public string FishingRod_DoFunction()
        {
            var r = random.Copy();
            Vector2 bobberTile = calculateBobberTile();
            int tileX = (int)bobberTile.X;
            int tileY = (int)bobberTile.Y;
            Tool_DoFunction();
            // Controller.Console.Warn($"PRE: {random.get_Index()})");
            string t = Location_GetFish(bobberTile);
            // Controller.Console.Warn($"POST: caught {t} ({r.get_Index()} -> {random.get_Index()})");
            random = r;
            return t;
        }

        public string Location_GetFish(Vector2 bobberTile)
        {
            return GetFishFromLocationData(bobberTile);
        }
        public string GetFishFromLocationData(Vector2 bobberTile)
        {
            GameLocation location = Game1.currentLocation;
            Farmer player = Game1.player;
            bool isTutorialCatch = player.fishCaught.Length == 0;
            int waterDepth = clearWaterDistance;
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
            bool usingGoodBait = false;
            if (player?.CurrentTool is FishingRod rod && rod.isFishing)
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
            possibleFish = from p in possibleFish
                           orderby (p.Precedence, random.Next())
                           select p;
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
                    if (spawn.Condition != null && !GameStateQuery.CheckConditions(spawn.Condition, location, null, null, null, null, ignoreQueryKeys))
                    {
                        continue;
                    }

                    // This item was selected
                    // Controller.Console.Warn($"PRE:TryResolve {spawn.ItemId} fish ({random.get_Index()})");
                    random.NextBool(); // object..ctor flipped
                    // Controller.Console.Warn($"POST:TryResolve {spawn.ItemId} fish ({random.get_Index()})");
                    if (CheckGenericFishRequirements(spawn.ItemId, allFishData, location, player, spawn, waterDepth, usingMagicBait, hasCuriosityLure, spawn.ItemId == baitTargetFish, isTutorialCatch))
                    {
                        // Fish was caught
                        return spawn.ItemId;
                    }
                }
            }
            return "(O)145";
        }

        public bool CheckGenericFishRequirements(string itemId, Dictionary<string, string> allFishData, GameLocation location, Farmer player, SpawnFishData spawn, int waterDepth, bool usingMagicBait, bool hasCuriosityLure, bool usingTargetBait, bool isTutorialCatch)
        {
            // Controller.Console.Warn($"pre initial check {itemId} {itemId.StartsWith("(O)")}");
            if (!itemId.StartsWith("(O)") || !allFishData.TryGetValue(itemId.Substring(3), out var rawSpecificFishData))
            {
                return !isTutorialCatch;
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
                if (!random.NextBool(chance))
                {
                    return false;
                }
            }
            return true;
        }

        private Vector2 calculateBobberTile()
        {
            return new Vector2(bobber.X / 64f, bobber.Y / 64f);
        }
    }
}