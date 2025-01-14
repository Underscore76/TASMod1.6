using System;
using Microsoft.Xna.Framework;
using StardewValley.Extensions;

namespace TASMod.Simulators.SkullCaverns
{
    public class SGrub : SMonster
    {
        public SGrub(Vector2 position, Random Game1_random)
            : base(position, "Grub", Game1_random)
        {
            SpriteHeight = 24;
            Game1_random.NextBool();

            Game1_random.Next(4);
            Game1_random.Next(4);
        }
    }
}
