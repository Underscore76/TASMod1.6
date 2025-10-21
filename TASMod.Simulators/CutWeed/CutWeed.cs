using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Tools;
using TASMod.Extensions;
using TASMod.Helpers;
using TASMod.System;
namespace TASMod.Simulators.CutWeed
{
    public class SwingHit
    {
        public int NumMixedSeeds = 0;
        public int NumMixedFlowerSeeds = 0;
        public int NumFiber = 0;
        public int NumHats = 0;
        public int NumQuartz = 0;
        public int NumWeeds = 0;
    }

    public class CutWeed
    {
        public static int CurrentFrame = -1;
        public static List<SwingHit> Hits = new List<SwingHit> { new(), new(), new(), new() };

        public static SwingHit Current()
        {
            return Estimate(Game1.game1.instanceIndex);
        }

        public static SwingHit Estimate(int index)
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
                            ModEntry.Console.Log($"CutWeed Estimate Exception: {e}", StardewModdingAPI.LogLevel.Error);
                        }
                    }
                }
                return Hits[index];
            }
            return new();
        }

        public static SwingHit EstimateIndex(int index)
        {
            Farmer farmer = Reflector.GetStaticVar(index, "Game1__player") as Farmer;
            Random random = (Reflector.GetStaticVar(index, "Game1_random") as Random).Copy();
            GameLocation loc = GameRunner.instance.gameInstances[index].instanceGameLocation;
            return EstimateFarmer(random, loc, farmer);
        }
        public static SwingHit EstimateFarmer(Random random, GameLocation loc, Farmer who)
        {
            Random r = random.Copy();
            MeleeWeapon weapon = who.CurrentTool as MeleeWeapon;
            SwingHit hit = new SwingHit();
            if (weapon != null)
            {
                Vector2 actionTile = who.GetToolLocation(ignoreClick: true);
                MeleeWeapon_DoDamage(hit, r, weapon, loc, (int)actionTile.X, (int)actionTile.Y, who.FacingDirection, 1, who);
            }
            return hit;
        }

        public static void MeleeWeapon_DoDamage(SwingHit hit, Random Game1_random, MeleeWeapon weapon, GameLocation location, int x, int y, int facingDirection, int power, Farmer who)
        {
            if (weapon.type.Value != 2)
            {
                // Tool.DoFunction
                Game1_random.Next();
            }
            // at StardewValley.Tools.MeleeWeapon.getAreaOfEffect
            Vector2 tileLoc = Vector2.Zero;
            Vector2 tileLoc2 = Vector2.Zero;
            Rectangle areaOfEffect = WeaponInfo.GetAreaOfEffect(weapon, x, y, facingDirection, ref tileLoc, ref tileLoc2, who.GetBoundingBox(), 0, Game1_random);
            foreach (Vector2 v in Utility.removeDuplicates(Utility.getListOfTileLocationsForBordersOfNonTileRectangle(areaOfEffect)))
            {
                if (location.objects.TryGetValue(v, out var obj) && obj.IsWeeds())
                {
                    cutWeed(hit, Game1_random, obj, who);
                    hit.NumWeeds++;
                }
            }
        }

        public static void cutWeed(SwingHit hit, Random Game1_random, StardewValley.Object obj, Farmer who)
        {
            string sound = "cut";
            string toDrop = null;
            if (Game1_random.NextBool())
            {
                toDrop = "771";
            }
            else if (Game1_random.NextDouble() < 0.05 + ((who.stats.Get("Book_WildSeeds") != 0) ? 0.04 : 0.0))
            {
                toDrop = "770";
            }
            else if (Game1.currentSeason == "summer" && Game1_random.NextDouble() < 0.05 + ((who.stats.Get("Book_WildSeeds") != 0) ? 0.04 : 0.0))
            {
                toDrop = "MixedFlowerSeeds";
            }
            if (obj.name.Contains("GreenRainWeeds") && Game1_random.NextDouble() < 0.1)
            {
                toDrop = "Moss";
            }

            string qualifiedItemId = obj.QualifiedItemId;
            if (qualifiedItemId != null)
            {
                switch (qualifiedItemId)
                {
                    // ice mines crystals
                    case "(O)319":
                    case "(O)320":
                    case "(O)321":
                        sound = "breakingGlass";
                        toDrop = null;
                        break;

                    // forest farm weeds
                    case "(O)793":
                    case "(O)794":
                    case "(O)792":
                        toDrop = "770"; // Mixed seeds
                        break;

                    // Special island weeds
                    case "(O)883":
                    case "(O)884":
                    case "(O)882":
                        if (Game1.MasterPlayer.hasOrWillReceiveMail("islandNorthCaveOpened") &&
                            Game1_random.NextDouble() < 0.1 &&
                            !Game1.MasterPlayer.hasOrWillReceiveMail("gotMummifiedFrog"))
                        {
                            toDrop = "828";
                        }
                        else if (Game1_random.NextDouble() < 0.01)
                        {
                            toDrop = "828";
                        }
                        else if (Game1_random.NextDouble() < 0.08)
                        {
                            toDrop = "831";
                        }
                        break;

                    case "GreenRainWeeds0":
                    case "GreenRainWeeds1":
                    case "GreenRainWeeds4":
                        sound = "weed_cut";
                        break;

                    case "(O)313":
                    case "(O)314":
                    case "(O)315":
                    case "(O)316":
                    case "(O)317":
                    case "(O)318":
                    case "(O)678":
                    case "(O)679":
                        break;
                }
            }
            if (sound.Equals("breakingGlass") && Game1_random.NextDouble() < 0.0025)
            {
                toDrop = "338";
            }
            // broadcasting sprites
            Game1_random.Next(); Game1_random.Next();
            Game1_random.Next(); Game1_random.Next();
            Game1_random.Next(); Game1_random.Next();

            if (!sound.Equals("breakingGlass"))
            {
                if (Game1_random.NextDouble() < 1E-05)
                {
                    // itemregistry.create
                    Game1_random.Next();
                    Game1_random.Next();
                    hit.NumHats++;
                }
                if (Game1_random.NextDouble() <= 0.01 && Game1.player.team.SpecialOrderRuleActive("DROP_QI_BEANS"))
                {
                    // won't happen currently
                }
            }
            if (toDrop != null)
            {
                // new Debris(new Object(toDrop, 1))
                Game1_random.Next(); Game1_random.Next();
            }
            if (Game1_random.NextDouble() < 0.02)
            {
                // frog
            }
            if (obj.Location.HasUnlockedAreaSecretNotes(who) && Game1_random.NextDouble() < 0.009)
            {
                // creating secret notes
            }
            switch (toDrop)
            {
                case "771":
                    hit.NumFiber++;
                    break;
                case "770":
                    hit.NumMixedSeeds++;
                    break;
                case "338":
                    hit.NumQuartz++;
                    break;
                case "MixedFlowerSeeds":
                    hit.NumMixedFlowerSeeds++;
                    break;
            }
        }
    }
}
