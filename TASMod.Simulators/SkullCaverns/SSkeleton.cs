using System;
using Microsoft.Xna.Framework;

namespace TASMod.Simulators.SkullCaverns
{
    public class SSkeleton : SMonster
    {
        public SSkeleton(Vector2 position, bool isMage, Random Game1_random)
            : base(position, "Skeleton", Game1_random.Next(4), Game1_random) { }
    }
}
