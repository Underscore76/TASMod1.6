using System;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Extensions;

namespace TASMod.Simulators.SkullCaverns
{
    public class SGreenSlime : SMonster
    {
        public SGreenSlime(Vector2 position, Random Game1_random)
            : base(position, "Green Slime", Game1_random)
        {
            Game1_random.NextBool();

            int readyToMate = Game1_random.Next(1000, 120000);
            int num = Game1_random.Next(200, 256);
            Color color = new Color(
                num / Game1_random.Next(2, 10),
                Game1_random.Next(180, 256),
                (Game1_random.NextDouble() < 0.1) ? 255 : (255 - num)
            );
            bool flip = Game1_random.NextBool();
            bool cute = Game1_random.NextDouble() < 0.49;
        }

        public SGreenSlime(Vector2 position, int mineLevel, Random Game1_random)
            : base(position, "Green Slime", Game1_random)
        {
            Utility.RandomFloat(0f, 100f, Game1_random);
            bool cute = Game1_random.NextDouble() < 0.49;
            bool flip = Game1_random.NextBool();
            int specialNumber = Game1_random.Next(100);

            Name = "Sludge";
            parseMonsterInfo("Sludge", Game1_random);
            Game1_random.Next(-20, 21);
            Game1_random.Next(-20, 21);
            Game1_random.Next(-20, 21);
            while (Game1_random.NextDouble() < 0.08)
            {
                objectsToDrop.Add("386");
            }

            if (Game1_random.NextDouble() < 0.009)
            {
                objectsToDrop.Add("337");
            }

            if (
                Game1_random.NextDouble() < 0.01
                && Game1.MasterPlayer.mailReceived.Contains("slimeHutchBuilt")
            )
            {
                objectsToDrop.Add("439");
            }

            Game1_random.NextBool(); // left drift
            int readyToMate = Game1_random.Next(1000, 120000);
            if (Game1_random.NextDouble() < 0.001)
            {
                objectsToDrop.Add("GoldCoin");
                double val = (double)(int)(Game1.stats.DaysPlayed / 28) * 0.08;
                val = Math.Min(val, 0.55);
                while (Game1_random.NextDouble() < 0.1 + val)
                {
                    objectsToDrop.Add("GoldCoin");
                }
            }
        }

        public void makePrismatic() { }
    }
}
