using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Constants;
using StardewValley.Extensions;
using StardewValley.Locations;
using TASMod.Helpers.Pathing;

namespace TASMod.Helpers
{
    public class MinesFloor
    {
        public class UpdateState
        {
            public int Index;
            private int last_mineLevel;
            private int last_miningLevel;
            private int last_luckLevel;
            private int last_stonesLeftOnThisLevel;
            private int last_characterCount;
            public UpdateState(int index)
            {
                Index = index;
                Reset();
            }
            public void Reset()
            {
                last_mineLevel = -1;
                last_miningLevel = -1;
                last_luckLevel = -1;
                last_stonesLeftOnThisLevel = -1;
                last_characterCount = -1;
            }

            public bool ShouldUpdate()
            {
                var locationInfo = InstanceCurrentLocation.Get(Index);
                var playerInfo = InstanceCurrentPlayer.Get(Index);
                if (locationInfo.IsMines)
                {
                    return (last_mineLevel != locationInfo.MineLevel)
                        || (last_miningLevel != playerInfo.Player.MiningLevel)
                        || (last_luckLevel != playerInfo.Player.LuckLevel)
                        || (last_stonesLeftOnThisLevel != locationInfo.StonesLeftOnThisLevel())
                        || (last_characterCount != locationInfo.EnemyCount);
                }
                return false;
            }

            public void Postfix()
            {
                var locationInfo = InstanceCurrentLocation.Get(Index);
                var playerInfo = InstanceCurrentPlayer.Get(Index);
                last_mineLevel = locationInfo.MineLevel;
                last_miningLevel = playerInfo.Player.MiningLevel;
                last_luckLevel = playerInfo.Player.LuckLevel;
                last_stonesLeftOnThisLevel = locationInfo.StonesLeftOnThisLevel();
                last_characterCount = locationInfo.EnemyCount;
            }
        }

        private UpdateState state;
        private Map map;
        private Dictionary<Vector2, int> ladderCounts;
        private List<Tuple<Vector2, int>> sortedLadderTiles;
        private int minLadderCount;
        private bool hasLadder;
        private Vector2 ladderTile;
        private Dictionary<Vector2, List<Tuple<string, int>>> stoneContents;
        private PathFinder pathFinder;

        public MinesFloor(int index)
        {
            state = new UpdateState(index);
            sortedLadderTiles = new List<Tuple<Vector2, int>>();
            ladderCounts = new Dictionary<Vector2, int>();
            stoneContents = new Dictionary<Vector2, List<Tuple<string, int>>>();
            pathFinder = new PathFinder();
            map = new Map();
        }

        public void SetIndex(int index)
        {
            if (state.Index == index)
                return;

            state.Index = index;
            Reset();
        }
        public void Reset()
        {
            state.Reset();
            ladderCounts.Clear();
            stoneContents.Clear();
            hasLadder = false;
            minLadderCount = int.MaxValue;
            ladderTile = Vector2.Zero;
        }

        public Dictionary<Vector2, List<Tuple<string, int>>> GetStoneContents()
        {
            UpdateStoneContents();
            return stoneContents;
        }
        public Dictionary<Vector2, int> GetLadderCounts()
        {
            UpdateLadders();
            return ladderCounts;
        }
        public List<Tuple<Vector2, int>> GetSortedLadderTiles()
        {
            UpdateLadders();
            return sortedLadderTiles;
        }
        public bool HasLadder()
        {
            UpdateLadders();
            return hasLadder;
        }
        public Vector2 GetLadderTile()
        {
            UpdateLadders();
            return ladderTile;
        }
        public int GetMinLadderCount()
        {
            UpdateLadders();
            return minLadderCount;
        }


        public void Update()
        {
            map.Build(InstanceCurrentLocation.Get(state.Index).Location);
            UpdateLadders(true);
            UpdateStoneContents(true);
        }

        public int EvalLadderTile(PlayerInfo playerInfo, MineShaft mine, Vector2 tile)
        {
            if (mine.ladderHasSpawned || mine.stonesLeftOnThisLevel == 0)
            {
                return -1;
            }
            int farmerLuckLevel = playerInfo.Player.LuckLevel;
            double chanceForLadderDown =
                0.02 + (double)farmerLuckLevel / 100.0 + playerInfo.Player.DailyLuck / 5.0;
            if (mine.EnemyCount == 0)
            {
                chanceForLadderDown += 0.04;
            }
            for (int i = 0; i < mine.stonesLeftOnThisLevel; i++)
            {
                Random r = Utility.CreateDaySaveRandom(tile.X * 1000, tile.Y, mine.mineLevel);
                r.NextDouble();
                if (
                    r.NextDouble()
                    < chanceForLadderDown
                        + 1.0 / (double)Math.Max(1, mine.stonesLeftOnThisLevel - i)
                )
                {
                    return i;
                }
            }
            return -1;
        }
        public void UpdateLadders(bool force = false)
        {
            if (!force && !state.ShouldUpdate())
                return;
            state.Postfix();

            LocationInfo locationInfo = InstanceCurrentLocation.Get(state.Index);
            PlayerInfo playerInfo = InstanceCurrentPlayer.Get(state.Index);
            ladderCounts.Clear();
            sortedLadderTiles.Clear();
            hasLadder = locationInfo.HasLadder(out var ladder);
            if (hasLadder)
            {
                ladderTile = ladder;
                minLadderCount = 0;
            }
            else
            {
                Vector2 baseTile = playerInfo.CurrentTile;
                if (locationInfo.Name != playerInfo.Player.currentLocation.Name)
                {
                    baseTile = (locationInfo.Location as MineShaft).tileBeneathLadder;
                }
                minLadderCount = int.MaxValue;
                float minDistance = float.MaxValue;
                foreach (var current in locationInfo.Location.Objects.Pairs)
                {
                    if (current.Value.Name == "Stone")
                    {
                        int count = EvalLadderTile(playerInfo, locationInfo.Location as MineShaft, current.Key);
                        ladderCounts.Add(current.Key, count);
                        sortedLadderTiles.Add(new(current.Key, count));
                        if (count < minLadderCount)
                        {
                            minLadderCount = count;
                            minDistance = Vector2.DistanceSquared(baseTile, current.Key);
                            ladderTile = current.Key;
                        }
                        else if (count == minLadderCount)
                        {
                            float distance = Vector2.DistanceSquared(baseTile, current.Key);
                            if (distance < minDistance)
                            {
                                minDistance = distance;
                                ladderTile = current.Key;
                            }
                        }
                    }
                }
                sortedLadderTiles.Sort((a, b) => a.Item2.CompareTo(b.Item2));
            }
        }

        public void UpdateStoneContents(bool force = false)
        {
            if (!force && !state.ShouldUpdate())
                return;
            state.Postfix();

            var location = InstanceCurrentLocation.Get(state.Index).Location;
            var player = InstanceCurrentPlayer.Get(state.Index).Player;
            stoneContents.Clear();
            foreach (var current in location.Objects.Pairs)
            {
                if (current.Value.Name == "Stone")
                {
                    List<Tuple<string, int>> results = EvalStoneContents(player, location as MineShaft, current.Key);
                    if (results.Count > 0)
                    {
                        stoneContents.Add(current.Key, results);
                    }
                }
            }
        }
        public List<Tuple<string, int>> EvalStoneContents(Farmer player, MineShaft mine, Vector2 tile)
        {
            string stoneId = mine.getObjectAtTile((int)tile.X, (int)tile.Y).ItemId;
            int x = (int)tile.X;
            int y = (int)tile.Y;
            int mineLevel = mine.mineLevel;
            Farmer who = player;

            // MineShaft::checkStoneForItems
            long farmerId = who?.UniqueMultiplayerID ?? 0;
            int farmerLuckLevel = who?.LuckLevel ?? 0;
            double num = who?.DailyLuck ?? 0.0;
            int farmerMiningLevel = who?.MiningLevel ?? 0;
            double chanceModifier =
                num / 2.0 + (double)farmerMiningLevel * 0.005 + (double)farmerLuckLevel * 0.001;
            Random r = Utility.CreateDaySaveRandom(x * 1000, y, mineLevel);
            r.NextDouble();
            double oreModifier = (
                (stoneId == 40.ToString() || stoneId == 42.ToString()) ? 1.2 : 0.8
            );
            int stonesLeftOnThisLevel = mine.stonesLeftOnThisLevel - 1;
            double chanceForLadderDown =
                0.02
                + 1.0 / (double)Math.Max(1, stonesLeftOnThisLevel)
                + (double)farmerLuckLevel / 100.0
                + player.DailyLuck / 5.0;

            if (mine.EnemyCount == 0)
            {
                chanceForLadderDown += 0.04;
            }
            if (who != null && who.hasBuff("dwarfStatue_1"))
            {
                chanceForLadderDown *= 1.25;
            }
            if (
                !mine.ladderHasSpawned
                && !mine.mustKillAllMonstersToAdvance()
                && (stonesLeftOnThisLevel == 0 || r.NextDouble() < chanceForLadderDown)
                && mine.shouldCreateLadderOnThisLevel()
            )
            {
                // createLadderDown(x, y);
            }
            List<Tuple<string, int>> breakStone = new List<Tuple<string, int>>(BreakStone(stoneId, x, y, who, mine, r));
            if (breakStone.Count != 0)
            {
                return breakStone;
            }

            if (stoneId == 44.ToString())
            {
                int whichGem = r.Next(59, 70);
                whichGem += whichGem % 2;
                bool reachedBottom = false;
                foreach (Farmer allFarmer in Game1.getAllFarmers())
                {
                    if (allFarmer.timesReachedMineBottom > 0)
                    {
                        reachedBottom = true;
                        break;
                    }
                }
                if (!reachedBottom)
                {
                    if (mineLevel < 40 && whichGem != 66 && whichGem != 68)
                    {
                        whichGem = r.Choose(66, 68);
                    }
                    else if (mineLevel < 80 && (whichGem == 64 || whichGem == 60))
                    {
                        whichGem = r.Choose(66, 70, 68, 62);
                    }
                }
                breakStone.Add(new("(O)" + whichGem.ToString(), 1));
                return breakStone;
            }
            int excavatorMultiplier = ((who == null || !who.professions.Contains(22)) ? 1 : 2);
            double dwarfStatueMultiplier = (
                (who != null && who.hasBuff("dwarfStatue_4")) ? 1.25 : 1.0
            );
            if (
                r.NextDouble()
                < 0.022
                    * (1.0 + chanceModifier)
                    * (double)excavatorMultiplier
                    * dwarfStatueMultiplier
            )
            {
                string id =
                    "(O)"
                    + (
                        535
                        + ((mine.getMineArea() == 40) ? 1 : ((mine.getMineArea() == 80) ? 2 : 0))
                    );
                if (mine.getMineArea() == 121)
                {
                    id = "(O)749";
                }
                if (who != null && who.professions.Contains(19) && r.NextBool())
                {
                    breakStone.Add(new(id, 1));
                }
                breakStone.Add(new(id, 1));
            }
            if (
                mineLevel > 20
                && r.NextDouble()
                    < 0.005
                        * (1.0 + chanceModifier)
                        * (double)excavatorMultiplier
                        * dwarfStatueMultiplier
            )
            {
                if (who != null && who.professions.Contains(19) && r.NextBool())
                {
                    breakStone.Add(new("(O)749", 1));
                }
                breakStone.Add(new("(O)749", 1));
            }
            if (r.NextDouble() < 0.05 * (1.0 + chanceModifier) * oreModifier)
            {
                int burrowerMultiplier = ((who == null || !who.professions.Contains(21)) ? 1 : 2);
                double addedCoalChance = (
                    (who != null && who.hasBuff("dwarfStatue_2")) ? 0.1 : 0.0
                );
                if (r.NextDouble() < 0.25 * (double)burrowerMultiplier + addedCoalChance)
                {
                    breakStone.Add(new("(O)382", 1));
                }
                string id = getOreIdForLevel(player, mine, r);
                if (id == "CalicoEgg")
                {
                    breakStone.Add(new("CalicoEgg", 1));
                }
                else
                {
                    breakStone.Add(new(id, 1));
                }
            }
            else if (r.NextBool())
            {
                breakStone.Add(new("(O)390", 1));
            }
            return breakStone;
        }
        // GameLocation::BreakStone
        public List<Tuple<string, int>> BreakStone(string stoneId, int x, int y, Farmer who, GameLocation loc, Random r)
        {
            List<Tuple<string, int>> items = new List<Tuple<string, int>>();
            int experience = 0;
            int addedOres = ((who != null && who.professions.Contains(18)) ? 1 : 0);
            if (who != null && who.hasBuff("dwarfStatue_0"))
            {
                addedOres++;
            }
            if (stoneId == 44.ToString())
            {
                stoneId = (r.Next(1, 8) * 2).ToString();
            }
            long farmerId = who?.UniqueMultiplayerID ?? 0;
            int farmerLuckLevel = who?.LuckLevel ?? 0;
            double farmerDailyLuck = who?.DailyLuck ?? 0.0;
            int farmerMiningLevel = who?.MiningLevel ?? 0;
            int amount;
            switch (stoneId)
            {
                case "95":
                    amount =
                        addedOres
                        + r.Next(1, 3)
                        + ((r.NextDouble() < (double)((float)farmerLuckLevel / 100f)) ? 1 : 0)
                        + ((r.NextDouble() < (double)((float)farmerMiningLevel / 200f)) ? 1 : 0);
                    items.Add(new Tuple<string, int>("(O)909", amount));
                    break;
                case "843":
                case "844":
                    amount =
                        addedOres
                        + r.Next(1, 3)
                        + ((r.NextDouble() < (double)((float)farmerLuckLevel / 100f)) ? 1 : 0)
                        + ((r.NextDouble() < (double)((float)farmerMiningLevel / 200f)) ? 1 : 0);
                    items.Add(new Tuple<string, int>("(O)849", amount));
                    break;
                case "25":
                    amount = r.Next(2, 5);
                    items.Add(new Tuple<string, int>("(O)719", amount));
                    if (loc is IslandLocation && r.NextDouble() < 0.1)
                    {
                        items.Add(new Tuple<string, int>("Nut", 1));
                    }
                    break;
                case "75":
                    items.Add(new Tuple<string, int>("(O)535", 1));
                    break;
                case "76":
                    items.Add(new Tuple<string, int>("(O)536", 1));
                    break;
                case "77":
                    items.Add(new Tuple<string, int>("(O)537", 1));
                    break;
                case "816":
                case "817":
                    if (r.NextDouble() < 0.1)
                    {
                        items.Add(new Tuple<string, int>("(O)823", 1));
                    }
                    else if (r.NextDouble() < 0.015)
                    {
                        items.Add(new Tuple<string, int>("(O)824", 1));
                    }
                    else if (r.NextDouble() < 0.1)
                    {
                        int index = 579 + r.Next(11);
                        items.Add(new Tuple<string, int>("(O)" + index.ToString(), 1));
                    }
                    amount =
                        addedOres
                        + r.Next(1, 3)
                        + ((r.NextDouble() < (double)((float)farmerLuckLevel / 100f)) ? 1 : 0)
                        + ((r.NextDouble() < (double)((float)farmerMiningLevel / 200f)) ? 1 : 0);
                    items.Add(new Tuple<string, int>("(O)881", amount));
                    break;
                case "818":
                    amount =
                        addedOres
                        + r.Next(1, 3)
                        + ((r.NextDouble() < (double)((float)farmerLuckLevel / 100f)) ? 1 : 0)
                        + ((r.NextDouble() < (double)((float)farmerMiningLevel / 200f)) ? 1 : 0);
                    items.Add(new Tuple<string, int>("(O)330", amount));
                    break;
                case "819":
                    items.Add(new Tuple<string, int>("(O)749", 1));
                    break;
                case "8":
                    amount = (who == null || who.stats.Get(StatKeys.Mastery(3)) == 0) ? 1 : 2;
                    items.Add(new Tuple<string, int>("(O)66", amount));
                    break;
                case "10":
                    amount = (who == null || who.stats.Get(StatKeys.Mastery(3)) == 0) ? 1 : 2;
                    items.Add(new Tuple<string, int>("(O)68", amount));
                    break;
                case "12":
                    amount = (who == null || who.stats.Get(StatKeys.Mastery(3)) == 0) ? 1 : 2;
                    items.Add(new Tuple<string, int>("(O)60", amount));
                    break;
                case "14":
                    amount = (who == null || who.stats.Get(StatKeys.Mastery(3)) == 0) ? 1 : 2;
                    items.Add(new Tuple<string, int>("(O)62", amount));
                    break;
                case "6":
                    amount = (who == null || who.stats.Get(StatKeys.Mastery(3)) == 0) ? 1 : 2;
                    items.Add(new Tuple<string, int>("(O)70", amount));
                    break;
                case "4":
                    amount = (who == null || who.stats.Get(StatKeys.Mastery(3)) == 0) ? 1 : 2;
                    items.Add(new Tuple<string, int>("(O)64", amount));
                    break;
                case "2":
                    amount = (who == null || who.stats.Get(StatKeys.Mastery(3)) == 0) ? 1 : 2;
                    items.Add(new Tuple<string, int>("(O)72", amount));
                    break;
                case "845":
                case "846":
                case "847":
                case "670":
                case "668":
                    amount =
                        addedOres
                        + r.Next(1, 3)
                        + ((r.NextDouble() < (double)((float)farmerLuckLevel / 100f)) ? 1 : 0)
                        + ((r.NextDouble() < (double)((float)farmerMiningLevel / 200f)) ? 1 : 0);
                    items.Add(new Tuple<string, int>("(O)390", amount));
                    if (r.NextDouble() < 0.08)
                    {
                        amount = 1 + addedOres;
                        items.Add(new Tuple<string, int>("(O)382", amount));
                    }
                    break;
                case "849":
                case "751":
                    amount =
                        addedOres
                        + r.Next(1, 4)
                        + ((r.NextDouble() < (double)((float)farmerLuckLevel / 100f)) ? 1 : 0)
                        + ((r.NextDouble() < (double)((float)farmerMiningLevel / 100f)) ? 1 : 0);
                    items.Add(new Tuple<string, int>("(O)378", amount));
                    break;
                case "850":
                case "290":
                    amount =
                        addedOres
                        + r.Next(1, 4)
                        + ((r.NextDouble() < (double)((float)farmerLuckLevel / 100f)) ? 1 : 0)
                        + ((r.NextDouble() < (double)((float)farmerMiningLevel / 100f)) ? 1 : 0);
                    items.Add(new Tuple<string, int>("(O)380", amount));
                    break;
                case "BasicCoalNode0":
                case "BasicCoalNode1":
                case "VolcanoCoalNode0":
                case "VolcanoCoalNode1":
                    amount =
                        addedOres
                        + r.Next(1, 4)
                        + ((r.NextDouble() < (double)((float)farmerLuckLevel / 100f)) ? 1 : 0)
                        + ((r.NextDouble() < (double)((float)farmerMiningLevel / 100f)) ? 1 : 0);
                    items.Add(new Tuple<string, int>("(O)382", amount));
                    break;
                case "VolcanoGoldNode":
                case "764":
                    amount =
                        addedOres
                        + r.Next(1, 4)
                        + ((r.NextDouble() < (double)((float)farmerLuckLevel / 100f)) ? 1 : 0)
                        + ((r.NextDouble() < (double)((float)farmerMiningLevel / 100f)) ? 1 : 0);
                    items.Add(new Tuple<string, int>("(O)384", amount));
                    break;
                case "765":
                    amount =
                        addedOres
                        + r.Next(1, 4)
                        + ((r.NextDouble() < (double)((float)farmerLuckLevel / 100f)) ? 1 : 0)
                        + ((r.NextDouble() < (double)((float)farmerMiningLevel / 100f)) ? 1 : 0);
                    items.Add(new Tuple<string, int>("(O)386", amount));
                    if (r.NextDouble() < 0.035)
                    {
                        items.Add(new Tuple<string, int>("(O)74", 1));
                    }
                    break;
                case "CalicoEggStone_0":
                case "CalicoEggStone_1":
                case "CalicoEggStone_2":
                    amount =
                        r.Next(1, 4)
                        + (r.NextBool((float)farmerLuckLevel / 100f) ? 1 : 0)
                        + (r.NextBool((float)farmerMiningLevel / 100f) ? 1 : 0);
                    items.Add(new Tuple<string, int>("CalicoEgg", amount));
                    break;
            }
            if (who != null && who.professions.Contains(19) && r.NextBool())
            {
                int numToDrop = ((who.stats.Get(StatKeys.Mastery(3)) == 0) ? 1 : 2);
                switch (stoneId)
                {
                    case "8":
                        items.Add(new Tuple<string, int>("(O)66", numToDrop));
                        break;
                    case "10":
                        items.Add(new Tuple<string, int>("(O)68", numToDrop));
                        break;
                    case "12":
                        items.Add(new Tuple<string, int>("(O)60", numToDrop));
                        break;
                    case "14":
                        items.Add(new Tuple<string, int>("(O)62", numToDrop));
                        break;
                    case "6":
                        items.Add(new Tuple<string, int>("(O)70", numToDrop));
                        break;
                    case "4":
                        items.Add(new Tuple<string, int>("(O)64", numToDrop));
                        break;
                    case "2":
                        items.Add(new Tuple<string, int>("(O)72", numToDrop));
                        break;
                }
            }
            if (stoneId == 46.ToString())
            {
                amount = r.Next(1, 4);
                items.Add(new Tuple<string, int>("(O)386", amount));
                amount = r.Next(1, 5);
                items.Add(new Tuple<string, int>("(O)384", amount));
                if (r.NextDouble() < 0.25)
                {
                    items.Add(new Tuple<string, int>("(O)74", 1));
                }
            }
            if (
                (loc.IsOutdoors || loc.treatAsOutdoors.Value)
                && experience == 0
            )
            {
                double chanceModifier =
                    farmerDailyLuck / 2.0
                    + (double)farmerMiningLevel * 0.005
                    + (double)farmerLuckLevel * 0.001;
                Random ran = Utility.CreateDaySaveRandom(x * 1000, y);
                items.Add(new Tuple<string, int>("(O)390", 1));
                if (who != null)
                {
                    who.gainExperience(3, 1);
                    double coalChance = 0.0;
                    if (who.professions.Contains(21))
                    {
                        coalChance += 0.05 * (1.0 + chanceModifier);
                    }
                    if (who.hasBuff("dwarfStatue_2"))
                    {
                        coalChance += 0.025;
                    }
                    if (ran.NextDouble() < coalChance)
                    {
                        items.Add(new Tuple<string, int>("(O)382", 1));
                    }
                }
                if (ran.NextDouble() < 0.05 * (1.0 + chanceModifier))
                {
                    items.Add(new Tuple<string, int>("(O)382", 1));
                }
            }
            if (
                who != null
                && loc.HasUnlockedAreaSecretNotes(who)
                && r.NextDouble() < 0.0075
            )
            {
                items.Add(new Tuple<string, int>("SecretNote", 1));
            }
            return items;
        }
        // Mines::getOreIdForLevel
        public string getOreIdForLevel(Farmer player, MineShaft mines, Random r)
        {
            if (mines.getMineArea() == 77377)
            {
                return "(O)380";
            }
            if (mines.mineLevel < 40)
            {
                if (mines.mineLevel >= 20 && r.NextDouble() < 0.1)
                {
                    return "(O)380";
                }
                return "(O)378";
            }
            if (mines.mineLevel < 80)
            {
                if (mines.mineLevel >= 60 && r.NextDouble() < 0.1)
                {
                    return "(O)384";
                }
                if (!(r.NextDouble() < 0.75))
                {
                    return "(O)378";
                }
                return "(O)380";
            }
            if (mines.mineLevel < 120)
            {
                if (!(r.NextDouble() < 0.75))
                {
                    if (!(r.NextDouble() < 0.75))
                    {
                        return "(O)378";
                    }
                    return "(O)380";
                }
                return "(O)384";
            }
            if (
                Utility.GetDayOfPassiveFestival("DesertFestival") > 0
                && r.NextDouble()
                    < 0.13
                        + (double)(
                            (float)((int)player.team.calicoEggSkullCavernRating.Value * 5)
                            / 1000f
                        )
            )
            {
                return "CalicoEgg";
            }
            if (r.NextDouble() < 0.01 + (double)((float)(mines.mineLevel - 120) / 2000f))
            {
                return "(O)386";
            }
            if (!(r.NextDouble() < 0.75))
            {
                if (!(r.NextDouble() < 0.75))
                {
                    return "(O)378";
                }
                return "(O)380";
            }
            return "(O)384";
        }
        //

        public Tuple<bool, double> EstimatePathCost(Vector2 end)
        {
            pathFinder.Reset();
            pathFinder.Update(state.Index, (int)end.X, (int)end.Y, true);
            if (pathFinder.hasPath)
            {
                return new Tuple<bool, double>(true, pathFinder.cost);
            }
            return new Tuple<bool, double>(false, -1);
        }

        public Vector2 NearestNeighbor(Vector2 end)
        {
            Vector2 nearest = end;
            double minCost = double.MaxValue;
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0)
                    {
                        continue;
                    }
                    Vector2 loc = new Vector2(end.X + i, end.Y + j);
                    var path = EstimatePathCost(loc);
                    if (path.Item1 && path.Item2 < minCost)
                    {
                        minCost = path.Item2;
                        nearest = loc;
                    }
                }
            }
            return nearest;
        }

        public Vector2 NearestRock(int minBound, int maxBound)
        {
            var ladders = GetSortedLadderTiles();
            Vector2 nearest = Vector2.Zero;
            double minCost = double.MaxValue;
            foreach (var ladder in ladders)
            {
                if (ladder.Item2 < minBound || ladder.Item2 > maxBound)
                {
                    continue;
                }
                var path = EstimatePathCost(ladder.Item1);
                if (path.Item1 && path.Item2 < minCost)
                {
                    minCost = path.Item2;
                    nearest = ladder.Item1;
                }
            }
            return nearest;
        }

        public Vector2 LinearClosestRock()
        {
            var playerTile = InstanceCurrentPlayer.Get(state.Index).CurrentTile;
            var ladders = GetLadderCounts();
            Vector2 nearest = Vector2.Zero;
            double minCost = double.MaxValue;
            foreach (var ladder in ladders)
            {
                var dist = Vector2.DistanceSquared(ladder.Key, playerTile);
                if (dist < minCost)
                {
                    minCost = dist;
                    nearest = ladder.Key;
                }
            }
            return nearest;
        }

        public Vector2 ClosestRock()
        {
            var playerTile = InstanceCurrentPlayer.Get(state.Index).CurrentTile;
            var locationInfo = InstanceCurrentLocation.Get(state.Index);
            var queue = new Queue<Vector2>();
            var visited = new HashSet<Vector2>();
            queue.Enqueue(playerTile);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (visited.Contains(current))
                {
                    continue;
                }
                visited.Add(current);
                if (locationInfo.Location.Objects.TryGetValue(current, out var obj) && obj.Name == "Stone")
                {
                    return current;
                }
                foreach (var neighbor in Utility.getAdjacentTileLocations(current))
                {
                    if (IsTilePassable(neighbor) && !visited.Contains(neighbor))
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }
            Controller.Console.Trace($"No rocks found in BFS search, returning (0, 0). scanned {visited.Count} tiles.");
            return Vector2.Zero;
        }
        public Vector2 ClosestRockBounded(int aboveBounds)
        {
            var ladders = GetSortedLadderTiles();
            var playerTile = InstanceCurrentPlayer.Get(state.Index).CurrentTile;
            var locationInfo = InstanceCurrentLocation.Get(state.Index);
            var queue = new Queue<Vector2>();
            var visited = new HashSet<Vector2>();
            queue.Enqueue(playerTile);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (visited.Contains(current))
                {
                    continue;
                }
                visited.Add(current);
                if (locationInfo.Location.Objects.TryGetValue(current, out var obj) && obj.Name == "Stone" && ladders.Any(l => l.Item1 == current && l.Item2 >= aboveBounds))
                {
                    return current;
                }
                foreach (var neighbor in Utility.getAdjacentTileLocations(current))
                {
                    if (IsTilePassable(neighbor) && !visited.Contains(neighbor))
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }
            Controller.Console.Trace($"No rocks found in BFS search, returning (0, 0). scanned {visited.Count} tiles.");
            return Vector2.Zero;
        }

        public Dictionary<Vector2, List<Tuple<string, int>>> GetStonesWithinRadius(int radius)
        {
            var nearbyStones = new Dictionary<Vector2, List<Tuple<string, int>>>();
            var playerTile = InstanceCurrentPlayer.Get(state.Index).CurrentTile;
            var stoneContents = GetStoneContents();
            foreach (var stone in stoneContents)
            {
                if (Vector2.DistanceSquared(stone.Key, playerTile) <= radius * radius)
                {
                    nearbyStones.Add(stone.Key, stone.Value);
                }
            }
            return nearbyStones;
        }

        public bool IsTilePassable(Vector2 tile)
        {
            var locationInfo = InstanceCurrentLocation.Get(state.Index);
            return locationInfo.Location.isTilePassable(new xTile.Dimensions.Location((int)tile.X, (int)tile.Y), Game1.viewport);
        }

        public List<Debris> GetDebrisWithinRadius(int radius)
        {
            var nearbyDebris = new List<Debris>();
            var playerTile = InstanceCurrentPlayer.Get(state.Index).CurrentTile;
            var locationInfo = InstanceCurrentLocation.Get(state.Index);
            foreach (var debris in locationInfo.Location.debris)
            {
                if (debris.debrisType.Value != Debris.DebrisType.OBJECT && debris.debrisType.Value != Debris.DebrisType.RESOURCE && debris.debrisType.Value != Debris.DebrisType.ARCHAEOLOGY)
                {
                    continue;
                }
                Vector2 debrisTile = ApproximateDebrisTile(debris);
                if (!IsTilePassable(debrisTile))
                {
                    continue;
                }
                if (Vector2.DistanceSquared(debrisTile, playerTile) <= radius * radius)
                {
                    nearbyDebris.Add(debris);
                }
            }
            return nearbyDebris;
        }
        public Vector2 ApproximateDebrisCenter(Debris debris)
        {
            Vector2 vector = default(Vector2);
            foreach (Chunk chunk in debris.Chunks)
            {
                vector += chunk.position.Value;
            }

            return vector / debris.Chunks.Count;
        }
        public Vector2 ApproximateDebrisTile(Debris debris)
        {
            Vector2 vector = default(Vector2);
            foreach (Chunk chunk in debris.Chunks)
            {
                vector += chunk.position.Value;
            }

            vector = vector / debris.Chunks.Count;
            return new(((int)vector.X + 32) / 64, ((int)vector.Y + 32) / 64);
        }
        public bool PlayerInRange(Vector2 position, Farmer farmer)
        {
            int appliedMagneticRadius = farmer.GetAppliedMagneticRadius();
            Point standingPixel = farmer.StandingPixel;
            if (Math.Abs(position.X + 32f - (float)standingPixel.X) <= (float)appliedMagneticRadius)
            {
                return Math.Abs(position.Y + 32f - (float)standingPixel.Y) <= (float)appliedMagneticRadius;
            }

            return false;
        }
        public bool WillMoveTowardPlayer(int index, Debris debris)
        {
            var player = InstanceCurrentPlayer.Get(index).Player;
            Vector2 vec = ApproximateDebrisCenter(debris);
            return PlayerInRange(vec, player);
        }
    }
}