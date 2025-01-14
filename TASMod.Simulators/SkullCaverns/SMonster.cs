using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;

namespace TASMod.Simulators.SkullCaverns
{
    public class SMonster : SNPC
    {
        public List<string> objectsToDrop = new List<string>();
        public bool isHardModeMonster = false;

        public SMonster(Vector2 position, string name, Random Game1_random)
            : this(position, name, 0, Game1_random) { }

        public SMonster(Vector2 position, string name, int facingDirection, Random Game1_random)
            : base(position, name)
        {
            parseMonsterInfo(name, Game1_random);
        }

        public int GetBaseDifficultyLevel()
        {
            return 0;
        }

        public virtual void BuffForAdditionalDifficulty(
            int additionalDifficulty,
            Random Game1_random
        )
        {
            isHardModeMonster = true;
        }

        public void parseMonsterInfo(string name, Random Game1_random)
        {
            string[] array = DataLoader.Monsters(Game1.content)[name].Split('/');
            int Health = Convert.ToInt32(array[0]);
            int DamageToFarmer = Convert.ToInt32(array[1]);
            string[] array2 = ArgUtility.SplitBySpace(array[6]);
            objectsToDrop.Clear();
            for (int i = 0; i < array2.Length; i += 2)
            {
                if (Game1_random.NextDouble() < Convert.ToDouble(array2[i + 1]))
                {
                    objectsToDrop.Add(array2[i]);
                }
            }

            bool mineMonster = Convert.ToBoolean(array[12]);
            if (Game1.player.timesReachedMineBottom >= 1 && mineMonster)
            {
                Health += Game1_random.Next(0, Health);
                DamageToFarmer += Game1_random.Next(0, DamageToFarmer / 2);
            }
        }
    }
}
