using System;
using Microsoft.Xna.Framework;
using StardewValley;

namespace TASMod.Simulators.SkullCaverns
{
    public class SBug : SMonster
    {
        public SBug(Vector2 position, int areaType, Random Game1_random)
            : base(position, "Bug", Game1_random)
        {
            SpriteHeight = 16;
        }

        public SBug(Vector2 position, int facingDirection, string specialType, Random Game1_random)
            : this(position, 0, Game1_random) { }

        public SBug(Vector2 position, int facingDirection, int areaType, Random Game1_random)
            : this(position, areaType, Game1_random) { }

        public override void BuffForAdditionalDifficulty(
            int additionalDifficulty,
            Random Game1_random
        )
        {
            Game1_random.NextDouble();
            base.BuffForAdditionalDifficulty(additionalDifficulty, Game1_random);
        }
    }
}
