using System;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Extensions;

namespace TASMod.Simulators.SkullCaverns
{
    public class SBigSlime : SMonster
    {
        public string heldItem;

        public SBigSlime(Vector2 position, int mineArea, Random Game1_random)
            : base(position, "Big Slime", Game1_random)
        {
            SpriteWidth = 32;
            SpriteHeight = 32;
            Game1_random.Next(-20, 21);
            Game1_random.Next(-20, 21);
            Game1_random.Next(-20, 21);

            Game1_random.Next(7, 11);
            if (Game1_random.NextDouble() < 0.01 && mineArea >= 40)
            {
                heldItem = "(O)221";
                // object ctor flipped
                Game1_random.NextBool();
            }

            if (Game1.mine != null && Game1.mine.GetAdditionalDifficulty() > 0)
            {
                if (Game1_random.NextDouble() < 0.1)
                {
                    heldItem = "(O)858";
                    // object ctor flipped
                    Game1_random.NextBool();
                }
                else if (Game1_random.NextDouble() < 0.005)
                {
                    heldItem = "(O)896";
                    // object ctor flipped
                    Game1_random.NextBool();
                }
            }

            if (Game1_random.NextBool() && Game1.player.team.SpecialOrderRuleActive("SC_NO_FOOD"))
            {
                heldItem = "(O)930";
                // object ctor flipped
                Game1_random.NextBool();
            }
        }

        public override Rectangle GetBoundingBox()
        {
            Vector2 vector = base.Position;
            return new Rectangle((int)vector.X + 8, (int)vector.Y, SpriteWidth * 4 * 3 / 4, 64);
        }
    }
}
