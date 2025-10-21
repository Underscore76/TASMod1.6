using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Constants;
using StardewValley.Extensions;
using StardewValley.Locations;
using StardewValley.Monsters;
using StardewValley.Tools;
using TASMod.Extensions;
using TASMod.Helpers;
using TASMod.System;

namespace TASMod.Simulators.EnemyKill
{
    public class EnemyHit
    {
        public string RareCosmetic = "";
        public string RareItem = "";
        public int Damage = 0;
        public int IndexNeededSpawnRare = 0;
        public int IndexAtSpawnRare = 0;
        public int IndexNeededVoidBook = 0;
        public int IndexAtVoidBook = 0;
        public bool VoidBook = false;
        public bool Ladder = false;
        public EnemyHit()
        {
        }
    }

    public class EnemyKill
    {
        public static int CurrentFrame = -1;
        public static List<EnemyHit> Hits = new List<EnemyHit> { new(), new(), new(), new() };

        public static EnemyHit Current()
        {
            return Estimate(Game1.game1.instanceIndex);
        }

        public static EnemyHit Estimate(int index)
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
                        catch { }
                    }
                }
                return Hits[index];
            }
            return new();
        }
        public static EnemyHit EstimateIndex(int index)
        {
            Farmer farmer = Reflector.GetStaticVar(index, "Game1__player") as Farmer;
            Random random = (Reflector.GetStaticVar(index, "Game1_random") as Random).Copy();
            GameLocation loc = GameRunner.instance.gameInstances[index].instanceGameLocation;
            return EstimateFarmer(random, loc, farmer);
        }
        public static EnemyHit EstimateFarmer(Random random, GameLocation loc, Farmer who)
        {
            Random r = random.Copy();
            MeleeWeapon weapon = who.CurrentTool as MeleeWeapon;
            EnemyHit hit = new EnemyHit();
            if (weapon != null)
            {
                Vector2 actionTile = who.GetToolLocation(ignoreClick: true);
                MeleeWeapon_DoDamage(hit, r, weapon, loc, (int)actionTile.X, (int)actionTile.Y, who.FacingDirection, 1, who);
            }
            return hit;
        }

        public static void MeleeWeapon_DoDamage(EnemyHit hit, Random Game1_random, MeleeWeapon weapon, GameLocation location, int x, int y, int facingDirection, int power, Farmer who)
        {
            if (weapon.type.Value != 2)
            {
                // Tool.DoFunction
                Game1_random.Next();
            }
            // at StardewValley.Tools.MeleeWeapon.getAreaOfEffect
            Vector2 tileLoc = Vector2.Zero;
            Vector2 tileLoc2 = Vector2.Zero;
            Rectangle areaOfEffect = WeaponInfo.GetAreaOfEffect(weapon, x, y, facingDirection, ref tileLoc, ref tileLoc2, PlayerInfo.BoundingBox, 0, Game1_random);

            // at StardewValley.GameLocation.damageMonster
            float effectiveCritChance = weapon.critChance.Value;
            if (weapon.type.Value == 1)
            {
                effectiveCritChance += 0.005f;
                effectiveCritChance *= 1.12f;
            }
            int minDamage = weapon.minDamage.Value;
            int maxDamage = weapon.maxDamage.Value;
            float knockback = weapon.knockback.Value;
            int addedPrecision = weapon.addedPrecision.Value;
            float critMultiplier = weapon.critMultiplier.Value;
            int type = weapon.type.Value;
            bool isOnSpecial = weapon.isOnSpecial;
            GameLocation_damageMonster(
                hit,
                Game1_random, location,
                areaOfEffect,
                (int)(minDamage * (1f + who.buffs.AttackMultiplier)),
                (int)(maxDamage * (1f + who.buffs.AttackMultiplier)),
                isBomb: false,
                knockback * (1f + who.buffs.KnockbackMultiplier),
                (int)(addedPrecision * (1f + who.buffs.WeaponPrecisionMultiplier)),
                effectiveCritChance * (1f + who.buffs.CriticalChanceMultiplier),
                critMultiplier * (1f + who.buffs.CriticalPowerMultiplier),
                type != 1 || !isOnSpecial,
                who
            );
        }

        public static void GameLocation_damageMonster(EnemyHit hit, Random Game1_random, GameLocation location, Rectangle areaOfEffect, int minDamage, int maxDamage, bool isBomb, float knockBackModifier, int addedPrecision, float critChance, float critMultiplier, bool triggerMonsterInvincibleTimer, Farmer who, bool isProjectile = false)
        {
            Monster hitMonster = null;
            for (int i = location.characters.Count - 1; i >= 0; i--)
            {
                if (i < location.characters.Count && location.characters[i] is Monster monster && monster.IsMonster && monster.Health > 0 && monster.TakesDamageFromHitbox(areaOfEffect))
                {
                    hitMonster = monster;
                    break;
                }
            }
            if (hitMonster == null)
            {
                return;
            }
            Rectangle monsterBox = hitMonster.GetBoundingBox();
            // rumble
            Game1_random.Next();
            Game1_random.Next();
            // get away from player trajectory
            Game1_random.Next();
            Game1_random.Next();

            int damageAmount = Game1_random.Next(minDamage, maxDamage + 1);
            bool crit = false;
            if (who != null && Game1_random.NextDouble() < (double)(critChance + (float)who.LuckLevel * (critChance / 40f)))
            {
                crit = true;
            }

            damageAmount = (crit ? ((int)((float)damageAmount * critMultiplier)) : damageAmount);
            damageAmount = Math.Max(1, damageAmount + ((who != null) ? (who.Attack * 3) : 0));
            hit.Damage = Monster_takeDamage(Game1_random, hitMonster, damageAmount, 0, 0, isBomb, (double)addedPrecision / 100.0, who);

            // create damage chunks
            Game1_random.Next();
            Game1_random.Next();

            GameLocation_onMonsterKilled(hit, Game1_random, location, who, hitMonster, monsterBox, false);
        }

        public static int Monster_takeDamage(Random Game1_random, Monster monster, int damage, int xTrajectory, int yTrajectory, bool isBomb, double addedPrecision, Farmer who)
        {
            if (monster is Bug bug)
            {
                return Bug_takeDamage(Game1_random, bug, damage, xTrajectory, yTrajectory, isBomb, addedPrecision, who);
            }
            return Monster_baseTakeDamage(Game1_random, monster, damage, xTrajectory, yTrajectory, isBomb, addedPrecision, "");
        }

        public static void Debris_ctor(Random Game1_random, int numChunks)
        {
            for (int i = 0; i < numChunks; i++)
            {
                // Debris:InitializeChunks
                Game1_random.Next();
            }
            for (int i = 0; i < numChunks; i++)
            {
                Game1_random.Next(2);
                Game1_random.Next(2);
                Game1_random.NextBool();
                Game1_random.Next(-32, -16);
            }
        }

        public static void createRadialDebris(Random Game1_random, int numberOfChunks)
        {
            while (numberOfChunks > 0)
            {
                switch (Game1_random.Next(4))
                {
                    case 0:
                        {
                            break;
                        }
                    case 1:
                        {
                            break;
                        }
                    case 2:
                        {
                            Game1_random.Next(-64, 64);
                            break;
                        }
                    case 3:
                        {
                            Game1_random.Next(-64, 64);
                            break;
                        }
                }
                Debris_ctor(Game1_random, 1);
                numberOfChunks--;
            }
        }

        public static int Bug_takeDamage(Random Game1_random, Bug bug, int damage, int xTrajectory, int yTrajectory, bool isBomb, double addedPrecision, Farmer who)
        {
            void shedChunks(Random Game1_random, Bug bug, int number, float scale)
            {
                createRadialDebris(Game1_random, number);
            }
            void localDeathAnimation(Random Game1_random, Bug bug)
            {
                // Utility.makeTemporarySpriteJuicier
                Game1_random.Next(); Game1_random.Next();
                Game1_random.Next(); Game1_random.Next();
                Game1_random.Next(); Game1_random.Next();
                Game1_random.Next(); Game1_random.Next();
            }
            void sharedDeathAnimation(Random Game1_random, Bug bug)
            {
                shedChunks(Game1_random, bug, Game1_random.Next(4, 9), 0.75f);
                localDeathAnimation(Game1_random, bug);
            }
            void deathAnimation(Random Game1_random, Bug bug)
            {
                sharedDeathAnimation(Game1_random, bug);
            }

            int actualDamage = Math.Max(1, damage - bug.resilience.Value);
            if (Game1_random.NextDouble() < bug.missChance.Value - bug.missChance.Value * addedPrecision)
            {
                actualDamage = -1;
            }
            else
            {
                deathAnimation(Game1_random, bug);
            }
            return actualDamage;
        }


        public static int Monster_baseTakeDamage(Random Game1_random, Monster monster, int damage, int xTrajectory, int yTrajectory, bool isBomb, double addedPrecision, string hitSound)
        {
            int actualDamage = Math.Max(1, damage - monster.resilience.Value);
            if (Game1_random.NextDouble() < monster.missChance.Value - monster.missChance.Value * addedPrecision)
            {
                actualDamage = -1;
            }
            return actualDamage;
        }

        public static void GameLocation_onMonsterKilled(EnemyHit hit, Random Game1_random, GameLocation location, Farmer who, Monster monster, Microsoft.Xna.Framework.Rectangle monsterBox, bool killedByBomb)
        {
            if (location is MineShaft)
            {
                MineShaft_MonsterDrop(hit, Game1_random, monster, monsterBox.Center.X, monsterBox.Center.Y, who);
            }
            return;
        }

        public static void Utility_trySpawnRareObject(EnemyHit hit, Random Game1_random, Farmer who)
        {
            double chanceModifier = 1.5;
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
                hit.RareCosmetic = "cosmetic";
                return;
            }

            hit.IndexAtSpawnRare = Game1_random.get_Index();
            Random r = Game1_random.Copy();
            if (Game1.stats.DaysPlayed > 2 && Game1_random.NextDouble() < 0.0006 * chanceModifier)
            {
                int whichBook = Game1_random.Next(5);
                hit.RareItem = "(O)Book_" + whichBook;
                hit.IndexNeededSpawnRare = hit.IndexAtSpawnRare;
                // itemregistery.create
                Game1_random.Next();
                Game1_random.Next();
            }
            else
            {
                while (r.NextDouble() >= 0.0006 * chanceModifier) { }
                hit.IndexNeededSpawnRare = r.get_Index();
            }
        }


        public static void MineShaft_MonsterDrop(EnemyHit hit, Random Game1_random, Monster monster, int x, int y, Farmer who)
        {
            // default GameLocation:MonsterDrop
            // drop the items
            for (int i = 0; i < monster.objectsToDrop.Count; i++)
            {
                // Debris:InitializeChunks
                Game1_random.Next();
            }
            Utility_trySpawnRareObject(hit, Game1_random, who);
            double chance = 0.0001 + ((!who.mailReceived.Contains("voidBookDropped")) ? ((double)who.stats.MonstersKilled * 1.5E-05) : 0.0004);
            if (who.stats.MonstersKilled > 10)
            {
                hit.IndexAtVoidBook = Game1_random.get_Index();
                Random copy = Game1_random.Copy();
                if (Game1_random.NextDouble() < chance)
                {
                    hit.VoidBook = true;
                    hit.IndexNeededVoidBook = hit.IndexAtVoidBook;
                }
                else
                {
                    while (copy.NextDouble() >= chance)
                    {
                    }
                    hit.IndexNeededVoidBook = copy.get_Index();
                }
            }
            // ladder stuff
            hit.Ladder = Game1_random.NextDouble() < 0.15;
        }
    }
}