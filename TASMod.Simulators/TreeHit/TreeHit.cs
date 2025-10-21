using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Constants;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using TASMod.Extensions;
using TASMod.System;
using Netcode;
using TASMod.Simulators.Books;
using BF = StardewValley.BellsAndWhistles.Butterfly;
using StardewValley.Menus;

namespace TASMod.Simulators.TreeHit
{
    public class SDayTimeMoneyBox
    {
        // daytime money box
        public int moneyShakeTimer;
        public int timeShakeTimer;
        public int questPulseTimer;
        public int whenToPulseTimer;
        public SDayTimeMoneyBox(DayTimeMoneyBox other)
        {
            moneyShakeTimer = other.moneyShakeTimer;
            timeShakeTimer = other.timeShakeTimer;
            questPulseTimer = other.questPulseTimer;
            whenToPulseTimer = other.whenToPulseTimer;
        }

        public void draw(Random Game1_random, Farmer who)
        {
            if (timeShakeTimer > 0)
            {
                timeShakeTimer -= Game1.currentGameTime.ElapsedGameTime.Milliseconds;
            }
            if (questPulseTimer > 0)
            {
                questPulseTimer -= Game1.currentGameTime.ElapsedGameTime.Milliseconds;
            }
            if (whenToPulseTimer >= 0)
            {
                whenToPulseTimer -= Game1.currentGameTime.ElapsedGameTime.Milliseconds;
                if (whenToPulseTimer <= 0)
                {
                    whenToPulseTimer = 3000;
                    if (who.hasNewQuestActivity())
                    {
                        questPulseTimer = 1000;
                    }
                }
            }
            if (timeShakeTimer > 0)
            {
                Game1_random.Next();
                Game1_random.Next();
            }
            if (who.hasVisibleQuests)
            {
                if (questPulseTimer > 0)
                {
                    float scaleMult = 1f / (Math.Max(300f, Math.Abs(questPulseTimer % 1000 - 500)) / 500f);
                    if (scaleMult > 1f)
                    {
                        Game1_random.Next();
                        Game1_random.Next();
                    }
                }
            }
            // drawMoneyBox
            if (moneyShakeTimer > 0)
            {
                Game1_random.Next();
                Game1_random.Next();
                Game1_random.Next();
                Game1_random.Next();
                moneyShakeTimer -= Game1.currentGameTime.ElapsedGameTime.Milliseconds;
            }
        }
    }
    public class TreeHitInstance
    {
        public bool HitTree = false;
        public int IndexAtSpawnBook = -1;
        public int IndexNeededSpawnBook = -1;
        public bool SecretNote = false;
        public bool WoodySecret = false;
        public bool MysteryBox = false;
        public string RareCosmetic = "";
        public string SkillBook = "";
        public bool Verbose = false;
    }

    public class TreeHit
    {
        public static int CurrentFrame = -1;
        public static List<TreeHitInstance> Hits = new List<TreeHitInstance> { new(), new(), new(), new() };

        public static TreeHitInstance Current()
        {
            return Estimate(Game1.game1.instanceIndex);
        }

        public static TreeHitInstance Estimate(int index)
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
                            Hits[i] = new();
                            ModEntry.Console.Log($"Error estimating tree hit for index {i}: {e}", StardewModdingAPI.LogLevel.Error);

                        }
                    }
                }
                return Hits[index];
            }
            return new();
        }

        public static TreeHitInstance VerboseEstimateIndex(int index)
        {
            if (index < 0 || index >= GameRunner.instance.gameInstances.Count)
                return new();
            Farmer farmer = Reflector.GetStaticVar(index, "Game1__player") as Farmer;
            Random random = Reflector.GetStaticVar(index, "Game1_random") as Random;
            NetRootDictionary<long, Farmer> otherFarmers = Reflector.GetStaticVar(index, "Game1_otherFarmers") as NetRootDictionary<long, Farmer>;
            GameLocation loc = GameRunner.instance.gameInstances[index].instanceGameLocation;
            DayTimeMoneyBox dayTimeMoneyBox = Reflector.GetStaticVar(index, "Game1_dayTimeMoneyBox") as DayTimeMoneyBox;
            if (loc == null || farmer == null || random == null || otherFarmers == null || dayTimeMoneyBox == null)
                return new();
            return EstimateFarmer(random.Copy(), loc, farmer, otherFarmers, dayTimeMoneyBox, true);
        }

        public static TreeHitInstance EstimateIndex(int index)
        {
            if (index < 0 || index >= GameRunner.instance.gameInstances.Count)
                return new();
            Farmer farmer = Reflector.GetStaticVar(index, "Game1__player") as Farmer;
            Random random = Reflector.GetStaticVar(index, "Game1_random") as Random;
            NetRootDictionary<long, Farmer> otherFarmers = Reflector.GetStaticVar(index, "Game1_otherFarmers") as NetRootDictionary<long, Farmer>;
            GameLocation loc = GameRunner.instance.gameInstances[index].instanceGameLocation;
            DayTimeMoneyBox dayTimeMoneyBox = Reflector.GetStaticVar(index, "Game1_dayTimeMoneyBox") as DayTimeMoneyBox;
            if (loc == null || farmer == null || random == null || otherFarmers == null || dayTimeMoneyBox == null)
                return new();
            return EstimateFarmer(random.Copy(), loc, farmer, otherFarmers, dayTimeMoneyBox);
        }
        public static TreeHitInstance EstimateFarmer(Random random, GameLocation loc, Farmer who, NetRootDictionary<long, Farmer> otherFarmers, DayTimeMoneyBox dayTimeMoneyBox, bool verbose = false)
        {
            Random r = random.Copy();

            // run the blink/object wiggle loop
            Tool tool = who.CurrentTool;
            TreeHitInstance hit = new TreeHitInstance();
            hit.Verbose = verbose;
            Vector2 toolLocation = who.GetToolLocation();
            Vector2 tile = new Vector2((int)toolLocation.X / 64, (int)toolLocation.Y / 64);
            if (tool != null && tool is Axe axe && loc.terrainFeatures.TryGetValue(tile, out var tf) && tf is Tree tree)
            {
                DelaySwing(hit, r, loc, who, otherFarmers, dayTimeMoneyBox);
                hit.HitTree = true;
                Axe_DoFunction(hit, r, axe, tree, tile, who);
            }
            return hit;
        }

        public static void DelaySwing(TreeHitInstance hit, Random Game1_random, GameLocation loc, Farmer who, NetRootDictionary<long, Farmer> otherFarmers, DayTimeMoneyBox dayTimeMoneyBox)
        {
            int blinkTimer = who.blinkTimer;
            Dictionary<long, int> otherBlinkTimers = new Dictionary<long, int>();
            SDayTimeMoneyBox sdayTimeMoneyBox = new SDayTimeMoneyBox(dayTimeMoneyBox);
            SGameLocation sloc = new SGameLocation();
            sloc.DisplayWidth = loc.map.DisplayWidth;
            sloc.DisplayHeight = loc.map.DisplayHeight;
            sloc.critters = new List<SCritter>();
            if (loc != null && loc.critters != null && loc.critters.Count > 0)
            {
                foreach (var critter in loc.critters)
                {
                    if (critter is BF bf)
                    {
                        sloc.critters.Add(new SButterfly(Game1_random, bf));
                    }
                }
            }
            if (hit.Verbose)
            {
                Controller.Console.Warn($"TreeHit DelaySwing for {who.Name} blinkTimer {blinkTimer} critters {loc.critters?.Count ?? 0}");
            }
            foreach (var kvp in otherFarmers)
            {
                if (kvp.Value.UniqueMultiplayerID == who.UniqueMultiplayerID)
                    continue;
                otherBlinkTimers[kvp.Key] = kvp.Value.blinkTimer;
            }
            int numSpots = loc.objects.Values.Where(o => o.QualifiedItemId == "(O)590" || o.QualifiedItemId == "(O)SeedSpot").Count();
            for (int i = 0; i < 8; i++)
            {
                if (hit.Verbose)
                {
                    Controller.Console.Warn($"TreeHit DelaySwing frame {i}");
                }
                int count = Game1_random.get_Index();
                // Farmer.updateCommon
                blinkTimer += 16;
                if (blinkTimer > 2200 && Game1_random.NextDouble() < 0.01)
                {
                    if (hit.Verbose)
                    {
                        Controller.Console.Warn($"\tblink {Game1_random.get_Index()}");
                    }
                    blinkTimer = -150;
                }
                // in multiplayer you need to estimate the blinks of the Game1.otherFarmers
                foreach (var kvp in otherFarmers)
                {
                    if (kvp.Value.UniqueMultiplayerID == who.UniqueMultiplayerID)
                        continue;
                    otherBlinkTimers[kvp.Key] += 16;
                    if (otherBlinkTimers[kvp.Key] > 2200 && Game1_random.NextDouble() < 0.01)
                    {
                        if (hit.Verbose)
                        {
                            Controller.Console.Warn($"\tother blink {Game1_random.get_Index()}");
                        }
                        otherBlinkTimers[kvp.Key] = -150;
                    }
                }
                // update currentlocation
                // TODO: butterfly updates as part of GameLocation.UpdateWhenCurrentLocation
                if (hit.Verbose)
                {
                    Controller.Console.Warn($"\tPRE:{sloc.critters?.Count ?? 0} critters {Game1_random.get_Index()}");
                }
                sloc.critters?.RemoveAll((SCritter critter) => critter.update(sloc));
                if (hit.Verbose)
                {
                    Controller.Console.Warn($"\tPOST:{sloc.critters?.Count ?? 0} critters {Game1_random.get_Index()}");
                }
                for (int j = 0; j < numSpots; j++)
                {
                    Game1_random.NextDouble();
                    if (hit.Verbose)
                    {
                        Controller.Console.Warn($"\tartifact spots {Game1_random.get_Index()}");
                    }
                }
                if (hit.Verbose)
                {
                    Controller.Console.Warn($"\tPRE:daytimemoneybox {Game1_random.get_Index()}");
                }
                sdayTimeMoneyBox.draw(Game1_random, who);
                if (hit.Verbose)
                {
                    Controller.Console.Warn($"\tend {Game1_random.get_Index()} {Game1_random.get_Index() - count}");
                }
            }
        }

        public static void Axe_DoFunction(TreeHitInstance hit, Random Game1_random, Axe axe, Tree tree, Vector2 tile, Farmer who)
        {
            Game1_random.Next(); // RecentMultiplayerRandom
            Game1_random.Next(); Game1_random.Next(); // rumble as heavy hitter

            Tree_performToolAction(hit, Game1_random, axe, tree, tile, who);
        }

        public static void Tree_performToolAction(TreeHitInstance hit, Random Game1_random, Axe axe, Tree tree, Vector2 tileLocation, Farmer who)
        {
            if (tree.growthStage.Value < 5)
                return;
            int numChunks = Game1_random.Next(1, 3);
            for (int i = 0; i < numChunks; i++)
            {
                Game1_random.Next();
            }
            if (!tree.stump.Value && who != null && who.currentLocation.HasUnlockedAreaSecretNotes(who) && Game1_random.NextDouble() < 0.005)
            {
                // secret note
                hit.SecretNote = true;
            }
            else if (!tree.stump.Value && who != null && Utility.tryRollMysteryBox(0.005, Game1_random))
            {
                // mystery box
                hit.MysteryBox = true;
            }
            else if (!tree.stump.Value && who != null && who.stats.Get("TreesChopped") > 20 && Game1_random.NextDouble() < 0.0003 + (who.mailReceived.Contains("GotWoodcuttingBook") ? 0.0007 : ((double)who.stats.Get("TreesChopped") * 1E-05)))
            {
                hit.WoodySecret = true;
            }
            else if (!tree.stump.Value)
            {
                Utility_trySpawnRareObject(hit, Game1_random, who);
            }
        }
        public static void Utility_trySpawnRareObject(TreeHitInstance hit, Random Game1_random, Farmer who)
        {
            double chanceModifier = 0.33;
            double luckMod = 1.0;
            double dailyLuckWeight = 1.0;

            if (who != null)
            {
                luckMod = 1.0 + who.team.AverageDailyLuck() * dailyLuckWeight;
            }
            if (who != null && who.stats.Get(StatKeys.Mastery(0)) != 0 && Game1_random.NextDouble() < 0.001 * chanceModifier * luckMod)
            {
            }
            if (Game1.stats.DaysPlayed > 2 && Game1_random.NextDouble() < 0.002 * chanceModifier)
            {
                hit.RareCosmetic = "Untraced RNG";
                return;
            }

            hit.IndexAtSpawnBook = Game1_random.get_Index();
            Random r = Game1_random.Copy();
            if (Game1.stats.DaysPlayed > 2 && Game1_random.NextDouble() < 0.0006 * chanceModifier)
            {
                int whichBook = Game1_random.Next(5);
                hit.SkillBook = "(O)Book_" + whichBook;
                hit.IndexNeededSpawnBook = hit.IndexAtSpawnBook;
                // itemregistery.create
                Game1_random.Next();
                Game1_random.Next();
            }
            else
            {
                while (r.NextDouble() >= 0.0006 * chanceModifier) { }
                hit.IndexNeededSpawnBook = r.get_Index();
            }
        }
    }
}
