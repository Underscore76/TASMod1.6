using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Constants;
using StardewValley.Extensions;
using StardewValley.Locations;
using TASMod.Helpers;

namespace TASMod.Overlays
{
    public class MinesRocks : IOverlay
    {
        public override string Name => "MinesRocks";
        public override string Description => "Show rocks in mines that have drops";

        public Color RectColor = new Color(0, 0, 0, 180);
        public Color TextColor = Color.White;

        private string currentLocationName = "";
        private int currentLocationNumObjects = -1;
        public Dictionary<Vector2, List<string>> objectsThatHaveDrops;

        public MinesRocks()
        {
            Active = true;
            Reset();
        }

        public bool ShouldUpdate()
        {
            return CurrentLocation.IsMines
                && (
                    Game1.currentLocation.Name != currentLocationName
                    || Game1.currentLocation.Objects.Count() != currentLocationNumObjects
                );
        }

        public override void Reset()
        {
            currentLocationName = "";
            currentLocationNumObjects = -1;
            objectsThatHaveDrops = new Dictionary<Vector2, List<string>>();
        }

        public override void ActiveUpdate()
        {
            if (!ShouldUpdate())
            {
                return;
            }
            currentLocationName = Game1.currentLocation.Name;
            currentLocationNumObjects = Game1.currentLocation.Objects.Count();
            objectsThatHaveDrops.Clear();
            foreach (
                KeyValuePair<Vector2, StardewValley.Object> current in Game1
                    .currentLocation
                    .Objects
                    .Pairs
            )
            {
                if (current.Value.Name == "Stone")
                {
                    List<string> results = EvalTile(
                        Game1.currentLocation as MineShaft,
                        current.Key
                    );
                    results = results.Where(o => !o.Contains("Stone")).ToList();
                    if (results.Count > 0)
                    {
                        objectsThatHaveDrops.Add(current.Key, results);
                    }
                }
            }
        }

        public override void ActiveDraw(SpriteBatch b)
        {
            if (!CurrentLocation.IsMines)
                return;

            foreach (KeyValuePair<Vector2, List<string>> current in objectsThatHaveDrops)
            {
                DrawTextAtTile(b, current.Value, current.Key, TextColor, RectColor);
            }
        }

        public List<string> EvalTile(MineShaft mine, Vector2 tile)
        {
            string stoneId = mine.getObjectAtTile((int)tile.X, (int)tile.Y).ItemId;
            int x = (int)tile.X;
            int y = (int)tile.Y;
            int mineLevel = mine.mineLevel;
            Farmer who = Game1.player;

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
                + Game1.player.DailyLuck / 5.0;

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
            List<string> breakStone = new List<string>(BreakStone(stoneId, x, y, who, r));
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
                breakStone.Add(Game1.objectData[whichGem.ToString()].Name);
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
                    ""
                    + (
                        535
                        + ((mine.getMineArea() == 40) ? 1 : ((mine.getMineArea() == 80) ? 2 : 0))
                    );
                if (mine.getMineArea() == 121)
                {
                    id = "749";
                }
                if (who != null && who.professions.Contains(19) && r.NextBool())
                {
                    breakStone.Add(Game1.objectData[id].Name);
                }
                breakStone.Add(Game1.objectData[id].Name);
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
                    breakStone.Add(Game1.objectData["749"].Name);
                }
                breakStone.Add(Game1.objectData["749"].Name);
            }
            if (r.NextDouble() < 0.05 * (1.0 + chanceModifier) * oreModifier)
            {
                int burrowerMultiplier = ((who == null || !who.professions.Contains(21)) ? 1 : 2);
                double addedCoalChance = (
                    (who != null && who.hasBuff("dwarfStatue_2")) ? 0.1 : 0.0
                );
                if (r.NextDouble() < 0.25 * (double)burrowerMultiplier + addedCoalChance)
                {
                    breakStone.Add(Game1.objectData["382"].Name);
                }
                string id = getOreIdForLevel(mine, r);
                if (id == "CalicoEgg")
                {
                    breakStone.Add("CalicoEgg");
                }
                else
                {
                    breakStone.Add(Game1.objectData[id].Name);
                }
            }
            else if (r.NextBool())
            {
                breakStone.Add(Game1.objectData["390"].Name);
            }
            return breakStone;
        }

        // GameLocation::BreakStone
        public List<string> BreakStone(string stoneId, int x, int y, Farmer who, Random r)
        {
            List<string> items = new List<string>();
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
                    items.Add(Game1.objectData["909"].Name + (amount > 1 ? $"x{amount}" : ""));
                    break;
                case "843":
                case "844":
                    amount =
                        addedOres
                        + r.Next(1, 3)
                        + ((r.NextDouble() < (double)((float)farmerLuckLevel / 100f)) ? 1 : 0)
                        + ((r.NextDouble() < (double)((float)farmerMiningLevel / 200f)) ? 1 : 0);
                    items.Add(Game1.objectData["849"].Name + (amount > 1 ? $"x{amount}" : ""));
                    break;
                case "25":
                    amount = r.Next(2, 5);
                    items.Add(Game1.objectData["719"].Name + (amount > 1 ? $"x{amount}" : ""));
                    if (Game1.currentLocation is IslandLocation && r.NextDouble() < 0.1)
                    {
                        items.Add("Nut");
                    }
                    break;
                case "75":
                    items.Add(Game1.objectData["535"].Name);
                    break;
                case "76":
                    items.Add(Game1.objectData["536"].Name);
                    break;
                case "77":
                    items.Add(Game1.objectData["537"].Name);
                    break;
                case "816":
                case "817":
                    if (r.NextDouble() < 0.1)
                    {
                        items.Add(Game1.objectData["823"].Name);
                    }
                    else if (r.NextDouble() < 0.015)
                    {
                        items.Add(Game1.objectData["824"].Name);
                    }
                    else if (r.NextDouble() < 0.1)
                    {
                        int index = 579 + r.Next(11);
                        items.Add(Game1.objectData[index.ToString()].Name);
                    }
                    amount =
                        addedOres
                        + r.Next(1, 3)
                        + ((r.NextDouble() < (double)((float)farmerLuckLevel / 100f)) ? 1 : 0)
                        + ((r.NextDouble() < (double)((float)farmerMiningLevel / 200f)) ? 1 : 0);
                    items.Add(Game1.objectData["881"].Name + (amount > 1 ? $"x{amount}" : ""));
                    break;
                case "818":
                    amount =
                        addedOres
                        + r.Next(1, 3)
                        + ((r.NextDouble() < (double)((float)farmerLuckLevel / 100f)) ? 1 : 0)
                        + ((r.NextDouble() < (double)((float)farmerMiningLevel / 200f)) ? 1 : 0);
                    items.Add(Game1.objectData["330"].Name + (amount > 1 ? $"x{amount}" : ""));
                    break;
                case "819":
                    items.Add(Game1.objectData["749"].Name);
                    break;
                case "8":
                    amount = (who == null || who.stats.Get(StatKeys.Mastery(3)) == 0) ? 1 : 2;
                    items.Add(Game1.objectData["66"].Name + (amount > 1 ? $"x{amount}" : ""));
                    break;
                case "10":
                    amount = (who == null || who.stats.Get(StatKeys.Mastery(3)) == 0) ? 1 : 2;
                    items.Add(Game1.objectData["68"].Name + (amount > 1 ? $"x{amount}" : ""));
                    break;
                case "12":
                    amount = (who == null || who.stats.Get(StatKeys.Mastery(3)) == 0) ? 1 : 2;
                    items.Add(Game1.objectData["60"].Name + (amount > 1 ? $"x{amount}" : ""));
                    break;
                case "14":
                    amount = (who == null || who.stats.Get(StatKeys.Mastery(3)) == 0) ? 1 : 2;
                    items.Add(Game1.objectData["62"].Name + (amount > 1 ? $"x{amount}" : ""));
                    break;
                case "6":
                    amount = (who == null || who.stats.Get(StatKeys.Mastery(3)) == 0) ? 1 : 2;
                    items.Add(Game1.objectData["70"].Name + (amount > 1 ? $"x{amount}" : ""));
                    break;
                case "4":
                    amount = (who == null || who.stats.Get(StatKeys.Mastery(3)) == 0) ? 1 : 2;
                    items.Add(Game1.objectData["64"].Name + (amount > 1 ? $"x{amount}" : ""));
                    break;
                case "2":
                    amount = (who == null || who.stats.Get(StatKeys.Mastery(3)) == 0) ? 1 : 2;
                    items.Add(Game1.objectData["72"].Name + (amount > 1 ? $"x{amount}" : ""));
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
                    items.Add(Game1.objectData["390"].Name + (amount > 1 ? $"x{amount}" : ""));
                    if (r.NextDouble() < 0.08)
                    {
                        amount = 1 + addedOres;
                        items.Add(Game1.objectData["382"].Name + (amount > 1 ? $"x{amount}" : ""));
                    }
                    break;
                case "849":
                case "751":
                    amount =
                        addedOres
                        + r.Next(1, 4)
                        + ((r.NextDouble() < (double)((float)farmerLuckLevel / 100f)) ? 1 : 0)
                        + ((r.NextDouble() < (double)((float)farmerMiningLevel / 100f)) ? 1 : 0);
                    items.Add(Game1.objectData["378"].Name + (amount > 1 ? $"x{amount}" : ""));
                    break;
                case "850":
                case "290":
                    amount =
                        addedOres
                        + r.Next(1, 4)
                        + ((r.NextDouble() < (double)((float)farmerLuckLevel / 100f)) ? 1 : 0)
                        + ((r.NextDouble() < (double)((float)farmerMiningLevel / 100f)) ? 1 : 0);
                    items.Add(Game1.objectData["380"].Name + (amount > 1 ? $"x{amount}" : ""));
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
                    items.Add(Game1.objectData["382"].Name + (amount > 1 ? $"x{amount}" : ""));
                    break;
                case "VolcanoGoldNode":
                case "764":
                    amount =
                        addedOres
                        + r.Next(1, 4)
                        + ((r.NextDouble() < (double)((float)farmerLuckLevel / 100f)) ? 1 : 0)
                        + ((r.NextDouble() < (double)((float)farmerMiningLevel / 100f)) ? 1 : 0);
                    items.Add(Game1.objectData["384"].Name + (amount > 1 ? $"x{amount}" : ""));
                    break;
                case "765":
                    amount =
                        addedOres
                        + r.Next(1, 4)
                        + ((r.NextDouble() < (double)((float)farmerLuckLevel / 100f)) ? 1 : 0)
                        + ((r.NextDouble() < (double)((float)farmerMiningLevel / 100f)) ? 1 : 0);
                    items.Add(Game1.objectData["386"].Name + (amount > 1 ? $"x{amount}" : ""));
                    if (r.NextDouble() < 0.035)
                    {
                        items.Add(Game1.objectData["74"].Name);
                    }
                    break;
                case "CalicoEggStone_0":
                case "CalicoEggStone_1":
                case "CalicoEggStone_2":
                    amount =
                        r.Next(1, 4)
                        + (r.NextBool((float)farmerLuckLevel / 100f) ? 1 : 0)
                        + (r.NextBool((float)farmerMiningLevel / 100f) ? 1 : 0);
                    items.Add("CalicoEgg" + (amount > 1 ? $"x{amount}" : ""));
                    break;
            }
            if (who != null && who.professions.Contains(19) && r.NextBool())
            {
                int numToDrop = ((who.stats.Get(StatKeys.Mastery(3)) == 0) ? 1 : 2);
                switch (stoneId)
                {
                    case "8":
                        items.Add(
                            Game1.objectData["66"].Name + (numToDrop > 1 ? $"x{numToDrop}" : "")
                        );
                        break;
                    case "10":
                        items.Add(
                            Game1.objectData["68"].Name + (numToDrop > 1 ? $"x{numToDrop}" : "")
                        );
                        break;
                    case "12":
                        items.Add(
                            Game1.objectData["60"].Name + (numToDrop > 1 ? $"x{numToDrop}" : "")
                        );
                        break;
                    case "14":
                        items.Add(
                            Game1.objectData["62"].Name + (numToDrop > 1 ? $"x{numToDrop}" : "")
                        );
                        break;
                    case "6":
                        items.Add(
                            Game1.objectData["70"].Name + (numToDrop > 1 ? $"x{numToDrop}" : "")
                        );
                        break;
                    case "4":
                        items.Add(
                            Game1.objectData["64"].Name + (numToDrop > 1 ? $"x{numToDrop}" : "")
                        );
                        break;
                    case "2":
                        items.Add(
                            Game1.objectData["72"].Name + (numToDrop > 1 ? $"x{numToDrop}" : "")
                        );
                        break;
                }
            }
            if (stoneId == 46.ToString())
            {
                amount = r.Next(1, 4);
                items.Add(Game1.objectData["386"].Name + (amount > 1 ? $"x{amount}" : ""));
                amount = r.Next(1, 5);
                items.Add(Game1.objectData["384"].Name + (amount > 1 ? $"x{amount}" : ""));
                if (r.NextDouble() < 0.25)
                {
                    items.Add(Game1.objectData["74"].Name);
                }
            }
            if (
                (Game1.currentLocation.IsOutdoors || Game1.currentLocation.treatAsOutdoors.Value)
                && experience == 0
            )
            {
                double chanceModifier =
                    farmerDailyLuck / 2.0
                    + (double)farmerMiningLevel * 0.005
                    + (double)farmerLuckLevel * 0.001;
                Random ran = Utility.CreateDaySaveRandom(x * 1000, y);
                items.Add(Game1.objectData["390"].Name);
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
                        items.Add(Game1.objectData["382"].Name);
                    }
                }
                if (ran.NextDouble() < 0.05 * (1.0 + chanceModifier))
                {
                    items.Add(Game1.objectData["382"].Name);
                }
            }
            if (
                who != null
                && Game1.currentLocation.HasUnlockedAreaSecretNotes(who)
                && r.NextDouble() < 0.0075
            )
            {
                items.Add("SecretNote");
            }
            return items;
        }

        // Mines::getOreIdForLevel
        public string getOreIdForLevel(MineShaft mines, Random r)
        {
            if (mines.getMineArea() == 77377)
            {
                return "380";
            }
            if (mines.mineLevel < 40)
            {
                if (mines.mineLevel >= 20 && r.NextDouble() < 0.1)
                {
                    return "380";
                }
                return "378";
            }
            if (mines.mineLevel < 80)
            {
                if (mines.mineLevel >= 60 && r.NextDouble() < 0.1)
                {
                    return "384";
                }
                if (!(r.NextDouble() < 0.75))
                {
                    return "378";
                }
                return "380";
            }
            if (mines.mineLevel < 120)
            {
                if (!(r.NextDouble() < 0.75))
                {
                    if (!(r.NextDouble() < 0.75))
                    {
                        return "378";
                    }
                    return "380";
                }
                return "384";
            }
            if (
                Utility.GetDayOfPassiveFestival("DesertFestival") > 0
                && r.NextDouble()
                    < 0.13
                        + (double)(
                            (float)((int)Game1.player.team.calicoEggSkullCavernRating.Value * 5)
                            / 1000f
                        )
            )
            {
                return "CalicoEgg";
            }
            if (r.NextDouble() < 0.01 + (double)((float)(mines.mineLevel - 120) / 2000f))
            {
                return "386";
            }
            if (!(r.NextDouble() < 0.75))
            {
                if (!(r.NextDouble() < 0.75))
                {
                    return "378";
                }
                return "380";
            }
            return "384";
        }
    }
}
