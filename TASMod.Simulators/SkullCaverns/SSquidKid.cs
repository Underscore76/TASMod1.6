using System;
using Microsoft.Xna.Framework;

namespace TASMod.Simulators.SkullCaverns
{
    public class SSquidKid : SMonster
    {
        public SSquidKid(Vector2 position, Random Game1_random)
            : base(position, "Squid Kid", Game1_random)
        {
            SpriteHeight = 16;
        }
    }
}
