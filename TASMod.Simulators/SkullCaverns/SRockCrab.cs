using System;
using Microsoft.Xna.Framework;

namespace TASMod.Simulators.SkullCaverns
{
    public class SRockCrab : SMonster
    {
        public SRockCrab(Vector2 position, Random Game1_random)
            : base(position, "Rock Crab", Game1_random)
        {
            bool waiter = Game1_random.NextDouble() < 0.4;
        }

        public SRockCrab(Vector2 position, string name, Random Game1_random)
            : base(position, name, Game1_random)
        {
            bool waiter = Game1_random.NextDouble() < 0.4;
        }
    }
}
