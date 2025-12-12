using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Media;
using Netcode;
using StardewValley;
using StardewValley.Constants;
using StardewValley.Extensions;
using StardewValley.GameData.LocationContexts;
using StardewValley.GameData.Locations;
using StardewValley.GameData.WildTrees;
using StardewValley.Internal;
using StardewValley.Locations;
using StardewValley.Monsters;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using TASMod.Extensions;
using TASMod.Helpers;
using TASMod.Networking;
using TASMod.System;
using xTile.Dimensions;
using ObjDict = System.Collections.Generic.Dictionary<Microsoft.Xna.Framework.Vector2, StardewValley.Object>;
using TFDict = System.Collections.Generic.Dictionary<Microsoft.Xna.Framework.Vector2, Netcode.NetRef<StardewValley.TerrainFeatures.TerrainFeature>>;

namespace TASMod.Simulators
{
    public static class FarmCaveSpawns
    {
        public static void Print(string message)
        {
            if (Controller.DebugMode)
            {
                Controller.Console.Warn(message);
            }
        }
        public static Dictionary<Vector2, string> GetSpawns()
        {
            // standard night info
            Random random = NightInfo.GetEndRandom();
            Print($"End of night random: {random}");
            FarmerSaving(random); // saving?
            FarmerSaving(random); // sending?
            Print($"After tool calls random: {random}");
            // quest refresh
            RefreshQuestOfTheDay((int)(Game1.stats.DaysPlayed + 1), random);
            Print($"After quest refresh random: {random}");
            // select weather and apply weather effects
            bool isRainingHere = UpdateWeather(random);
            Print($"After weather random({isRainingHere}): {random}");
            // 
            FarmUpdate(random, isRainingHere);
            Print($"After farm update random: {random}");

            return RunFarmCave(random);
        }

        public static void FarmerSaving(Random random)
        {
            for (int i = 1; i < GameRunner.instance.gameInstances.Count; i++)
            {
                // each farmhand does a random long
                Utility.RandomLong(random); // 8
                var player = InstanceCurrentPlayer.Get(i).Player;
                foreach (var item in player.Items)
                {
                    if (item is Tool)
                    {
                        random.Next();
                        random.Next();
                    }
                }
            }
        }

        public static Dictionary<Vector2, string> RunFarmCave(Random random)
        {
            Dictionary<Vector2, string> spawns = new Dictionary<Vector2, string>();
            GameLocation loc = Game1.getLocationFromName("FarmCave");
            foreach (var pair in loc.objects.Pairs)
            {
                spawns[pair.Key] = Game1.objectData[pair.Value.ParentSheetIndex.ToString()].Name;
            }
            while (random.NextDouble() < 0.66)
            {
                string fruitId = random.Next(5) switch
                {
                    0 => "296",
                    1 => "396",
                    2 => "406",
                    3 => "410",
                    _ => (random.NextDouble() < 0.1) ? "613" : random.Next(634, 639).ToString(),
                };
                Vector2 v = new Vector2(random.Next(1, loc.map.Layers[0].LayerWidth - 1), random.Next(1, loc.map.Layers[0].LayerHeight - 4));
                random.Next(); // object ctor
                if (loc.CanItemBePlacedHere(v) && !spawns.ContainsKey(v))
                {
                    spawns[v] = Game1.objectData[fruitId].Name;
                }
            }
            return spawns;
        }

        public static void GameLocation_spawnObjects(GameLocation loc, Random random, ObjDict loc_Objects, TFDict loc_terrainFeatures)
        {
            Random r = Utility.CreateRandom(Game1.stats.DaysPlayed + 1, Game1.uniqueIDForThisGame / 2);
            LocationData data = loc.GetData();
            if (data != null && loc.numberOfSpawnedObjectsOnMap < data.MaxSpawnedForageAtOnce)
            {
                Season season = loc.GetSeason();
                List<SpawnForageData> possibleForage = new List<SpawnForageData>();
                foreach (SpawnForageData spawn in GameLocation.GetData("Default").Forage.Concat(data.Forage))
                {
                    if ((spawn.Condition == null || GameStateQuery.CheckConditions(spawn.Condition, loc, null, null, null, r)) && (!spawn.Season.HasValue || spawn.Season == season))
                    {
                        possibleForage.Add(spawn);
                    }
                }
                if (possibleForage.Any())
                {
                    int numberToSpawn = r.Next(data.MinDailyForageSpawn, data.MaxDailyForageSpawn + 1);
                    numberToSpawn = Math.Min(numberToSpawn, data.MaxSpawnedForageAtOnce - loc.numberOfSpawnedObjectsOnMap);
                    ItemQueryContext itemQueryContext = new ItemQueryContext(loc, null, r, "location '" + loc.NameOrUniqueName + "' > forage");
                    for (int i = 0; i < numberToSpawn; i++)
                    {
                        for (int attempt = 0; attempt < 11; attempt++)
                        {
                            int xCoord = r.Next(loc.map.DisplayWidth / 64);
                            int yCoord = r.Next(loc.map.DisplayHeight / 64);
                            Vector2 location = new Vector2(xCoord, yCoord);
                            if (loc_Objects.ContainsKey(location) || loc_terrainFeatures.ContainsKey(location) || loc.IsNoSpawnTile(location) || loc.doesTileHaveProperty(xCoord, yCoord, "Spawnable", "Back") == null || loc.doesEitherTileOrTileIndexPropertyEqual(xCoord, yCoord, "Spawnable", "Back", "F") || !loc.CanItemBePlacedHere(location) || loc.hasTileAt(xCoord, yCoord, "AlwaysFront") || loc.hasTileAt(xCoord, yCoord, "AlwaysFront2") || loc.hasTileAt(xCoord, yCoord, "AlwaysFront3") || loc.hasTileAt(xCoord, yCoord, "Front") || loc.isBehindBush(location) || (!r.NextBool(0.1) && loc.isBehindTree(location)))
                            {
                                continue;
                            }
                            SpawnForageData forage = r.ChooseFrom(possibleForage);
                            if (!r.NextBool(forage.Chance))
                            {
                                continue;
                            }
                            loc_Objects.Add(location, DictionaryCloneHelper.CreateObjectWithRandom(random, forage.ItemId, 1));
                        }
                    }
                }
            }

            List<Vector2> positionOfArtifactSpots = new List<Vector2>();
            foreach (KeyValuePair<Vector2, StardewValley.Object> v in loc.Objects.Pairs)
            {
                if (v.Value.QualifiedItemId == "(O)590" || v.Value.QualifiedItemId == "(O)SeedSpot")
                {
                    positionOfArtifactSpots.Add(v.Key);
                }
            }
            for (int i2 = positionOfArtifactSpots.Count - 1; i2 >= 0; i2--)
            {
                if ((!(loc is IslandNorth) || !(positionOfArtifactSpots[i2].X < 26f)) && r.NextBool(0.15))
                {
                    loc_Objects.Remove(positionOfArtifactSpots[i2]);
                    positionOfArtifactSpots.RemoveAt(i2);
                }
            }
            if (positionOfArtifactSpots.Count > ((loc is not Farm) ? 1 : 0) && (loc.GetSeason() != Season.Winter || positionOfArtifactSpots.Count > 4))
            {
                return;
            }
            double chanceForNewArtifactAttempt = 1.0;
            while (r.NextDouble() < chanceForNewArtifactAttempt)
            {
                int xCoord2 = r.Next(loc.map.DisplayWidth / 64);
                int yCoord2 = r.Next(loc.map.DisplayHeight / 64);
                Vector2 location2 = new Vector2(xCoord2, yCoord2);
                if (loc.CanItemBePlacedHere(location2) && !loc.IsTileOccupiedBy(location2) && !loc_Objects.ContainsKey(location2) && !loc_terrainFeatures.ContainsKey(location2) && !loc.hasTileAt(xCoord2, yCoord2, "AlwaysFront") && !loc.hasTileAt(xCoord2, yCoord2, "Front") && !loc.isBehindBush(location2) && (loc.doesTileHaveProperty(xCoord2, yCoord2, "Diggable", "Back") != null || (loc.GetSeason() == Season.Winter && loc.doesTileHaveProperty(xCoord2, yCoord2, "Type", "Back") != null && loc.doesTileHaveProperty(xCoord2, yCoord2, "Type", "Back").Equals("Grass"))))
                {
                    if (loc.Name.Equals("Forest") && xCoord2 >= 93 && yCoord2 <= 22)
                    {
                        continue;
                    }
                    r.NextBool(0.166);
                    Print($"Placing artifact spot at {location2.X},{location2.Y}");
                    loc_Objects.Add(location2, DictionaryCloneHelper.CreateObjectWithRandom(random, "590", 1));
                }
                chanceForNewArtifactAttempt *= 0.75;
                if (loc.GetSeason() == Season.Winter)
                {
                    chanceForNewArtifactAttempt += 0.10000000149011612;
                }
            }
        }

        public static Dictionary<Vector2, int> GameLocation_DayUpdate(GameLocation loc, Random random, ObjDict loc_Objects, TFDict loc_terrainFeatures)
        {
            Dictionary<Vector2, int> grassGrowth = new Dictionary<Vector2, int>();
            int counter = 0;
            KeyValuePair<Vector2, Netcode.NetRef<TerrainFeature>>[] pairs = loc_terrainFeatures.ToArray();
            for (int i = 0; i < pairs.Length; i++)
            {
                KeyValuePair<Vector2, Netcode.NetRef<TerrainFeature>> pair2 = pairs[i];
                if (!loc.isTileOnMap(pair2.Key))
                {
                    loc_terrainFeatures.Remove(pair2.Key);
                }
                else
                {
                    switch (pair2.Value.Value)
                    {
                        case Tree tree:
                            Print($"{counter++}:{random.get_Index()}:Tree:{pair2.Key.X},{pair2.Key.Y},{tree.growthStage.Value}");
                            WildTreeData data = tree.GetData();
                            int growthStage = tree.growthStage.Value;
                            if (tree.GetMaxSizeHere() > growthStage)
                            {
                                float chance = data?.GrowthChance ?? 0.2f;
                                float fertilizedGrowthChance = data?.FertilizedGrowthChance ?? 1f;
                                if (random.NextBool(chance) || (tree.fertilized.Value && random.NextBool(fertilizedGrowthChance)))
                                {// grow up
                                    growthStage++;
                                }
                            }
                            if (growthStage >= 5 && !tree.stump.Value && loc is Farm && random.NextBool(data?.SeedSpreadChance ?? 0.15f))
                            {
                                int xCoord = random.Next(-3, 4) + (int)tree.Tile.X;
                                int yCoord = random.Next(-3, 4) + (int)tree.Tile.Y;
                                Vector2 location2 = new Vector2(xCoord, yCoord);
                                if (!loc.IsNoSpawnTile(location2, "Tree") && loc.isTileLocationOpen(new Location(xCoord, yCoord)) && !loc.IsTileOccupiedBy(location2) && !loc.isWaterTile(xCoord, yCoord) && loc.isTileOnMap(location2) && !loc_terrainFeatures.ContainsKey(location2) && !loc_Objects.ContainsKey(location2))
                                {
                                    loc_terrainFeatures.Add(location2, new Netcode.NetRef<TerrainFeature>(DictionaryCloneHelper.CreateTreeWithRandom(random, tree.treeType.Value, 0)));
                                }
                            }
                            bool hasSeed = data != null && data.SeedItemId != null && growthStage >= 5 && random.NextBool(data.SeedOnShakeChance);
                            break;
                        case HoeDirt hoeDirt:
                            Print($"{counter++}:{random.get_Index()}:HoeDirt:{pair2.Key.X},{pair2.Key.Y}");
                            random.NextBool(); // NOTE:assumes no paddy crops AND will fail on crop growing to full size
                            break;
                        case Grass grass:
                            Print($"{counter++}:{random.get_Index()}:Grass:{pair2.Key.X},{pair2.Key.Y},{grass.grassType.Value},{grass.numberOfWeeds.Value}");
                            if ((grass.grassType.Value == 1 || grass.grassType.Value == 7) && (loc.GetSeason() != Season.Winter || loc.HasMapPropertyWithValue("AllowGrassGrowInWinter")) && grass.numberOfWeeds.Value < 4)
                            {
                                grassGrowth[grass.Tile] = Utility.Clamp(grass.numberOfWeeds.Value + random.Next(1, 4), 0, 4);
                            }
                            else
                            {
                                grassGrowth[grass.Tile] = grass.numberOfWeeds.Value;
                            }
                            break;
                    }
                }
            }

            // spawnObjects
            if (loc.map != null && (loc.IsOutdoors || loc.map.Properties.ContainsKey("ForceSpawnForageables")) && !loc.map.Properties.ContainsKey("skipWeedGrowth"))
            {
                GameLocation_spawnObjects(loc, random, loc_Objects, loc_terrainFeatures);
            }
            // removewhere block
            foreach (var pair in loc.terrainFeatures.Pairs)
            {
                if (pair.Value is HoeDirt hoeDirt && (hoeDirt.crop == null || hoeDirt.crop.forageCrop.Value))
                {
                    Print($"{counter++}:{random.get_Index()}:RemoveHoeDirt:{pair.Key.X},{pair.Key.Y}");
                    if (loc.objects.TryGetValue(pair.Key, out var value) && value != null && value.IsSpawnedObject && value.isForage())
                    {
                        continue;
                    }
                    if (random.NextBool(loc.GetDirtDecayChance(pair.Key)))
                    {
                        loc_terrainFeatures.Remove(pair.Key);
                    }
                }
            }
            // loc_terrainFeatures.RemoveWhere(
            // (KeyValuePair<Vector2, NetRef<TerrainFeature>> pair) =>
            //     pair.Value.Value is HoeDirt hoeDirt && (hoeDirt.crop == null || hoeDirt.crop.forageCrop.Value) &&
            //     (!loc_Objects.TryGetValue(pair.Key, out var value) || value == null || !value.IsSpawnedObject || !value.isForage())
            //     && random.NextBool(loc.GetDirtDecayChance(pair.Key)));
            return grassGrowth;
        }

        public static void FarmUpdate(Random random, bool isRainingHere)
        {
            Farm farm = Game1.getFarm();
            ObjDict loc_Objects = DictionaryCloneHelper.CloneWithReflection(Reflector.GetValue(farm.objects, "compositeDict") as ObjDict);
            TFDict loc_terrainFeatures = DictionaryCloneHelper.CloneWithReflection(farm.terrainFeatures.FieldDict);
            var grassGrowth = GameLocation_DayUpdate(farm, random, loc_Objects, loc_terrainFeatures);
            Print($"After day update random: {random}");
            if (farm.ShouldSpawnForestFarmForage() && !Game1.IsWinter)
            {
                while (random.NextDouble() < 0.75)
                {
                    Print($"\t spawn forest farm forage {random.get_Index()}");
                    Vector2 v2 = new Vector2(random.Next(18), random.Next(farm.map.Layers[0].LayerHeight));
                    Print($"\t random vec {random.get_Index()}:{v2}");
                    if (random.NextBool() || Game1.whichFarm != 2)
                    {
                        v2 = farm.getRandomTile(random);
                        Print($"\t farm random vec {random.get_Index()}:{v2}");
                    }
                    else
                    {
                        Print($"\t using forest farm vec {random.get_Index()}:{v2}");
                    }
                    if (!loc_Objects.ContainsKey(v2) && !loc_terrainFeatures.ContainsKey(v2) && farm.CanItemBePlacedHere(v2, itemIsPassable: false, CollisionMask.All, CollisionMask.None) && !farm.hasTileAt((int)v2.X, (int)v2.Y, "AlwaysFront") && ((Game1.whichFarm == 2 && v2.X < 18f) || farm.doesTileHavePropertyNoNull((int)v2.X, (int)v2.Y, "Type", "Back").Equals("Grass")))
                    {
                        string itemId = Game1.season switch
                        {
                            Season.Spring => random.Next(4) switch
                            {
                                0 => "(O)" + 16,
                                1 => "(O)" + 22,
                                2 => "(O)" + 20,
                                _ => "(O)257",
                            },
                            Season.Summer => random.Next(4) switch
                            {
                                0 => "(O)402",
                                1 => "(O)396",
                                2 => "(O)398",
                                _ => "(O)404",
                            },
                            Season.Fall => random.Next(4) switch
                            {
                                0 => "(O)281",
                                1 => "(O)420",
                                2 => "(O)422",
                                _ => "(O)404",
                            },
                            _ => "(O)792",
                        };
                        loc_Objects.Add(v2, DictionaryCloneHelper.CreateObjectWithRandom(random, itemId, 1));
                        Print($"\t added forest farm forage {random.get_Index()}:{itemId} at {v2}");
                    }
                }
                Print($"After forest farm forage random: {random.get_Index()}");
                // fancy weeds
                if (loc_Objects.Count > 0)
                {
                    for (int k = 0; k < 6; k++)
                    {
                        if (Utility.TryGetRandom(loc_Objects, out Vector2 key, out var o2, random) && o2.IsWeeds())
                        {
                        }
                    }
                }
            }
            // add crows
            Print($"Before crows random: {random.get_Index()}");
            int numCrops = 0;
            foreach (KeyValuePair<Vector2, TerrainFeature> pair in farm.terrainFeatures.Pairs)
            {
                if (pair.Value is HoeDirt { crop: not null })
                {
                    numCrops++;
                }
            }
            int potentialCrows = Math.Min(4, numCrops / 16);
            for (int i = 0; i < potentialCrows; i++)
            {
                if (!(random.NextDouble() < 0.3))
                {
                    Print($"\tcrow[{i}] skip {random.get_Index()}");
                    continue;
                }
                else
                {
                    Print($"\tcrow[{i}] attempt {random.get_Index()}");
                }
                for (int attempts = 0; attempts < 10; attempts++)
                {
                    if (!Utility.TryGetRandom(loc_terrainFeatures, out var tile, out var feature, random) || !(feature.Value is HoeDirt dirt))
                    {
                        Print($"\t crow[{i}] attempt[{attempts}] NON_HOEDIRT {random.get_Index()}");
                        continue;
                    }
                    Print($"\t crow[{i}] attempt[{attempts}] BREAK {random.get_Index()}");
                    break;
                }
            }
            spawnWeedsAndStones(random, farm, loc_Objects, loc_terrainFeatures, isRainingHere, (Game1.season == Season.Summer) ? 30 : 20);
            Print($"After spawnWeedsAndStones: {random}");
            spawnWeeds(random, farm, loc_Objects, loc_terrainFeatures, grassGrowth, false);
            Print($"After spawnWeeds: {random}");
            HandleGrassGrowth(random, farm, loc_Objects, loc_terrainFeatures, grassGrowth);
        }

        public static void spawnWeeds(Random random, GameLocation loc, ObjDict loc_Objects, TFDict loc_terrainFeatures, Dictionary<Vector2, int> grassGrowth, bool weedsOnly)
        {
            int dayOfMonth = NightInfo.GetDayOfMonthFromDay((int)(Game1.stats.DaysPlayed + 1));
            LocationData data = loc.GetData();
            int numberOfNewWeeds = random.Next(data?.MinDailyWeeds ?? 1, (data?.MaxDailyWeeds ?? 5) + 1);
            if (dayOfMonth == 1 && Game1.IsSpring)
            {
                numberOfNewWeeds *= data?.FirstDayWeedMultiplier ?? 15;
            }
            for (int i = 0; i < numberOfNewWeeds; i++)
            {
                int numberOfTries = 0;
                while (numberOfTries < 3)
                {
                    int xCoord = random.Next(loc.map.DisplayWidth / 64);
                    int yCoord = random.Next(loc.map.DisplayHeight / 64); // 5241
                    Vector2 location = new Vector2(xCoord, yCoord);
                    loc_Objects.TryGetValue(location, out var o);
                    int grass = -1;
                    int tree = -1;
                    if (random.NextDouble() < 0.15 + (weedsOnly ? 0.05 : 0.0)) // 5246
                    {
                        grass = 1;
                    }
                    else if (!weedsOnly)
                    {
                        if (random.NextDouble() < 0.35) // 5252
                        {
                            tree = 1;
                        }
                        else if (!loc.IsFarm && random.NextDouble() < 0.35)
                        {
                            tree = 2;
                        }
                    }
                    if (tree != -1)
                    {
                        if (loc is Farm && random.NextDouble() < 0.25) // 5260
                        {
                            return;
                        }
                    }
                    else if (o == null && loc.doesTileHaveProperty(xCoord, yCoord, "Diggable", "Back") != null && loc.isTileLocationOpen(new Location(xCoord, yCoord)) && !loc.IsTileOccupiedBy(location) && !loc.isWaterTile(xCoord, yCoord))
                    {
                        if (loc.IsNoSpawnTile(location, "Grass"))
                        {
                            continue;
                        }
                        if (grass != -1 && loc.GetSeason() != Season.Winter && loc.Name == "Farm")
                        {
                            if (Game1.GetFarmTypeID() == "MeadowlandsFarm" && random.NextDouble() < 0.1)
                            {
                                grass = 7;
                            }
                            int numberOfWeeds = random.Next(1, 3);
                            loc_terrainFeatures.Add(location, new Netcode.NetRef<TerrainFeature>(new Grass(grass, numberOfWeeds)));
                            // loc.terrainFeatures.Add(location, new Grass(grass, numberOfWeeds));
                            grassGrowth.Add(location, numberOfWeeds);
                        }
                    }
                    numberOfTries++;
                }
            }
        }

        public static void HandleGrassGrowth(Random random, Farm farm, ObjDict loc_Objects, TFDict loc_terrainFeatures, Dictionary<Vector2, int> grassGrowth)
        {
            void growWeedGrass(Random random, Farm farm, int iterations)
            {
                Print($"sim:growWeedGrass:{loc_terrainFeatures.Count}");
                for (int i = 0; i < iterations; i++)
                {
                    KeyValuePair<Vector2, Netcode.NetRef<TerrainFeature>>[] array = loc_terrainFeatures.ToArray();
                    for (int j = 0; j < array.Length; j++)
                    {
                        KeyValuePair<Vector2, Netcode.NetRef<TerrainFeature>> pair = array[j];
                        if (pair.Value.Value is not Grass)
                        {
                            continue;
                        }
                        double d = random.NextDouble();
                        if (!(d < 0.65))
                        {
                            Print($"sim:growWeedGrass:check:fail:{random.get_Index()}");
                            continue;
                        }
                        Print($"sim:growWeedGrass:check:success:{random.get_Index()}");
                        if (grassGrowth[pair.Key] < 4)
                        {
                            random.Next(3);
                            Print($"sim:growWeedGrass:grow:{random.get_Index()}");
                        }
                        else
                        {
                            if (grassGrowth[pair.Key] < 4)
                            {
                                continue;
                            }
                            int xCoord = (int)pair.Key.X;
                            int yCoord = (int)pair.Key.Y;
                            Vector2[] adjacentTileLocationsArray = Utility.getAdjacentTileLocationsArray(pair.Key);
                            for (int k = 0; k < adjacentTileLocationsArray.Length; k++)
                            {
                                Vector2 tile = adjacentTileLocationsArray[k];
                                if (farm.isTileOnMap(xCoord, yCoord) && !farm.IsTileBlockedBy(tile) && !loc_Objects.ContainsKey(tile) && !loc_terrainFeatures.ContainsKey(tile) && !grassGrowth.ContainsKey(tile) && farm.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Diggable", "Back") != null && !farm.IsNoSpawnTile(tile))
                                {
                                    if (random.NextDouble() < 0.25)
                                    {
                                        Print($"sim:growWeedGrass:expand:{random.get_Index()}");
                                        loc_terrainFeatures.Add(tile, new Netcode.NetRef<TerrainFeature>(new Grass(1, random.Next(1, 3))));
                                        Print($"sim:growWeedGrass:expand:numWeeds:{random.get_Index()}");
                                    }
                                    else
                                    {
                                        Print($"sim:growWeedGrass:expand:fail:{random.get_Index()}");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            foreach (var pair in loc_terrainFeatures)
            {
                Print($"sim:growGrassWeed:tfs:{pair.Value.Value.GetType().Name}:{pair.Key.X},{pair.Key.Y}");
            }
            foreach (var pair in loc_Objects)
            {
                Print($"sim:growGrassWeed:Objects:{pair.Value.Name}:{pair.Key.X},{pair.Key.Y}:{pair.Value.QualifiedItemId}");
            }
            growWeedGrass(random, farm, 1);
        }

        public static void spawnWeedsAndStones(Random random, GameLocation loc, ObjDict loc_Objects, TFDict loc_terrainFeatures, bool IsRainingHere, int numDebris = -1, bool weedsOnly = false, bool spawnFromOldWeeds = true)
        {
            HashSet<Vector2> spawnedTrees = new HashSet<Vector2>();
            bool greenRain = false;
            int daysPlayed = (int)(Game1.stats.DaysPlayed + 1);
            int numWeedsAndStones = ((numDebris != -1) ? numDebris : ((random.NextDouble() < 0.95) ? ((random.NextDouble() < 0.25) ? random.Next(10, 21) : random.Next(5, 11)) : 0));
            if (IsRainingHere)
            {
                numWeedsAndStones *= 2;
            }
            if (NightInfo.GetDayOfMonthFromDay(daysPlayed) == 1)
            {
                numWeedsAndStones *= 5;
            }

            if (loc_Objects.Count <= 0 && spawnFromOldWeeds)
            {
                return;
            }

            if (!(loc is Farm))
            {
                numWeedsAndStones /= 2;
            }
            Print($"numWeedsAndStones:{random.get_Index()}:{numWeedsAndStones}:{IsRainingHere}");
            for (int i = 0; i < numWeedsAndStones; i++)
            {
                Vector2 v = (spawnFromOldWeeds ? new Vector2(random.Next(-1, 2), random.Next(-1, 2)) : new Vector2(random.Next(loc.map.Layers[0].LayerWidth), random.Next(loc.map.Layers[0].LayerHeight)));
                Print($"v_init:{random.get_Index()}");
                if (!spawnFromOldWeeds && loc is IslandWest)
                {
                    v = new Vector2(random.Next(57, 97), random.Next(44, 68));
                }
                while (spawnFromOldWeeds && v.Equals(Vector2.Zero))
                {
                    v = new Vector2(random.Next(-1, 2), random.Next(-1, 2));
                }
                Print($"v_final:{random.get_Index()}");
                Vector2 fromTile = Vector2.Zero;
                StardewValley.Object fromObj = null;
                if (spawnFromOldWeeds)
                {
                    Utility.TryGetRandom(loc_Objects, out fromTile, out fromObj, random);
                    Print($"TryGetRandom:{random.get_Index()}");
                }
                Vector2 baseVect = (spawnFromOldWeeds ? fromTile : Vector2.Zero);
                if ((loc is Mountain && v.X + baseVect.X > 100f) || loc is IslandNorth)
                {
                    continue;
                }
                bool num = loc is Farm || loc is IslandWest;
                int checked_tile_x = (int)(v.X + baseVect.X);
                int checked_tile_y = (int)(v.Y + baseVect.Y);
                Vector2 checked_tile = v + baseVect;
                int health = 1;
                bool is_valid_tile = false;
                bool tile_is_diggable = loc.doesTileHaveProperty(checked_tile_x, checked_tile_y, "Diggable", "Back") != null;
                if (num == tile_is_diggable && !loc.IsNoSpawnTile(checked_tile) && loc.doesTileHaveProperty(checked_tile_x, checked_tile_y, "Type", "Back") != "Wood")
                {
                    bool is_tile_clear = false;
                    if (loc.CanItemBePlacedHere(checked_tile) && !loc_terrainFeatures.ContainsKey(checked_tile) && !spawnedTrees.Contains(checked_tile))
                    {
                        is_tile_clear = true;
                    }
                    else if (spawnFromOldWeeds)
                    {
                        if (loc_Objects.TryGetValue(checked_tile, out var tileObj))
                        {
                            if (greenRain)
                            {
                                is_tile_clear = false;
                            }
                            else if (!tileObj.IsTapper())
                            {
                                is_tile_clear = true;
                            }
                        }
                        if (!is_tile_clear && loc_terrainFeatures.TryGetValue(checked_tile, out var terrainFeature) && (terrainFeature.Value is HoeDirt || terrainFeature.Value is Flooring))
                        {
                            is_tile_clear = !greenRain && loc.getLargeTerrainFeatureAt(checked_tile_x, checked_tile_y) == null;
                        }
                    }
                    if (is_tile_clear)
                    {
                        if (spawnFromOldWeeds)
                        {
                            is_valid_tile = true;
                        }
                        else if (!loc_Objects.ContainsKey(checked_tile))
                        {
                            is_valid_tile = true;
                        }
                    }
                }
                if (!is_valid_tile)
                {
                    continue;
                }
                string whatToAdd = null;
                if (random.NextBool() && !weedsOnly && (!spawnFromOldWeeds || fromObj.IsBreakableStone() || fromObj.IsTwig()))
                {
                    Print($"whatToAddCheck:{random.get_Index()}");
                    // whatToAdd = random.Choose("(O)294", "(O)295", "(O)343", "(O)450");
                    whatToAdd = random.Choose("294", "295", "343", "450");
                    Print($"randomChoose:{random.get_Index()}");
                }
                else if (!spawnFromOldWeeds || fromObj.IsWeeds())
                {
                    Print($"whatToAddCheck:{random.get_Index()}");
                    whatToAdd = GameLocation.getWeedForSeason(random, loc.GetSeason());
                    if (whatToAdd.StartsWith("(O)"))
                    {
                        whatToAdd = whatToAdd.Substring(3);
                    }
                    Print($"getWeedForSeason:{random.get_Index()}:{whatToAdd}");
                    if (loc.IsGreenRainingHere())
                    {
                        if (loc.doesTileHavePropertyNoNull((int)(v.X + baseVect.X), (int)(v.Y + baseVect.Y), "Type", "Back") == (loc.IsFarm ? "Dirt" : "Grass"))
                        {
                            int which = random.Next(8);
                            whatToAdd = "(O)GreenRainWeeds" + which;
                            if (which == 2 || which == 3 || which == 7)
                            {
                                health = 2;
                            }
                        }
                        else
                        {
                            whatToAdd = null;
                        }
                    }
                }
                else
                {
                    Print($"whatToAddCheck:{random.get_Index()}");
                }
                if (loc is Farm && !spawnFromOldWeeds && random.NextDouble() < 0.05 && !loc.terrainFeatures.ContainsKey(checked_tile) && !spawnedTrees.Contains(checked_tile))
                {
                    random.Next(3);
                    random.Next(3);
                    // new tree
                    Print($"SpawnedTree.ctor:{random.get_Index()}");
                    spawnedTrees.Add(checked_tile);
                    continue;
                }
                if (whatToAdd == null)
                {
                    continue;
                }
                // ItemRegistry.Create
                // random.Next();
                loc_Objects.TryAdd(checked_tile, DictionaryCloneHelper.CreateObjectWithRandom(random, whatToAdd, 1));
                Print($"SpawnedObject.ctor:{random.get_Index()}:{whatToAdd}");
            }
        }

        public static class DefaultResolversClone
        {
            public static bool SEASON_DAY(string[] query)
            {
                var date = WorldDate.ForDaysPlayed((int)Game1.stats.DaysPlayed);
                for (int i = 1; i < query.Length; i += 2)
                {
                    if (!ArgUtility.TryGetEnum<Season>(query, i, out var season, out var error, "Season season") || !ArgUtility.TryGetInt(query, i + 1, out var day, out error, "int day"))
                    {
                        throw new Exception(error);
                    }
                    if (date.Season == season && date.DayOfMonth == day)
                    {
                        return true;
                    }
                }
                return false;
            }

            public static bool RandomImpl(Random random, string[] query, int skipArguments)
            {
                if (!ArgUtility.TryGetFloat(query, skipArguments, out var chance, out var error, "float chance"))
                {
                    throw new Exception(error);
                }
                bool addDailyLuck = false;
                for (int i = skipArguments + 1; i < query.Length; i++)
                {
                    if (query[i].EqualsIgnoreCase("@addDailyLuck"))
                    {
                        addDailyLuck = true;
                    }
                }
                if (addDailyLuck)
                {
                    chance += (float)Game1.player.DailyLuck;
                }
                return random.NextDouble() < (double)chance;
            }

            public static bool RANDOM(string[] query, Random random)
            {
                return RandomImpl(random, query, 1);
            }

            public static bool DAYS_PLAYED(string[] query)
            {
                if (!ArgUtility.TryGetInt(query, 1, out var minDaysPlayed, out var error, "int minDaysPlayed") || !ArgUtility.TryGetOptionalInt(query, 2, out var maxDaysPlayed, out error, int.MaxValue, "int maxDaysPlayed"))
                {
                    throw new Exception(error);
                }
                uint daysPlayed = Game1.stats.DaysPlayed + 1;
                if (daysPlayed >= minDaysPlayed)
                {
                    return daysPlayed <= maxDaysPlayed;
                }
                return false;
            }
            public static bool AnyArgMatches(string[] query, int startAt, Func<string, bool?> check)
            {
                for (int i = startAt; i < query.Length; i++)
                {
                    bool? flag = check(query[i]);
                    if (flag.HasValue)
                    {
                        if (flag == true)
                        {
                            return true;
                        }
                        continue;
                    }
                    return false;
                }
                return false;
            }
            public static bool DAY_OF_MONTH(string[] query)
            {
                var date = WorldDate.ForDaysPlayed((int)Game1.stats.DaysPlayed);
                return AnyArgMatches(query, 1, delegate (string rawDay)
                {
                    if (int.TryParse(rawDay, out var result))
                    {
                        return date.DayOfMonth == result;
                    }
                    if (rawDay.EqualsIgnoreCase("even"))
                    {
                        return date.DayOfMonth % 2 == 0;
                    }
                    if (rawDay.EqualsIgnoreCase("odd"))
                    {
                        return date.DayOfMonth % 2 == 1;
                    }
                    throw new Exception($"Invalid day of month specifier: {rawDay}");
                });
            }

            /// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
            public static bool IS_GREEN_RAIN_DAY(string[] query)
            {
                var tomorrow = WorldDate.ForDaysPlayed((int)Game1.stats.DaysPlayed);
                tomorrow.TotalDays++;
                return Utility.isGreenRainDay(tomorrow.DayOfMonth, tomorrow.Season);
            }

            public static bool SYNCED_RANDOM(string[] query)
            {
                if (!ArgUtility.TryGet(query, 1, out var interval, out var error, allowBlank: true, "string interval") || !ArgUtility.TryGet(query, 2, out var key, out error, allowBlank: true, "string key") || !Utility.TryCreateIntervalRandom(interval, key, out var syncedRandom, out error))
                {
                    throw new Exception(error);
                }
                return RandomImpl(syncedRandom, query, 3);
            }
            public static bool SYNCED_SUMMER_RAIN_RANDOM(string[] query)
            {
                Random random = Utility.CreateDaySaveRandom(Game1.hash.GetDeterministicHashCode("summer_rain_chance"));
                WorldDate date = WorldDate.ForDaysPlayed((int)Game1.stats.DaysPlayed);
                float chanceToRain = 0.12f + (float)date.DayOfMonth * 0.003f;
                return random.NextBool(chanceToRain);
            }
            public static bool SEASON(string[] query)
            {
                for (int i = 1; i < query.Length; i++)
                {
                    if (!ArgUtility.TryGetEnum<Season>(query, i, out var season, out var error, "Season season"))
                    {
                        throw new Exception(error);
                    }
                    if (Game1.season == season)
                    {
                        return true;
                    }
                }
                return false;
            }
            public static bool YEAR(string[] query)
            {
                if (!ArgUtility.TryGetInt(query, 1, out var minYear, out var error, "int minYear") || !ArgUtility.TryGetOptionalInt(query, 2, out var maxYear, out error, int.MaxValue, "int maxYear"))
                {
                    throw new Exception(error);
                }
                int year = Game1.year;
                if (year >= minYear)
                {
                    return year <= maxYear;
                }
                return false;
            }

            public static bool PLAYER_HAS_MAIL(string[] query)
            {
                if (!ArgUtility.TryGet(query, 1, out var playerKey, out var error, allowBlank: true, "string playerKey") || !ArgUtility.TryGet(query, 2, out var mailId, out error, allowBlank: true, "string mailId") || !ArgUtility.TryGetOptional(query, 3, out var rawType, out error, "any", allowBlank: true, "string rawType"))
                {
                    throw new Exception(error);
                }
                string type = rawType?.ToLower();
                switch (type)
                {
                    case "mailbox":
                        return Game1.player.mailbox.Contains(mailId);
                    case "tomorrow":
                        return Game1.player.mailForTomorrow.Contains(mailId);
                    case "received":
                        return Game1.player.mailReceived.Contains(mailId);
                    case "any":
                        return Game1.player.hasOrWillReceiveMail(mailId);
                    default:
                        throw new Exception($"Invalid mail type specifier: {type}");
                }
            }
        }

        /*
"WeatherConditions": [
      {
        "Id": "FirstVisitSun",
        "Condition": "!PLAYER_HAS_MAIL Any Visited_Island",
        "Weather": "Sun"
      },
      {
        "Id": "Rain",
        "Condition": "RANDOM .24",
        "Weather": "Rain"
      },
      {
        "Id": "Default",
        "Condition": null,
        "Weather": "Sun"
      }
    ],
        */

        public static bool CheckConditions(string queryString, Random random)
        {
            if (queryString == null)
            {
                return true;
            }
            string[] tokens = queryString.Split(", ");
            foreach (string token in tokens)
            {
                string[] parts = token.Split(' ');
                bool negate = false;
                if (parts[0].StartsWith("!"))
                {
                    negate = true;
                    parts[0] = parts[0].Substring(1);
                }
                switch (parts[0].Trim())
                {
                    case "RANDOM":
                        if (DefaultResolversClone.RANDOM(parts, random) == negate)
                        {
                            return false;
                        }
                        break;
                    case "SEASON_DAY":
                        if (DefaultResolversClone.SEASON_DAY(parts) == negate)
                        {
                            return false;
                        }
                        break;
                    case "IS_GREEN_RAIN_DAY":
                        if (DefaultResolversClone.IS_GREEN_RAIN_DAY(parts) == negate)
                        {
                            return false;
                        }
                        break;
                    case "SYNCED_RANDOM":
                        if (DefaultResolversClone.SYNCED_RANDOM(parts) == negate)
                        {
                            return false;
                        }
                        break;
                    case "SYNCED_SUMMER_RAIN_RANDOM":
                        if (DefaultResolversClone.SYNCED_SUMMER_RAIN_RANDOM(parts) == negate)
                        {
                            return false;
                        }
                        break;
                    case "DAYS_PLAYED":
                        if (DefaultResolversClone.DAYS_PLAYED(parts) == negate)
                        {
                            return false;
                        }
                        break;
                    case "DAY_OF_MONTH":
                        if (DefaultResolversClone.DAY_OF_MONTH(parts) == negate)
                        {
                            return false;
                        }
                        break;
                    case "SEASON":
                        if (DefaultResolversClone.SEASON(parts) == negate)
                        {
                            return false;
                        }
                        break;
                    case "YEAR":
                        if (DefaultResolversClone.YEAR(parts) == negate)
                        {
                            return false;
                        }
                        break;
                    case "PLAYER_HAS_MAIL":
                        if (DefaultResolversClone.PLAYER_HAS_MAIL(parts) == negate)
                        {
                            return false;
                        }
                        break;
                    default:
                        throw new NotImplementedException($"Condition '{token}' not implemented in simulation.");
                }
            }
            return true;
        }

        public static bool UpdateWeather(Random random)
        {
            WorldDate date = WorldDate.ForDaysPlayed((int)Game1.stats.DaysPlayed); // what a fucked up function
            string weatherForTomorrow = Game1.getWeatherModificationsForDate(date, Game1.weatherForTomorrow);
            bool isRaining = false;
            bool isGreenRain = false;
            bool isLightning = false;
            bool isDebrisWeather = false;
            bool isSnowing = false;
            switch (weatherForTomorrow)
            {
                case "Rain":
                    isRaining = true;
                    break;
                case "GreenRain":
                    isGreenRain = true;
                    break;
                case "Storm":
                    isRaining = true;
                    isLightning = true;
                    break;
                case "Wind":
                    isDebrisWeather = true;
                    break;
                case "Snow":
                    isSnowing = true;
                    break;
            }
            foreach (KeyValuePair<string, LocationContextData> pair in Game1.locationContextData)
            {
                foreach (WeatherCondition weatherCondition in pair.Value.WeatherConditions)
                {
                    Print($"'{weatherCondition.Id}:{weatherCondition.Condition}:{random.get_Index()}");
                    if (CheckConditions(weatherCondition.Condition, random))
                    {
                        // TODO: the weather check may depend on things like the actual day
                        // mostly because it will shortcircuit the checks in order
                        break;
                    }
                }
            }

            // handle debris weather case
            Print($"After weather condition random: {random}");
            if (isDebrisWeather)
            {
                int debrisToMake = random.Next(16, 64);
                Print($"Debris to make: {debrisToMake}");
                for (int i = 0; i < debrisToMake; i++)
                {
                    random.NextDouble();
                    random.NextDouble();
                    random.NextDouble();
                    random.NextDouble();
                    random.NextDouble();
                    random.NextDouble();
                }
            }

            return isRaining;
        }

        public static void RefreshQuestOfTheDay(int daysPlayed, Random random)
        {
            void ResourceCollection(Random random)
            {
                random.Next();
            } // ItemRegistry.Create

            int maxTimesReachedMineBottom()
            {
                int result = 0;
                foreach (Farmer farmer in Game1.getOnlineFarmers())
                {
                    result = Math.Max(result, farmer.timesReachedMineBottom);
                }
                return result;
            }
            void SlayMonster(Random random)
            {
                Random initRandom = Utility.CreateRandom(Game1.uniqueIDForThisGame, daysPlayed);
                for (int i = 0; i < initRandom.Next(1, 100); i++)
                {
                    initRandom.Next();
                }
                List<string> possibleMonsters = new List<string>();
                int mineLevel = Utility.GetAllPlayerDeepestMineLevel();
                if (mineLevel < 39)
                {
                    possibleMonsters.Add("Green Slime");
                    if (mineLevel > 10)
                    {
                        possibleMonsters.Add("Rock Crab");
                    }
                    if (mineLevel > 30)
                    {
                        possibleMonsters.Add("Duggy");
                    }
                }
                else if (mineLevel < 79)
                {
                    possibleMonsters.Add("Frost Jelly");
                    if (mineLevel > 70)
                    {
                        possibleMonsters.Add("Skeleton");
                    }
                    possibleMonsters.Add("Dust Spirit");
                }
                else
                {
                    possibleMonsters.Add("Sludge");
                    possibleMonsters.Add("Ghost");
                    possibleMonsters.Add("Lava Crab");
                    possibleMonsters.Add("Squid Kid");
                }
                string monsterName = initRandom.ChooseFrom(possibleMonsters);
                string[] monsterInfo;
                switch (monsterName)
                {
                    case "Green Slime":
                    case "Sludge":
                    case "Frost Jelly":
                        monsterInfo = DataLoader.Monsters(Game1.content)["Green Slime"].Split('/');
                        break;
                    default:
                        monsterInfo = DataLoader.Monsters(Game1.content)[monsterName].Split('/');
                        break;
                }
                int numSplits = ArgUtility.SplitBySpace(monsterInfo[6]).Length;
                for (int i = 0; i < numSplits; i += 2)
                {
                    random.Next();
                }
                if (maxTimesReachedMineBottom() >= 1)
                {
                    random.Next();
                    random.Next();
                }
            }
            void Fishing(Random random) { random.Next(); } // ItemRegistry.Create
            void Socialize(Random random) { }
            void ItemDelivery(Random random) { random.Next(); } // ItemRegistry.Create
            // select the quest
            int dayOfMonth = NightInfo.GetDayOfMonthFromDay(daysPlayed);
            double d = Utility.CreateRandom(daysPlayed, Game1.uniqueIDForThisGame / 2, 100.0, daysPlayed * 777).NextDouble();
            Print($"Quest selection random: {random} \t daysPlayed: {daysPlayed} \t dayOfMonth: {dayOfMonth} \t d: {d}");
            if (d < 0.08)
            {
                Print("Resource Collection Quest");
                ResourceCollection(random);
                return;
            }
            if (d < 0.2 && MineShaft.lowestLevelReached > 0 && InstanceCurrentPlayer.Get(0).Player.stats.DaysPlayed > 5)
            {
                Print("Slay Monster Quest");
                SlayMonster(random);
                return;
            }
            if (d < 0.5)
            {
                Print("No Quest");
                // no quest
                return;
            }
            if (d < 0.6)
            {
                Print("Fishing Quest");
                Fishing(random);
                return;
            }
            if (d < 0.66 && Game1.shortDayNameFromDayOfSeason(dayOfMonth).Equals("Mon"))
            {
                Print("Socialize Quest");
                Socialize(random);
                return;
            }

            Print("Item Delivery Quest");
            ItemDelivery(random);

        }
    }
}