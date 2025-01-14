using System;
using Microsoft.Xna.Framework;

namespace TASMod.Simulators.SkullCaverns
{
    public class SDustSpirit : SMonster
    {
        public SDustSpirit(Vector2 position, Random Game1_random)
            : base(position, "Dust Spirit", Game1_random)
        {
            float Scale = (float)Game1_random.Next(75, 101) / 100f;
            byte voice = (byte)Game1_random.Next(1, 24);
        }
    }
}
