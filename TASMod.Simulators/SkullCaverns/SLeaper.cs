using System;
using Microsoft.Xna.Framework;
using StardewValley;

namespace TASMod.Simulators.SkullCaverns
{
    public class SLeaper : SMonster
    {
        public SLeaper(Vector2 position, Random Game1_random)
            : base(position, "Spider", Game1_random)
        {
            Utility.RandomFloat(1f, 1.5f, Game1_random);
            SpriteWidth = 32;
            SpriteHeight = 32;
        }
    }
}
