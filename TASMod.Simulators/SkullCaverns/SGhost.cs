using System;
using Microsoft.Xna.Framework;

namespace TASMod.Simulators.SkullCaverns
{
    public class SGhost : SMonster
    {
        public SGhost(Vector2 position, string name, Random Game1_random, int identifier)
            : base(position, name, Game1_random) { }

        public SGhost(Vector2 position, string name, Random Game1_random)
            : this(position, name, Game1_random, Game1_random.Next(-99999, 99999)) { }
    }
}
