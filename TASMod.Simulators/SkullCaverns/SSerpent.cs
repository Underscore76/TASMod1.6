using System;
using Microsoft.Xna.Framework;

namespace TASMod.Simulators.SkullCaverns
{
    public class SSerpent : SMonster
    {
        public SSerpent(Vector2 position, Random Game1_random)
            : this(position, "Serpent", Game1_random) { }

        public SSerpent(Vector2 position, string name, Random Game1_random)
            : base(position, name, Game1_random)
        {
            Game1_random.Next(10);
            if (name == "Royal Serpent")
            {
                Game1_random.Next(3, 7);
                if (Game1_random.NextDouble() < 0.1)
                {
                    Game1_random.Next(5, 10);
                }
                else if (Game1_random.NextDouble() < 0.01) { }
            }
            SpriteWidth = 32;
            SpriteHeight = 32;
        }

        public override Rectangle GetBoundingBox()
        {
            Vector2 vector = base.Position;
            return new Rectangle((int)vector.X + 8, (int)vector.Y, SpriteWidth * 4 * 3 / 4, 96);
        }
    }
}
