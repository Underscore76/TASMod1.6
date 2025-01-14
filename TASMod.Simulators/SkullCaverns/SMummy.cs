using System;
using Microsoft.Xna.Framework;
using StardewValley;

namespace TASMod.Simulators.SkullCaverns
{
    public class SMummy : SMonster
    {
        public SMummy(Vector2 position, Random Game1_random)
            : base(position, "Mummy", Game1_random)
        {
            SpriteHeight = 32;
        }
    }
}
