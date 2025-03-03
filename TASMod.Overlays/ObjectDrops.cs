using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Constants;
using StardewValley.Extensions;
using StardewValley.Locations;
using TASMod.Helpers;
using Object = StardewValley.Object;
namespace TASMod.Overlays
{
    public class ObjectDrops : IOverlay
    {

        public class ObjectDrop
        {
            string item;
            int quantity;
            public ObjectDrop(string item, int quantity = 1)
            {
                this.item = item;
                this.quantity = quantity;
            }

            public override string ToString()
            {
                string text;
                if (item.StartsWith("(O)"))
                {
                    text = DropInfo.ObjectName(item.Substring(3));
                }
                else
                {
                    text = item;
                }
                if (quantity > 1)
                {
                    text += " x" + quantity;
                }
                return text;
            }
        }
        public override string Name => "ObjectDrops";
        public override string Description => "displays SDV Object drops when broken";

        public Dictionary<Vector2, List<ObjectDrop>> Drops = new();
        public int LastObjectCount;

        public override void ActiveUpdate()
        {
            if (!(Game1.currentLocation is Farm)) return;

            if (LastObjectCount == Game1.currentLocation.Objects.Length) return;

            LastObjectCount = Game1.currentLocation.Objects.Length;
            Drops.Clear();
            foreach (var obj in Game1.currentLocation.Objects.Values)
            {
                if (obj.ItemId == "343" || obj.ItemId == "450")
                {
                    List<ObjectDrop> drops = OnStoneDestroyed(obj);
                    if (drops.Count > 0)
                    {
                        Drops.Add(obj.TileLocation, drops);
                    }
                }
            }
        }

        public override void ActiveDraw(SpriteBatch spriteBatch)
        {
            if (!(Game1.currentLocation is Farm)) return;

            foreach (var kvp in Drops)
            {
                Vector2 tile = kvp.Key;
                List<ObjectDrop> drops = kvp.Value;
                string text = string.Join(", ", drops);
                float scale = FitTextInTile(text);
                DrawTextAtTile(spriteBatch, text, tile, Color.White, Color.Black, scale);
            }
        }

        public List<ObjectDrop> OnStoneDestroyed(Object stone)
        {
            List<ObjectDrop> items = new();
            Farmer who = Game1.player;
            string stoneId = stone.ItemId;
            int x = (int)stone.TileLocation.X;
            int y = (int)stone.TileLocation.Y;
            if (stoneId == "343" || stoneId == "450")
            {
                Random r = Utility.CreateDaySaveRandom(x * 2000, y);
                double geodeChanceMultiplier = ((who != null && who.hasBuff("dwarfStatue_4")) ? 1.25 : 1.0);
                if (r.NextDouble() < 0.035 * geodeChanceMultiplier && Game1.stats.DaysPlayed > 1)
                {
                    string text = "(O)" + (535 + ((Game1.stats.DaysPlayed > 60 && r.NextDouble() < 0.2) ? 1 : ((Game1.stats.DaysPlayed > 120 && r.NextDouble() < 0.2) ? 2 : 0)));
                    items.Add(new(text));
                }
                int burrowerMultiplier = ((who == null || !who.professions.Contains(21)) ? 1 : 2);
                double addedCoalChance = ((who != null && who.hasBuff("dwarfStatue_2")) ? 0.03 : 0.0);
                if (r.NextDouble() < 0.035 * (double)burrowerMultiplier + addedCoalChance && Game1.stats.DaysPlayed > 1)
                {
                    items.Add(new("coal"));
                }
                if (r.NextDouble() < 0.01 && Game1.stats.DaysPlayed > 1)
                {
                    items.Add(new("stone"));
                }
            }
            items.AddRange(breakStone(stoneId, x, y, who, Utility.CreateDaySaveRandom(x * 4000, y)));
            return items;
        }

        public List<ObjectDrop> breakStone(string stoneId, int x, int y, Farmer who, Random r)
        {
            List<ObjectDrop> items = new();
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
            if (stoneId != null)
            {
                switch (stoneId.Length)
                {
                    case 2:
                        switch (stoneId[1])
                        {
                            case '5':
                                switch (stoneId)
                                {
                                    case "95":
                                        items.Add(new("(O)909",
                                            addedOres + r.Next(1, 3)
                                            + ((r.NextDouble() < (double)((float)farmerLuckLevel / 100f)) ? 1 : 0)
                                            + ((r.NextDouble() < (double)((float)farmerMiningLevel / 200f)) ? 1 : 0)
                                        ));
                                        break;
                                    case "25":
                                        items.Add(new("(O)719", r.Next(2, 5)));
                                        if (Game1.currentLocation is IslandLocation && r.NextDouble() < 0.1)
                                        {
                                            items.Add(new("Nut"));
                                        }
                                        break;
                                    case "75":
                                        items.Add(new("(O)535"));
                                        break;
                                }
                                break;
                            case '6':
                                if (stoneId == "76")
                                {
                                    items.Add(new("(O)536"));
                                }
                                break;
                            case '7':
                                if (stoneId == "77")
                                {
                                    items.Add(new("(O)537"));
                                }
                                break;
                            case '0':
                                if (stoneId == "10")
                                {
                                    items.Add(new("(O)68", (who == null || who.stats.Get(StatKeys.Mastery(3)) == 0) ? 1 : 2));
                                }
                                break;
                            case '2':
                                if (stoneId == "12")
                                {
                                    items.Add(new("(O)60", (who == null || who.stats.Get(StatKeys.Mastery(3)) == 0) ? 1 : 2));
                                }
                                break;
                            case '4':
                                if (stoneId == "14")
                                {
                                    items.Add(new("(O)62", (who == null || who.stats.Get(StatKeys.Mastery(3)) == 0) ? 1 : 2));
                                }
                                break;
                        }
                        break;
                    case 3:
                        switch (stoneId[2])
                        {
                            case '3':
                                if (!(stoneId == "843"))
                                {
                                    break;
                                }
                                goto IL_0492;
                            case '4':
                                if (!(stoneId == "844"))
                                {
                                    goto IL_0294;
                                }
                                goto IL_0492;
                            case '6':
                                if (stoneId == "816")
                                {
                                    goto IL_0578;
                                }
                                if (!(stoneId == "846"))
                                {
                                    break;
                                }
                                goto IL_0805;
                            case '7':
                                if (stoneId == "817")
                                {
                                    goto IL_0578;
                                }
                                if (!(stoneId == "847"))
                                {
                                    break;
                                }
                                goto IL_0805;
                            case '8':
                                if (!(stoneId == "818"))
                                {
                                    if (!(stoneId == "668"))
                                    {
                                        break;
                                    }
                                    goto IL_0805;
                                }
                                items.Add(new("(O)330", addedOres + r.Next(1, 3) + ((r.NextDouble() < (double)((float)farmerLuckLevel / 100f)) ? 1 : 0) + ((r.NextDouble() < (double)((float)farmerMiningLevel / 100f)) ? 1 : 0)));
                                break;
                            case '9':
                                if (!(stoneId == "819"))
                                {
                                    if (!(stoneId == "849"))
                                    {
                                        break;
                                    }
                                    goto IL_0874;
                                }
                                items.Add(new("(O)749"));
                                break;
                            case '5':
                                if (!(stoneId == "845"))
                                {
                                    if (stoneId == "765")
                                    {
                                        items.Add(new("(O)386", addedOres + r.Next(1, 4) + ((r.NextDouble() < (double)((float)farmerLuckLevel / 100f)) ? 1 : 0) + ((r.NextDouble() < (double)((float)farmerMiningLevel / 100f)) ? 1 : 0)));
                                        if (r.NextDouble() < 0.035)
                                        {
                                            items.Add(new("(O)74"));
                                        }
                                        experience = 50;
                                    }
                                    break;
                                }
                                goto IL_0805;
                            case '0':
                                switch (stoneId)
                                {
                                    case "670":
                                        break;
                                    case "850":
                                    case "290":
                                        items.Add(new("(O)380", addedOres + r.Next(1, 4) + ((r.NextDouble() < (double)((float)farmerLuckLevel / 100f)) ? 1 : 0) + ((r.NextDouble() < (double)((float)farmerMiningLevel / 100f)) ? 1 : 0)));
                                        goto end_IL_00b3;
                                    default:
                                        goto end_IL_00b3;
                                }
                                goto IL_0805;
                            case '1':
                                {
                                    if (!(stoneId == "751"))
                                    {
                                        break;
                                    }
                                    goto IL_0874;
                                }
                            IL_0805:
                                items.Add(new("(O)390", addedOres + r.Next(1, 3) + ((r.NextDouble() < (double)((float)farmerLuckLevel / 100f)) ? 1 : 0) + ((r.NextDouble() < (double)((float)farmerMiningLevel / 100f)) ? 1 : 0)));
                                if (r.NextDouble() < 0.08)
                                {
                                    items.Add(new("(O)382", 1 + addedOres));
                                }
                                break;
                            IL_0492:
                                items.Add(new("(O)848", addedOres + r.Next(1, 3) + ((r.NextDouble() < (double)((float)farmerLuckLevel / 100f)) ? 1 : 0) + ((r.NextDouble() < (double)((float)farmerMiningLevel / 200f)) ? 1 : 0)));
                                break;
                            IL_0874:
                                items.Add(new("(O)378", addedOres + r.Next(1, 4) + ((r.NextDouble() < (double)((float)farmerLuckLevel / 100f)) ? 1 : 0) + ((r.NextDouble() < (double)((float)farmerMiningLevel / 100f)) ? 1 : 0)));
                                break;
                        }
                        break;
                    case 1:
                        switch (stoneId[0])
                        {
                            case '8':
                                items.Add(new("(O)66", (who == null || who.stats.Get(StatKeys.Mastery(3)) == 0) ? 1 : 2));
                                break;
                            case '6':
                                items.Add(new("(O)70", (who == null || who.stats.Get(StatKeys.Mastery(3)) == 0) ? 1 : 2));
                                break;
                            case '4':
                                items.Add(new("(O)64", (who == null || who.stats.Get(StatKeys.Mastery(3)) == 0) ? 1 : 2));
                                break;
                            case '2':
                                items.Add(new("(O)72", (who == null || who.stats.Get(StatKeys.Mastery(3)) == 0) ? 1 : 2));
                                break;
                        }
                        break;
                    case 14:
                        {
                            char c = stoneId[13];
                            if (c != '0')
                            {
                                if (c != '1' || !(stoneId == "BasicCoalNode1"))
                                {
                                    break;
                                }
                            }
                            else if (!(stoneId == "BasicCoalNode0"))
                            {
                                break;
                            }
                            goto IL_0981;
                        }
                    case 16:
                        switch (stoneId[15])
                        {
                            case '0':
                                if (stoneId == "VolcanoCoalNode0")
                                {
                                    goto IL_0981;
                                }
                                if (!(stoneId == "CalicoEggStone_0"))
                                {
                                    break;
                                }
                                goto IL_0b37;
                            case '1':
                                if (stoneId == "VolcanoCoalNode1")
                                {
                                    goto IL_0981;
                                }
                                if (!(stoneId == "CalicoEggStone_1"))
                                {
                                    break;
                                }
                                goto IL_0b37;
                            case '2':
                                {
                                    if (!(stoneId == "CalicoEggStone_2"))
                                    {
                                        break;
                                    }
                                    goto IL_0b37;
                                }
                            IL_0b37:
                                items.Add(new("CalicoEgg", r.Next(1, 4) + (r.NextBool((float)farmerLuckLevel / 100f) ? 1 : 0) + (r.NextBool((float)farmerMiningLevel / 100f) ? 1 : 0)));
                                break;
                        }
                        break;
                    case 15:
                        {
                            if (!(stoneId == "VolcanoGoldNode"))
                            {
                                break;
                            }
                            goto IL_0a08;
                        }
                    IL_0294:
                        if (!(stoneId == "764"))
                        {
                            break;
                        }
                        goto IL_0a08;
                    IL_0a08:
                        items.Add(new("(O)384", addedOres + r.Next(1, 4) + ((r.NextDouble() < (double)((float)farmerLuckLevel / 100f)) ? 1 : 0) + ((r.NextDouble() < (double)((float)farmerMiningLevel / 100f)) ? 1 : 0)));
                        break;
                    IL_0981:
                        items.Add(new("(O)382", addedOres + r.Next(1, 4) + ((r.NextDouble() < (double)((float)farmerLuckLevel / 100f)) ? 1 : 0) + ((r.NextDouble() < (double)((float)farmerMiningLevel / 100f)) ? 1 : 0)));
                        break;
                    IL_0578:
                        if (r.NextDouble() < 0.1)
                        {
                            items.Add(new("(O)823"));
                        }
                        else if (r.NextDouble() < 0.015)
                        {
                            items.Add(new("(O)824"));
                        }
                        else if (r.NextDouble() < 0.1)
                        {
                            items.Add(new("(O)" + (579 + r.Next(11))));
                        }
                        items.Add(new("(O)881", addedOres + r.Next(1, 3) + ((r.NextDouble() < (double)((float)farmerLuckLevel / 100f)) ? 1 : 0) + ((r.NextDouble() < (double)((float)farmerMiningLevel / 100f)) ? 1 : 0)));
                        break;
                    end_IL_00b3:
                        break;
                }
            }
            if (who != null && who.professions.Contains(19) && r.NextBool())
            {
                int numToDrop = ((who.stats.Get(StatKeys.Mastery(3)) == 0) ? 1 : 2);
                if (stoneId != null)
                {
                    switch (stoneId.Length)
                    {
                        case 1:
                            switch (stoneId[0])
                            {
                                case '8':
                                    items.Add(new("(O)66", numToDrop));
                                    break;
                                case '6':
                                    items.Add(new("(O)70", numToDrop));
                                    break;
                                case '4':
                                    items.Add(new("(O)64", numToDrop));
                                    break;
                                case '2':
                                    items.Add(new("(O)72", numToDrop));
                                    break;
                            }
                            break;
                        case 2:
                            switch (stoneId[1])
                            {
                                case '0':
                                    if (stoneId == "10")
                                    {
                                        items.Add(new("(O)68", numToDrop));
                                    }
                                    break;
                                case '2':
                                    if (stoneId == "12")
                                    {
                                        items.Add(new("(O)60", numToDrop));
                                    }
                                    break;
                                case '4':
                                    if (stoneId == "14")
                                    {
                                        items.Add(new("(O)62", numToDrop));
                                    }
                                    break;
                            }
                            break;
                    }
                }
            }
            if (stoneId == 46.ToString())
            {
                /*
    public const int copperDebris = 0;
    public const int ironDebris = 2;
    public const int coalDebris = 4;
    public const int goldDebris = 6; 384
    public const int coinsDebris = 8;
    public const int iridiumDebris = 10;
    public const int woodDebris = 12;
    public const int stoneDebris = 14;
    public const int bigStoneDebris = 32;
    public const int bigWoodDebris = 34;
                */
                items.Add(new("(O)386", r.Next(1, 4)));
                items.Add(new("(O)384", r.Next(1, 5)));
                if (r.NextDouble() < 0.25)
                {
                    items.Add(new("(O)74"));
                }
            }
            if ((Game1.currentLocation.IsOutdoors || Game1.currentLocation.treatAsOutdoors.Value) && experience == 0)
            {
                double chanceModifier = farmerDailyLuck / 2.0 + (double)farmerMiningLevel * 0.005 + (double)farmerLuckLevel * 0.001;
                Random ran = Utility.CreateDaySaveRandom(x * 1000, y);
                // items.Add(new("(O)390"));
                if (who != null)
                {
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
                        items.Add(new("(O)382"));
                    }
                }
                if (ran.NextDouble() < 0.05 * (1.0 + chanceModifier))
                {
                    items.Add(new("(O)382"));
                }
            }
            if (who != null && Game1.currentLocation.HasUnlockedAreaSecretNotes(who) && r.NextDouble() < 0.0075)
            {
                items.Add(new("SecretNote"));

            }
            return items;
        }

    }
}