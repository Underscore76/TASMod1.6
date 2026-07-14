using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.GameData.GarbageCans;
using StardewValley.Internal;
using StardewValley.Locations;
using StardewValley.Monsters;
using StardewValley.Objects;
using StardewValley.Projectiles;
using StardewValley.TerrainFeatures;
using xTile;
using xTile.Layers;
using xTile.ObjectModel;

namespace TASMod.Helpers
{
    public class LocationHelpers
    {
        public static Dictionary<
            string,
            IEnumerable<KeyValuePair<Vector2, StardewValley.Object>>
        > AllForage
        {
            get
            {
                Dictionary<
                    string,
                    IEnumerable<KeyValuePair<Vector2, StardewValley.Object>>
                > forage =
                    new Dictionary<
                        string,
                        IEnumerable<KeyValuePair<Vector2, StardewValley.Object>>
                    >();
                foreach (GameLocation location in Game1.locations)
                {
                    if (location.Name == "Desert" && !Game1.player.hasOrWillReceiveMail("ccVault"))
                        continue;
                    forage.Add(location.Name, LocationForage(location));
                }
                return forage;
            }
        }

        public static IEnumerable<KeyValuePair<Vector2, StardewValley.Object>> LocationForage(
            GameLocation location
        )
        {
            if (location == null)
                return null;
            return location.Objects.Pairs.Where(
                (pair) =>
                {
                    return IsForage(location, pair.Value.Category, pair.Value.ParentSheetIndex);
                }
            );
        }

        private static bool IsForage(GameLocation location, int category, int parentSheetIndex)
        {
            if (
                category != -79
                && category != -81
                && category != -80
                && category != -75
                && !(location is Beach)
            )
            {
                return (int)parentSheetIndex == 430 || parentSheetIndex == 590;
            }
            return true;
        }
        private static Dictionary<string, Tuple<Vector2, int>> TrashCans = new()
        {
            {"JodiAndKent", new Tuple<Vector2, int>(new Vector2(13, 86), 0)},
            {"EmilyAndHaley", new Tuple<Vector2, int>(new Vector2(19, 89), 1)},
            {"Mayor", new Tuple<Vector2, int>(new Vector2(56, 85), 2)},
            {"Museum", new Tuple<Vector2, int>(new Vector2(108, 91), 3)},
            {"Blacksmith ", new Tuple<Vector2, int>(new Vector2(97, 80), 4)},
            {"Saloon", new Tuple<Vector2, int>(new Vector2(47, 70), 5)},
            {"Evelyn", new Tuple<Vector2, int>(new Vector2(52, 63), 6)},
            {"JojaMart", new Tuple<Vector2, int>(new Vector2(110, 56), 7)}
        };

        public static List<(string, Vector2, string)> GetTrashCans()
        {
            List<(string, Vector2, string)> trash = new();
            if (Game1.currentLocation == null) return trash;

            var player = InstanceCurrentPlayer.Get(0).Player;
            var location = Game1.getLocationFromName("Town");

            foreach (var can in TrashCans)
            {
                string item = GetGarbageItem(can.Key, location, player);
                if (item == "")
                    continue;
                trash.Add((can.Key, can.Value.Item1, item));
            }
            return trash;
        }

        private static string GetGarbageItem(string can, GameLocation location, Farmer player)
        {
            GarbageCanData allData = DataLoader.GarbageCans(Game1.content);
            GarbageCanEntryData data = allData.GarbageCans.GetValueOrDefault(can);
            float baseChance = ((data != null && data.BaseChance > 0f) ? data.BaseChance : allData.DefaultBaseChance);
            baseChance += (float)player.DailyLuck;
            if (player.stats.Get("Book_Trash") != 0)
            {
                baseChance += 0.2f;
            }
            Random garbageRandom = Utility.CreateDaySaveRandom(777 + Game1.hash.GetDeterministicHashCode(can));
            int prewarm = garbageRandom.Next(0, 100);
            for (int i = 0; i < prewarm; i++)
            {
                garbageRandom.NextDouble();
            }
            prewarm = garbageRandom.Next(0, 100);
            for (int j = 0; j < prewarm; j++)
            {
                garbageRandom.NextDouble();
            }
            bool baseChancePassed = garbageRandom.NextDouble() < (double)baseChance;
            ItemQueryContext itemQueryContext = new ItemQueryContext(location, player, garbageRandom, "garbage data '" + can + "'");
            List<GarbageCanItemData>[] array = new List<GarbageCanItemData>[3]
            {
                allData.BeforeAll,
                data?.Items,
                allData.AfterAll
            };
            foreach (List<GarbageCanItemData> itemList in array)
            {
                if (itemList == null)
                {
                    continue;
                }
                foreach (GarbageCanItemData entry in itemList)
                {
                    if (string.IsNullOrWhiteSpace(entry.Id))
                    {
                    }
                    else if ((baseChancePassed || entry.IgnoreBaseChance) && GameStateQuery.CheckConditions(entry.Condition, location, null, null, null, garbageRandom))
                    {
                        // try to resolve a random item
                        string result = TryResolveRandomItem(garbageRandom, entry, itemQueryContext);
                        switch (result)
                        {
                            case "":
                                continue;
                            case "RANDOM_BASE_SEASON_ITEM":
                                return DropInfo.ObjectName(Utility.getRandomItemFromSeason(random: garbageRandom, season: location.GetSeason(), forQuest: false));
                            case "DISH_OF_THE_DAY":
                                return Game1.dishOfTheDay.Name;
                            case "(O)CalicoEgg":
                                return "Calico Egg";
                            case "(H)66":
                                return "Garbage Hat";
                            case "(F)TrashCatalogue":
                                return "Trash Catalogue";
                            default:
                                return DropInfo.ObjectName(result.Substring(3));
                        }
                    }
                }
            }
            return "";
        }
        public static string TryResolveRandomItem(Random random, GarbageCanItemData data, ItemQueryContext itemQueryContext)
        {
            string itemId = data.ItemId;
            List<string> randomItemId = data.RandomItemId;
            if (randomItemId != null && randomItemId.Any())
            {
                itemId = random.ChooseFrom(data.RandomItemId);
            }
            return itemId;
        }
    }


    public class LocationInfo
    {
        public int index;
        public GameLocation Location;
        public bool Active => Location != null;
        public string Name => Location?.Name;
        public IEnumerable<NPC> Characters => Location?.characters.Where((n) => (!(n is Monster)));
        public IEnumerable<NPC> Monsters => Location?.characters.Where((n) => (n is Monster));
        public IEnumerable<KeyValuePair<Vector2, StardewValley.Object>> Forage => LocationHelpers.LocationForage(Location);
        public bool IsMines => Location is MineShaft;
        public int MineLevel => (Location as MineShaft).mineLevel;
        public int StonesLeftOnThisLevel()
        {
            if (Location is MineShaft mine)
            {
                return mine.stonesLeftOnThisLevel;
            }
            return 0;
        }
        public bool LadderHasSpawned()
        {
            if (Location is MineShaft mine)
            {
                return mine.ladderHasSpawned;
            }
            return false;
        }
        public int EnemyCount
        {
            get
            {
                if (Location is MineShaft mine)
                    return mine.EnemyCount;
                return 0;
            }
        }
        public bool FogActive
        {
            get
            {
                if (Location is MineShaft mine)
                    return mine.isFogUp.Value;
                return false;
            }
        }
        public Random mineRandom
        {
            get
            {
                if (Location is MineShaft mine)
                {
                    return mine.mineRandom;
                }
                return null;
            }
        }
        public bool MustKillAllMonstersToAdvance()
        {
            if (Location is MineShaft mine)
            {
                return mine.mustKillAllMonstersToAdvance();
            }
            return false;
        }

        public bool HasLadder(out Vector2 location)
        {
            location = Vector2.Zero;
            if (Location is MineShaft mine)
            {
                if (mine.ladderHasSpawned || mine.loadedMapNumber == 10 || mine.loadedMapNumber == 20)
                {
                    // have to find it...
                    xTile.Dimensions.Size mapDims = mine.map.Layers[0].LayerSize;
                    for (int i = 0; i < mapDims.Width; i++)
                    {
                        for (int j = 0; j < mapDims.Height; j++)
                        {
                            int index = mine.getTileIndexAt(i, j, "Buildings");
                            if (index == 173 || index == 174)
                            {
                                location = new Vector2(i, j);
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
    }

    public class InstanceCurrentLocation
    {
        public static LocationInfo Get(int index)
        {
            if (GameRunner.instance.gameInstances.Count == 1)
            {
                return new LocationInfo { index = index, Location = Game1.currentLocation };
            }
            if (index < 0 || index >= GameRunner.instance.gameInstances.Count)
                return new LocationInfo { index = index, Location = null };
            var location = GameRunner.instance.gameInstances[index]?.instanceGameLocation;
            return new LocationInfo { index = index, Location = location };
        }
    }
}
