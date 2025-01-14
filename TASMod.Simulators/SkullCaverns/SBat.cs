using System;
using Microsoft.Xna.Framework;

namespace TASMod.Simulators.SkullCaverns
{
    public class SBat : SMonster
    {
        public SBat(Vector2 position, int mineLevel, Random Game1_random)
            : base(position, "Bat", Game1_random)
        {
            SpriteWidth = 16;
            SpriteHeight = Game1_random.Next(-5, 6);
            switch (mineLevel)
            {
                case 77377:
                    parseMonsterInfo("Lava Bat", Game1_random);
                    Name = "Haunted Skull";
                    objectsToDrop.Clear();
                    break;
                case -555:
                    parseMonsterInfo("Magma Sprite", Game1_random);
                    Game1_random.Next(6, 9);
                    break;
                case -556:
                    parseMonsterInfo("Magma Sparker", Game1_random);
                    Game1_random.Next(6, 8);
                    break;
                case -789:
                    parseMonsterInfo("Iridium Bat", Game1_random);
                    objectsToDrop.Clear();
                    break;
                case -666:
                    parseMonsterInfo("Iridium Bat", Game1_random);
                    objectsToDrop.Clear();
                    break;
                default:
                    if (mineLevel >= 40 && mineLevel < 80)
                    {
                        Name = "Frost Bat";
                        parseMonsterInfo("Frost Bat", Game1_random);
                    }
                    else if (mineLevel >= 80 && mineLevel < 171)
                    {
                        Name = "Lava Bat";
                        parseMonsterInfo("Lava Bat", Game1_random);
                    }
                    else if (mineLevel >= 171)
                    {
                        Name = "Iridium Bat";
                        parseMonsterInfo("Iridium Bat", Game1_random);
                    }
                    break;
            }
        }
    }
}
